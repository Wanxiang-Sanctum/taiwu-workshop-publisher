# tools

本目录维护仓库内 C# 命令行工具。

## Taiwu.WorkshopPublisher.Cli

`Taiwu.WorkshopPublisher.Cli` 是发布 workflow 和本地校验共用的执行器。它读取 `publishing/` 下的 YAML 发布清单，定位已打包 Mod 目录中的 `Config.Lua`，校验发布内容，生成 SteamCMD workshop item VDF，并可调用已经可用的 SteamCMD。

命令行解析由 `System.CommandLine` 负责，YAML 解析由 `YamlDotNet` 负责，Lua 语法解析由 `Loretta.CodeAnalysis.Lua` 负责，SteamCMD 调用由 `CliWrap` 负责。CLI 只做发布协调和字段提取；`Config.Lua` 读取是静态字段提取，不执行 Lua 代码，也不读取 Mod 仓库源码结构或重新组包。

## 命令

- `build-matrix`：从 YAML 发布清单生成 GitHub Actions matrix；传入旧清单时只输出新增清单条目或发布选择器变化的条目。
- `resolve-target`：从 YAML 发布清单中的 Mod id 解析 Workshop file id、Release 仓库、Release tag 和 artifact 目录。
- `prepare-content`：从下载好的单个 Release zip 中定位唯一的 `Config.Lua`，并输出发布内容目录。
- `validate`：检查发布目录、`Config.Lua`、Workshop file id、预览图和 `Settings.Lua` 风险。
- `vdf`：生成 SteamCMD workshop item VDF；要求传入清单中的 `fileId`，并校验它与 `Config.Lua` 的 `FileId` 一致。
- `publish`：使用已生成的 VDF 调用 SteamCMD；从 `STEAM_USERNAME` 读取账号名，不读取账号密码；已信任的 SteamCMD 会话状态由调用方准备，可用 `--steam-home` 指向该状态所在的 SteamCMD HOME。

## 示例

解析发布目标：

```powershell
dotnet run --project tools/Taiwu.WorkshopPublisher.Cli -- resolve-target `
  --manifest publishing/workshop.yml `
  --mod-id example-mod `
  --artifacts-root artifacts
```

按清单变化生成发布矩阵：

```powershell
dotnet run --project tools/Taiwu.WorkshopPublisher.Cli -- build-matrix `
  --manifest publishing/workshop.yml `
  --previous-manifest artifacts/previous-workshop.yml
```

准备一个已下载的 Release zip：

```powershell
dotnet run --project tools/Taiwu.WorkshopPublisher.Cli -- prepare-content `
  --release-asset-dir artifacts/release-assets `
  --extract-dir artifacts/extracted-release
```

校验一个已打包的 Mod 目录：

```powershell
dotnet run --project tools/Taiwu.WorkshopPublisher.Cli -- validate `
  --content-dir artifacts/extracted-release/Wanxiang.Example `
  --file-id 1234567890
```

生成 SteamCMD VDF：

```powershell
dotnet run --project tools/Taiwu.WorkshopPublisher.Cli -- vdf `
  --content-dir artifacts/extracted-release/Wanxiang.Example `
  --file-id 1234567890 `
  --output artifacts/steamcmd/workshop-item.vdf
```

`validate` 和 `vdf` 默认拒绝发布目录中的 `Settings.Lua`，避免把本机玩家设置发布出去。确有需要时必须显式传入 `--allow-settings`。
