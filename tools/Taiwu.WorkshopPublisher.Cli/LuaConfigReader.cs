using System.Globalization;
using Loretta.CodeAnalysis;
using Loretta.CodeAnalysis.Lua;
using Loretta.CodeAnalysis.Lua.Syntax;

namespace Taiwu.WorkshopPublisher.Cli;

internal static class LuaConfigReader
{
    public static LuaTable ReadConfig(string text, string path, IReadOnlySet<string> fieldsToRead)
    {
        SyntaxTree tree = LuaSyntaxTree.ParseText(text, path: path);
        Diagnostic[] diagnostics =
        [
            .. tree
            .GetDiagnostics()
            .Where(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error),
        ];

        if (diagnostics.Length > 0)
        {
            throw new CliException($"Config.Lua parse failed: {diagnostics[0]}");
        }

        CompilationUnitSyntax root = tree.GetCompilationUnitRoot();
        ReturnStatementSyntax[] returnStatements =
        [
            .. root
            .Statements
            .Statements
            .OfType<ReturnStatementSyntax>(),
        ];

        if (returnStatements.Length != 1)
        {
            throw new CliException("Config.Lua must contain exactly one return statement.");
        }

        ReturnStatementSyntax returnStatement = returnStatements[0];

        if (returnStatement.Expressions.Count != 1)
        {
            throw new CliException("Config.Lua must return exactly one table.");
        }

        if (returnStatement.Expressions[0] is not TableConstructorExpressionSyntax table)
        {
            throw new CliException("Config.Lua must return a table constructor.");
        }

        return ConvertTopLevelTable(table, fieldsToRead);
    }

    private static LuaValue ConvertExpression(ExpressionSyntax expression)
    {
        return expression switch
        {
            LiteralExpressionSyntax literal => ConvertLiteral(literal),
            ParenthesizedExpressionSyntax parenthesized => ConvertExpression(parenthesized.Expression),
            TableConstructorExpressionSyntax => LuaTable.Empty,
            _ => throw new CliException(
                $"Config.Lua contains unsupported expression: {expression.Kind()}"),
        };
    }

    private static LuaValue ConvertLiteral(LiteralExpressionSyntax literal)
    {
        object? value = literal.Token.Value;

        return value switch
        {
            null => LuaNil.Instance,
            bool boolean => new LuaBoolean(boolean),
            string text => new LuaString(text),
            _ when literal.IsKind(SyntaxKind.NumericalLiteralExpression)
                => ParseNumber(literal.Token.Text),
            _ => throw new CliException($"Config.Lua contains unsupported literal: {literal.Kind()}"),
        };
    }

    private static LuaNumber ParseNumber(string text)
    {
        string normalized = text.Replace("_", string.Empty, StringComparison.Ordinal);

        if (!decimal.TryParse(
            normalized,
            NumberStyles.Float,
            CultureInfo.InvariantCulture,
            out decimal value))
        {
            throw new CliException($"Config.Lua contains unsupported number literal: {text}");
        }

        return new LuaNumber(normalized, value);
    }

    private static LuaTable ConvertTopLevelTable(TableConstructorExpressionSyntax table, IReadOnlySet<string> fieldsToRead)
    {
        Dictionary<string, LuaValue> fields = new(StringComparer.Ordinal);

        foreach (TableFieldSyntax field in table.Fields)
        {
            string? key = field switch
            {
                IdentifierKeyedTableFieldSyntax identifierField => identifierField.Identifier.ValueText,
                ExpressionKeyedTableFieldSyntax expressionField => TryConvertTableKey(expressionField.Key),
                _ => null,
            };

            if (key is null || !fieldsToRead.Contains(key))
            {
                continue;
            }

            if (!fields.TryAdd(key, field switch
            {
                IdentifierKeyedTableFieldSyntax identifierField => ConvertExpression(identifierField.Value),
                ExpressionKeyedTableFieldSyntax expressionField => ConvertExpression(expressionField.Value),
                _ => throw new InvalidOperationException("Unexpected Config.Lua table field shape."),
            }))
            {
                throw new CliException($"Config.Lua contains duplicate field: {key}");
            }
        }

        return new LuaTable(fields, []);
    }

    private static string? TryConvertTableKey(ExpressionSyntax expression)
    {
        try
        {
            return ConvertTableKey(expression);
        }
        catch (CliException)
        {
            return null;
        }
    }

    private static string ConvertTableKey(ExpressionSyntax expression)
    {
        LuaValue key = ConvertExpression(expression);

        return key switch
        {
            LuaString stringKey => stringKey.Value,
            LuaNumber numberKey => numberKey.Raw,
            _ => throw new CliException("Config.Lua table field key must be a string or number."),
        };
    }
}

internal abstract record LuaValue;

internal sealed record LuaString(string Value) : LuaValue;

internal sealed record LuaNumber(string Raw, decimal Value) : LuaValue;

internal sealed record LuaBoolean(bool Value) : LuaValue;

internal sealed record LuaNil : LuaValue
{
    public static LuaNil Instance { get; } = new();

    private LuaNil()
    {
    }
}

internal sealed record LuaTable(
    IReadOnlyDictionary<string, LuaValue> Fields,
    IReadOnlyList<LuaValue> Items) : LuaValue
{
    public static LuaTable Empty { get; } = new(new Dictionary<string, LuaValue>(StringComparer.Ordinal), []);
}
