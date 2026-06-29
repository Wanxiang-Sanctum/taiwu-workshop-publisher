# publishing

本目录面向使用模板发布 Mod 的开发者。`workshop.yml` 是提交驱动发布的唯一入口；当该文件的变更进入 `publishing` 分支后，发布 workflow 会比较当前清单和基线清单，并按变化生成发布矩阵。

如果你要维护 workflow 或 C# CLI 的实现，先读 [CONTRIBUTING.md](../CONTRIBUTING.md)；CLI 命令细节见 [tools/README.md](../tools/README.md)。本文件只描述发布清单、Release 产物和 Steam 凭据这些模板使用者需要稳定依赖的契约。

## 发布前提

每个准备发布的 Mod 需要先满足这些条件：

- Steam Workshop item 已经存在，并且拥有非 0 的 published file id。
- 发布账号有权限更新该 Workshop item。
- Release 仓库已经创建目标 GitHub Release，并且产物满足下方 Release 产物契约。
- GitHub Environment `steam-workshop` 已配置发布所需 secrets。

本仓库不会重新组包，不理解 Mod 源码结构，也不会创建新的 Workshop item。

## 清单格式

清单以 Mod 为主体，保持小而固定：

```yaml
repository: owner/mods-repo
mods:
  example-mod:
    fileId: 1234567890
    tag: release-tag

  external-mod:
    fileId: 2345678901
    repository: other-owner/other-mod-repo
    tag: other-release-tag
```

字段含义：

- 顶级 `repository` 是默认 Release 仓库，使用 `owner/repository` 格式。
- `mods` 是必填字典，以 Mod id 为 key；这个 key 是 CI 和运维用的稳定 Mod 标识，不是 Workshop 标题，也不是版本信源。
- `mods.<id>.fileId` 是已存在 Steam Workshop item 的 published file id，必须非 0。
- `mods.<id>.tag` 是 Release 仓库自己的 tag；本仓库不约束命名格式。
- `mods.<id>.repository` 可选。单个 Mod 来自不同 Release 仓库时，用它覆盖顶级 `repository`。

空模板使用 `mods: {}`，表示当前没有发布目标；省略 `mods` 是清单错误。没有顶级 `repository` 时，每个 Mod 都必须显式填写 `mods.<id>.repository`。

如果 Mod id 或 tag 含有会影响 YAML 解析的字符，按 YAML 规则给该字符串加引号即可。重复 Mod id 会被视为清单错误。

## 触发发布

发布由 `publishing` 分支上的 `publishing/workshop.yml` 驱动：

1. 在目标 Release 仓库准备 GitHub Release zip。
2. 在本仓库修改 `publishing/workshop.yml`，让目标 Mod 指向新的 `tag`、`repository` 或 `fileId`。
3. 将清单变更合并或推送到 `publishing` 分支。
4. `Publish Workshop` workflow 生成发布矩阵，并逐个 Mod 下载 Release zip、准备发布目录、生成 VDF、调用 SteamCMD。

纯 YAML 格式、排序、注释变化不会发布。删除清单条目只表示不再由本仓库发布该 Mod，不会触发 Steam Workshop 更新。

需要重跑失败的发布时，使用 GitHub Actions 对同一次 workflow run 的 rerun 功能。不要为了重跑而制造一条新的发布路径。

## 变更识别

发布矩阵只包含当前清单中新增或发布选择器发生变化的条目。`mods` 下的 key 是清单中的资源地址；同一 id 下的有效 `repository`、`fileId` 或 `tag` 任一变化都会触发该 Mod 更新。

顶级 `repository` 变化只影响继承它的条目；显式填写了 `mods.<id>.repository` 且有效仓库没有变化的条目，不会因为顶级 `repository` 变化进入矩阵。

基线规则：

