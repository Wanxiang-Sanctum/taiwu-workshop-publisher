# Contributing

本文件面向维护 Taiwu Workshop Publisher 模板的开发者。它承接仓库维护流程、验证命令和文档同步规则；发布清单和 Steam 凭据契约仍由 [publishing/README.md](publishing/README.md) 维护，CLI 命令参考仍由 [tools/README.md](tools/README.md) 维护。

## 维护边界

本仓库维护的是发布模板，不是 Mod 源码仓库。维护工作通常落在这些边界内：

- GitHub Actions workflow。
- `publishing/workshop.yml` 的模板和清单解析契约。
- `tools/Taiwu.WorkshopPublisher.Cli` 的发布辅助逻辑。
- 仓库级构建、格式化、依赖和文档。

不要在本仓库引入 Mod 构建、组包、测试或 Release 创建流程。清单指向的 Release 仓库负责这些内容。

## 本地验证

本仓库使用 `global.json` 固定 .NET SDK 功能带，并通过 `packages.lock.json` 做 locked restore。

恢复和构建：

```powershell
dotnet restore Taiwu.WorkshopPublisher.slnx --locked-mode
dotnet build Taiwu.WorkshopPublisher.slnx --no-restore
```

格式化和仓库检查由 `repo.proj` 统一调用 dprint：

```powershell
dotnet msbuild repo.proj -t:InstallTools
dotnet msbuild repo.proj -t:Check
dotnet msbuild repo.proj -t:Format
```

`Pull Request` workflow 会执行 locked restore、build 和 `repo.proj -t:Check`。本地改动至少应跑同一组检查；只改文档时也应跑 `repo.proj -t:Check`。

## 依赖和工具

更新 `PackageReference` 或 `Directory.Packages.props` 后，运行普通 restore 刷新 lock file，再提交对应 `packages.lock.json`：

```powershell
dotnet restore Taiwu.WorkshopPublisher.slnx --force-evaluate
```

更新 dprint 或其它 aqua 管理的工具后，刷新校验和并提交对应配置变更：

```powershell
dotnet msbuild repo.proj -t:UpdateToolChecksums
```

## 分支职责

`master` 是工具、文档和 workflow 模板的开发分支；`publishing` 是生产发布分支。`publishing` 必须包含完整仓库内容，不是只放清单的孤立分支。

`Publish Workshop` workflow 使用 `publishing` 分支上的 CLI 和 workflow 定义运行。更新发布工具或 workflow 后，需要把 `master` 合并或快进到 `publishing`，发布分支才会使用新实现。

指向 `publishing` 的 pull request 只校验；合并后进入 `publishing` 的 push 才会触发发布。需要重跑失败的发布时，使用 GitHub Actions 对同一次 workflow run 的 rerun 功能，不通过无意义清单变更制造新发布。

## 文档同步

修改模板行为时，按契约归属同步文档：

- 改仓库职责、读者入口、分支职责或发布模型摘要时，同步更新 [README.md](README.md)。
- 改清单字段、发布选择器、基线规则、Release zip 产物约定或 GitHub Environment secret 时，同步更新 [publishing/README.md](publishing/README.md)。
- 改 CLI 命令、参数、默认值、校验行为或 GitHub Actions output 时，同步更新 [tools/README.md](tools/README.md) 和调用它的 workflow。
- 改本地验证、依赖更新、工具安装或维护协作流程时，同步更新本文件。

避免在多个文档复制同一份可变清单。根 README 只保留稳定路由和模型摘要；子目录 README 拥有各自目录的契约；本文件拥有维护流程。
