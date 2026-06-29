# Taiwu Workshop Publisher

[![zread](https://img.shields.io/badge/Ask_Zread-_.svg?style=flat&color=00b0aa&labelColor=000000&logo=data%3Aimage%2Fsvg%2Bxml%3Bbase64%2CPHN2ZyB3aWR0aD0iMTYiIGhlaWdodD0iMTYiIHZpZXdCb3g9IjAgMCAxNiAxNiIgZmlsbD0ibm9uZSIgeG1sbnM9Imh0dHA6Ly93d3cudzMub3JnLzIwMDAvc3ZnIj4KPHBhdGggZD0iTTQuOTYxNTYgMS42MDAxSDIuMjQxNTZDMS44ODgxIDEuNjAwMSAxLjYwMTU2IDEuODg2NjQgMS42MDE1NiAyLjI0MDFWNC45NjAxQzEuNjAxNTYgNS4zMTM1NiAxLjg4ODEgNS42MDAxIDIuMjQxNTYgNS42MDAxSDQuOTYxNTZDNS4zMTUwMiA1LjYwMDEgNS42MDE1NiA1LjMxMzU2IDUuNjAxNTYgNC45NjAxVjIuMjQwMUM1LjYwMTU2IDEuODg2NjQgNS4zMTUwMiAxLjYwMDEgNC45NjE1NiAxLjYwMDFaIiBmaWxsPSIjZmZmIi8%2BCjxwYXRoIGQ9Ik00Ljk2MTU2IDEwLjM5OTlIMi4yNDE1NkMxLjg4ODEgMTAuMzk5OSAxLjYwMTU2IDEwLjY4NjQgMS42MDE1NiAxMS4wMzk5VjEzLjc1OTlDMS42MDE1NiAxNC4xMTM0IDEuODg4MSAxNC4zOTk5IDIuMjQxNTYgMTQuMzk5OUg0Ljk2MTU2QzUuMzE1MDIgMTQuMzk5OSA1LjYwMTU2IDE0LjExMzQgNS42MDE1NiAxMy43NTk5VjExLjAzOTlDNS42MDE1NiAxMC42ODY0IDUuMzE1MDIgMTAuMzk5OSA0Ljk2MTU2IDEwLjM5OTlaIiBmaWxsPSIjZmZmIi8%2BCjxwYXRoIGQ9Ik0xMy43NTg0IDEuNjAwMUgxMS4wMzg0QzEwLjY4NSAxLjYwMDEgMTAuMzk4NCAxLjg4NjY0IDEwLjM5ODQgMi4yNDAxVjQuOTYwMUMxMC4zOTg0IDUuMzEzNTYgMTAuNjg1IDUuNjAwMSAxMS4wMzg0IDUuNjAwMUgxMy43NTg0QzE0LjExMTkgNS42MDAxIDE0LjM5ODQgNS4zMTM1NiAxNC4zOTg0IDQuOTYwMVYyLjI0MDFDMTQuMzk4NCAxLjg4NjY0IDE0LjExMTkgMS42MDAxIDEzLjc1ODQgMS42MDAxWiIgZmlsbD0iI2ZmZiIvPgo8cGF0aCBkPSJNNCAxMkwxMiA0TDQgMTJaIiBmaWxsPSIjZmZmIi8%2BCjxwYXRoIGQ9Ik00IDEyTDEyIDQiIHN0cm9rZT0iI2ZmZiIgc3Ryb2tlLXdpZHRoPSIxLjUiIHN0cm9rZS1saW5lY2FwPSJyb3VuZCIvPgo8L3N2Zz4K&logoColor=ffffff)](https://zread.ai/Wanxiang-Sanctum/taiwu-workshop-publisher)

Taiwu Workshop Publisher 是把太吾绘卷 Mod 的 GitHub Release 产物发布到 Steam Workshop 的模板仓库。它维护发布清单、GitHub Actions workflow 和 C# 辅助工具；不维护 Mod 源码、组包规则或 GitHub Release 打包流程。

这个仓库有两个读者入口：

- 使用模板的开发者：维护 `publishing/workshop.yml`、GitHub Environment 和 Release 产物约定。见 [publishing/README.md](publishing/README.md)。
- 维护模板的开发者：维护 workflow、CLI、构建检查和模板文档。先读 [CONTRIBUTING.md](CONTRIBUTING.md)，CLI 细节见 [tools/README.md](tools/README.md)。

## 发布模型

标准发布路径只有一条：把 `publishing/workshop.yml` 的变更合并或推送到 `publishing` 分支。`Publish Workshop` workflow 会比较当前清单和基线清单，只处理新增清单条目或发布选择器发生变化的条目；随后下载指定 GitHub Release asset，解压出唯一的 `Config.Lua` 所在目录，生成 SteamCMD workshop item VDF，并在受保护的 GitHub Environment 中调用 SteamCMD。

发布信源分工固定如下：

- `publishing/workshop.yml` 只声明要发布的 Mod、Steam Workshop file id、Release 仓库和 Release tag。
- 清单指向的 Release 仓库负责提供可发布 zip；Mod 源码和组包规则留在该仓库或它自己的构建链路中。
- 发布目录中的 `Config.Lua` 提供 Workshop 标题、简介、封面、可见性和更新说明。
- SteamCMD VDF 的 `publishedfileid` 来自清单中的 `fileId`；`Config.Lua` 的 `FileId` 只用于一致性校验。

本仓库只支持更新已有 Steam Workshop item，不支持创建新 item。每个 Mod 必须先在 Steam Workshop 侧拥有非 0 的 file id，并且清单 `fileId` 必须和发布内容 `Config.Lua` 中的 `FileId` 一致。

## 不在范围内

本仓库不负责这些事项：

- 构建、打包或测试 Mod 源码。
- 创建 GitHub Release 或决定 Release tag 命名规则。
- 创建新的 Steam Workshop item。
- 判断重复版本、重复 tag 或重复内容是否应该发布。
- 自动维护 Workshop tags、依赖关系或太吾写入的 custom metadata。

当前 SteamCMD 路径只维护 Steamworks 文档列出的 VDF 字段。Workshop tags、依赖关系和太吾写入的 custom metadata 变化时，需要用太吾内置上传流程或其它明确支持对应 Steam API 的工具处理。

## 仓库结构

- `publishing/`：模板使用者的发布入口，包含 `workshop.yml` 和发布清单说明。
- `.github/workflows/publish-workshop.yml`：提交驱动的 Steam Workshop 发布 workflow。
- `.github/workflows/pull-request.yml`：pull request 验证 workflow。
- `CONTRIBUTING.md`：模板维护流程、验证命令和文档同步边界。
- `tools/Taiwu.WorkshopPublisher.Cli`：workflow 与本地诊断共用的 C# CLI。
- `repo.proj`：仓库级检查、格式化和工具安装入口。

## 分支和 workflow

`master` 是工具、文档和 workflow 模板的开发分支；`publishing` 是生产发布分支。`publishing` 必须包含完整仓库内容，不是只放清单的孤立分支。workflow 使用 `publishing` 分支上的 CLI 和 workflow 定义运行。

`Pull Request` workflow 对指向 `master` 或 `publishing` 的 pull request 执行仓库验证。指向 `publishing` 的 pull request 只校验；合并后进入 `publishing` 的 push 才会触发发布。

`Publish Workshop` workflow 只由推送 `publishing/workshop.yml` 到 `publishing` 触发。workflow 无法解析基线时会失败，而不是无基线全量发布。

## 维护入口

仓库维护流程、验证命令、依赖更新和文档同步规则见 [CONTRIBUTING.md](CONTRIBUTING.md)。需要本地复现 CLI 行为时，使用 [tools/README.md](tools/README.md) 中的命令示例。

SteamCMD workshop VDF 和 `workshop_build_item` 语义以 Steamworks 文档为准：

- https://partner.steamgames.com/doc/features/workshop/implementation
- https://developer.valvesoftware.com/wiki/SteamCMD
