# AI 协作指令

## 强制规则

### 1. 每轮必须更新 CHAT-LOG.md
在每轮对话结束前，必须将本轮内容追加到 `.ai/CHAT-LOG.md`。

### 2. 开始工作前必须确认当前状态
1. 读取 `.ai/project/STATUS.md` 了解项目整体状态
2. 根据当前任务，读取相关模块的 `600-状态.md`
3. 确认接下来要做什么

### 3. 完成任务后同步更新文档

> 拥抱实事求是，设计和计划应随着真实执行过程中认知的深入而同步演进。

1. 更新对应模块的 `600-状态.md`
2. 如发现原设计需调整，同步更新 `200-开发设计.md`
3. 如发现计划需调整，同步更新 `400-开发计划.md`
4. 如有必要，更新 `.ai/project/STATUS.md`

### 4. 代码变更必须可验收
1. 参考模块的 `300-验证设计.md` 了解验收标准
2. 选择合适的验证手段
3. 执行验证并记录结果

### 5. 偏离计划需要先沟通
如果实现过程中发现需要偏离计划，先与用户/管理者确认。

---

## 项目结构

```
.ai/
├── AI-INSTRUCTIONS.md          ← 你正在读的文件
├── CHAT-LOG.md                 ← 时间线流水账
├── project/                    ← 项目级管理
│   ├── OVERVIEW.md             ← 项目概述
│   ├── ROADMAP.md              ← 路线图
│   └── STATUS.md               ← 项目级状态汇总
└── modules/                    ← 模块级管理
    ├── core-main-window/
    ├── core-image-annotation/
    ├── core-image-composite/
    ├── core-hotkey/
    └── core-history/
```

---

## 协作框架

本项目采用**多智能体协作框架**进行开发。

协作相关文档在 `.collab/` 目录（Git Worktree，检出 collab 分支）。

详见 `.collab/prompts/` 目录下的协作协议。
