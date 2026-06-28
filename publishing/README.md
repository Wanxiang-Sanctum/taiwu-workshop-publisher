# publishing

本目录维护 Steam Workshop 发布意图清单。`workshop.yml` 是提交驱动发布的唯一入口；当该文件的变更进入 `publishing` 分支后，发布 workflow 会比较推送前后的清单，并按变化生成发布矩阵。

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
- `mods.<id>.repository` 可选。单个 Mod 需要来自不同 Release 仓库时，用它覆盖顶级 `repository`。

空模板使用 `mods: {}`，表示当前没有发布目标；省略 `mods` 是清单错误。

如果 Mod id 或 tag 含有会影响 YAML 解析的字符，按 YAML 规则给该字符串加引号即可。重复 Mod id 会被视为清单错误。

## 变更识别

发布矩阵只包含当前清单中新增或发布选择器发生变化的条目。`mods` 下的 key 是清单中的资源地址；同一 id 下的有效 `repository`、`fileId` 或 `tag` 任一变化都会触发该 Mod 更新。顶级 `repository` 变化只影响继承它的条目；显式填写了 `mods.<id>.repository` 且有效仓库没有变化的条目，不会因为顶级 `repository` 变化进入矩阵。

纯 YAML 格式、排序、注释变化不会发布。删除清单条目只表示不再由本仓库发布该 Mod，不会触发 Steam Workshop 更新。

基线规则：

- 普通推送以推送前 ref 的清单作为基线。
- 首次创建 `publishing` 分支时，如果本次推送的第一个 commit 有唯一父提交且父提交包含清单，workflow 会以父提交清单为基线。
- 首次创建 `publishing` 分支时，如果第一个 commit 没有父提交，或它的唯一父提交不包含清单，当前清单只建立初始化基线，不发布。
- 无法解析首个提交、首个提交有多个父提交、无法解析基线提交，或基线提交不包含清单文件时，workflow 会失败而不是推断变化。

## 产物约定

每个清单条目指向一个 GitHub Release。该 Release 必须有且只有一个 zip asset。

zip 解压后必须能发现且只发现一个 `Config.Lua`；该文件所在目录就是 Steam Workshop 发布内容目录。发布 workflow 不理解 Mod 仓库源码结构，也不重新组包。

本仓库只支持更新已有 Steam Workshop item，不支持创建新 item。`mods.<id>.fileId` 必须和发布目录内 `Config.Lua` 的 `FileId` 一致；二者冲突时发布失败。

Steam Workshop VDF 的 `publishedfileid` 来自清单中的 `fileId`。标题、描述、封面、可见性和更新说明来自发布目录内的 `Config.Lua`。本工具读取 `Title`、`FileId`、`Description`、`ChangeLog`、`Cover`、`WorkshopCover` 和 `Visibility`；其中 `FileId` 只用于和清单 `fileId` 做一致性校验，其它字段随 Mod 内容发布，不由本清单解释。

重复版本、重复 tag 或重复内容不由本仓库识别；只要清单和 Release 产物满足约定，workflow 会调用 SteamCMD。

目标 Release 仓库按上述产物约定产出，并且本仓库具备读取 Release asset 和发布 Steam Workshop 的权限时，就可以用同一套 workflow 发布。
