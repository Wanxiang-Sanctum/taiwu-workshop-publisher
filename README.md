# Taiwu Workshop Publisher

Taiwu Workshop Publisher 是把太吾绘卷 Mod 的 GitHub Release 产物发布到 Steam Workshop 的模板仓库。它维护发布清单、GitHub Actions workflow 和 C# 辅助工具；不维护 Mod 源码、组包规则或 GitHub Release 打包流程。

## 发布模型

标准发布路径只有一条：把 `publishing/workshop.yml` 的变更合并或推送到 `publishing` 分支。`Publish Workshop` workflow 会比较推送前后的清单，只处理新增清单条目或发布选择器发生变化的条目；随后下载指定 GitHub Release asset，解压出唯一的 `Config.Lua` 所在目录，生成 SteamCMD workshop item VDF，并在受保护的 GitHub Environment 中调用 SteamCMD。

本仓库只支持更新已有 Steam Workshop item，不支持创建新 item。清单中的每个 Mod 必须显式填写非 0 的 `fileId`；该值必须和发布内容 `Config.Lua` 中的 `FileId` 一致，否则发布失败。

发布信源分工如下：

- `publishing/workshop.yml` 只声明要发布的 Mod、Steam Workshop file id、Release 仓库和 Release tag。
- 清单指向的 Release 仓库负责提供可发布 zip；Mod 源码和组包规则留在该仓库或它自己的构建链路中。
- 每个 GitHub Release 必须有且只有一个 zip asset；zip 解压后必须能发现且只发现一个 `Config.Lua`。
- Steam Workshop 标题、简介、封面、可见性和更新说明来自发布目录中的 `Config.Lua`。
- SteamCMD VDF 的 `publishedfileid` 来自发布清单的 `fileId`；`Config.Lua` 的 `FileId` 只用于一致性校验。
- Mod 自身版本随发布内容中的 `Config.Lua` 一起交付；发布清单不重复维护版本，也不约束 tag 必须携带版本语义。
- 发布选择器由 `mods` 字典的 key 标识，并由有效 `repository`、`fileId` 和 `tag` 决定；新增清单条目会进入发布矩阵，纯格式、排序、注释变化不会发布，删除条目不会触发 Workshop 更新。
- 重复版本、重复 tag 或重复内容不由本仓库识别；重跑同一次发布会再次调用 SteamCMD。

当前 SteamCMD 路径只维护 Steamworks 文档列出的 VDF 字段。Workshop tags、依赖关系和太吾写入的 custom metadata 不在本仓库自动更新范围内；这些字段变化时需要用太吾内置上传流程或其它明确支持对应 Steam API 的工具处理。

## 仓库结构

- `publishing/workshop.yml`：发布意图清单。格式和产物约定见 `publishing/README.md`。
- `tools/Taiwu.WorkshopPublisher.Cli`：CI 与本地校验共用的 C# CLI。命令说明见 `tools/README.md`。
- `.github/workflows/publish-workshop.yml`：提交驱动的 Steam Workshop 发布 workflow。
- `.github/workflows/pull-request.yml`：locked restore、build 和格式检查。

## GitHub Actions

`Pull Request` workflow 对指向 `master` 或 `publishing` 的 pull request 执行 locked restore、build 和 dprint 检查，并通过 `packages.lock.json` 复用 NuGet 缓存。指向 `publishing` 的 pull request 只校验；合并后进入 `publishing` 的 push 才会触发发布。

`master` 是工具、文档和 workflow 模板的开发分支；`publishing` 是生产发布分支。`publishing` 必须包含完整仓库内容，不是只放清单的孤立分支。`Publish Workshop` workflow 只由推送 `publishing/workshop.yml` 到 `publishing` 触发。workflow 使用 `publishing` 分支上的 CLI 和 workflow 定义运行；更新发布工具后，需要把 `master` 合并或快进到 `publishing`，发布分支才会使用新实现。

workflow 用当前清单和基线清单生成发布矩阵；新增清单条目会进入矩阵，删除条目不会发布。发布分支初始化和基线解析规则见 `publishing/README.md`；无法解析基线时，workflow 会失败而不是无基线全量发布。需要重跑时使用 GitHub Actions 对同一次 workflow run 的 rerun 功能，不新增另一条发布路径。

发布 workflow 需要 GitHub Environment `steam-workshop`。建议把它设为需要人工审批的受保护环境，并配置这些 secrets：

- `STEAM_USERNAME`：Steam 发布账号。
- `STEAM_CONFIG_VDF`：已通过 Steam Guard 信任后的 SteamCMD `config.vdf` 文件内容。该值等同登录凭据，必须放在受保护环境 secret 中。
- `RELEASE_REPOSITORY_TOKEN`：可选。Release 仓库为私有或默认 `github.token` 无权读取时提供。

`STEAM_CONFIG_VDF` 应来自受控机器上已完成 Steam Guard 验证的 SteamCMD。用发布账号登录一次 SteamCMD 并完成验证后，复制该 SteamCMD 配置目录下 `config.vdf` 的全文作为 secret；不要把该文件提交到仓库或写入任何 CI cache。CI 发布时只执行 `+login <username>`，不传入账号密码；再次用密码登录会绕过已信任的无密码路径，并可能触发新的 Steam Guard 验证。

SteamCMD 运行文件和认证状态分开处理：

- `Publish Workshop` workflow 使用 GitHub 官方 `actions/cache` 缓存 SteamCMD 运行文件，并用 shell 安装 Linux 运行依赖、下载官方 SteamCMD tarball、完成首次初始化。
- 认证状态不进入 cache；发布前，workflow 会把 `STEAM_CONFIG_VDF` 写入临时 SteamCMD HOME 下的 `Steam/config/config.vdf`，未配置该 secret 时发布失败。
- 发布后，workflow 会删除临时 SteamCMD HOME 和可能残留在 SteamCMD 运行目录中的旧认证状态。
- workflow 不会把 CI 中可能更新的 `config.vdf` 写回 secret；如果 SteamCMD 要求重新验证，重新在受控机器登录并替换 `STEAM_CONFIG_VDF`。

C# CLI 只负责生成 VDF 和调用已经可用的 SteamCMD。

## 本地维护

恢复和构建：

```powershell
dotnet restore Taiwu.WorkshopPublisher.slnx --locked-mode
dotnet build Taiwu.WorkshopPublisher.slnx --no-restore
```

更新 `PackageReference` 或 `Directory.Packages.props` 后，运行普通 restore 刷新 lock file，再提交对应 `packages.lock.json`：

```powershell
dotnet restore Taiwu.WorkshopPublisher.slnx --force-evaluate
```

格式化和仓库检查：

```powershell
dotnet msbuild repo.proj -t:Check
dotnet msbuild repo.proj -t:Format
```

SteamCMD workshop VDF 和 `workshop_build_item` 语义以 Steamworks 文档为准：

- https://partner.steamgames.com/doc/features/workshop/implementation
- https://developer.valvesoftware.com/wiki/SteamCMD