- 普通推送以推送前 ref 的清单作为基线。
- 首次创建 `publishing` 分支时，如果本次推送的第一个 commit 有唯一父提交且父提交包含清单，workflow 会以父提交清单为基线。
- 首次创建 `publishing` 分支时，如果第一个 commit 没有父提交，或它的唯一父提交不包含清单，当前清单只建立初始化基线，不发布。
- 无法解析首个提交、首个提交有多个父提交、无法解析基线提交，或基线提交不包含清单文件时，workflow 会失败而不是推断变化。

## Release 产物契约

每个清单条目指向一个 GitHub Release。workflow 使用 GitHub CLI 下载匹配 `*.zip` 的 asset；Release 必须有且只有一个 zip asset。

zip 解压后的发布目录由唯一的 `Config.Lua` 决定。该目录会作为 SteamCMD VDF 的 `contentfolder`，因此 Mod 的实际发布内容必须和 `Config.Lua` 放在同一目录边界内。

工具读取 `Config.Lua` 中这些字段：

- `Title`：Workshop 标题，必填。
- `FileId`：Workshop published file id，必填，用于和清单 `fileId` 做一致性校验。
- `Description`：Workshop 简介，可为空。
- `ChangeLog`：Workshop 更新说明，可选。
- `Cover`：默认预览图路径，可选。
- `WorkshopCover`：Workshop 专用预览图路径，可选，优先于 `Cover`。
- `Visibility`：Workshop 可见性，可选，必须是 SteamCMD 支持的 `0`、`1`、`2` 或 `3`。

如果声明了预览图路径，该文件必须存在。`Settings.Lua` 默认被视为本机玩家设置风险，发布 workflow 会拒绝包含它的发布目录。

Mod 自身版本随发布内容中的 `Config.Lua` 一起交付；发布清单不重复维护版本，也不约束 tag 必须携带版本语义。重复版本、重复 tag 或重复内容不由本仓库识别；只要清单和 Release 产物满足约定，workflow 会调用 SteamCMD。

## GitHub Environment

发布 workflow 需要 GitHub Environment `steam-workshop`。建议把它设为需要人工审批的受保护环境，并配置这些 secrets：

- `STEAM_USERNAME`：Steam 发布账号。
- `STEAM_CONFIG_VDF`：已通过 Steam Guard 信任后的 SteamCMD `config.vdf` 文件内容。该值等同登录凭据，必须放在受保护环境 secret 中。
- `RELEASE_REPOSITORY_TOKEN`：可选。Release 仓库为私有或默认 `github.token` 无权读取时提供。

`STEAM_CONFIG_VDF` 应来自受控机器上已完成 Steam Guard 验证的 SteamCMD。用发布账号登录一次 SteamCMD 并完成验证后，复制该 SteamCMD 配置目录下 `config.vdf` 的全文作为 secret；不要把该文件提交到仓库或写入任何 CI cache。

CI 发布时只执行 `+login <username>`，不传入账号密码。再次用密码登录会绕过已信任的无密码路径，并可能触发新的 Steam Guard 验证。

SteamCMD 运行文件和认证状态分开处理：

- `Publish Workshop` workflow 使用 GitHub 官方 `actions/cache` 缓存 SteamCMD 运行文件，并用 shell 安装 Linux 运行依赖、下载官方 SteamCMD tarball、完成首次初始化。
- 认证状态不进入 cache；发布前，workflow 会把 `STEAM_CONFIG_VDF` 写入临时 SteamCMD HOME 下的 `Steam/config/config.vdf`，未配置该 secret 时发布失败。
- 发布后，workflow 会删除临时 SteamCMD HOME 和可能残留在 SteamCMD 运行目录中的旧认证状态。
- workflow 不会把 CI 中可能更新的 `config.vdf` 写回 secret；如果 SteamCMD 要求重新验证，重新在受控机器登录并替换 `STEAM_CONFIG_VDF`。

## 本地诊断

模板使用者通常不需要直接运行 CLI。需要在本地复现清单解析、Release zip 解压或 VDF 生成问题时，使用 [tools/README.md](../tools/README.md) 中的命令示例。
