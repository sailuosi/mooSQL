# LINQ 编译流程（已迁移）

> **本文档已过时。** Phase 2 重构后，编译与执行已分离，请参阅上级目录文档：
>
> **[../README.md](../README.md)** — 当前三层架构（Compile → SentenceBag → SentenceExecutor）

## 历史说明（Phase 1）

旧设计分两阶段：

1. **BuildSequence** — Expression → `SelectQueryClause`（Statement）
2. **BuildQuery** — 投影 finalization + `BuildMapper` + `SetRunQuery`（已删除）

当前仅保留第一阶段编译；执行统一走 `SentenceExecutor` + `SQLBuilder.query<T>()`。
