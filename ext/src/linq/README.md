# LINQ 模块文档索引

> **全景分析（2026-06）** → [LINQ全景分析与项目对比.md](./LINQ全景分析与项目对比.md)  
> **双访问器对齐 FastLinq（迁移清单）** → [双访问器对齐FastLinq-迁移清单.md](./双访问器对齐FastLinq-迁移清单.md)  
> **Phase 2 架构详解** → [src/README.md](./src/README.md)

---

## 历史纪要（Phase 1 前）

linq功能点纪要


LINQ模型  到 SQL模型  的编译：

	ClauseCompiler.Build<T>()


SQL模型对象 到 SQL脚本的编译：

	DataConnectRunner.TranslateCmds()



关键类


	SQLCmds   -- 承载编译好的可执行的SQL脚本

	QueryInfo -- 承载 SQL模型对象信息

	DataConnectRunner -- 执行SQL模版到SQL脚本的翻译；

	ClauseSqlTranslator + ClauseCompiler -- LINQ Expression → SentenceBag（双访问器 + StatementExpression）；

	Query -- 承载一组 QueryInfo

	QueryMate / EntityVisitCompiler -- linq编译入口，调用 ClauseCompiler 输出 SentenceBag

	QueryRunner -- 执行SQL模型的查询，利用数据连接，使用DataConnectRunner翻译后，执行查询，并返回结果。

	TableT   --  linq的核心成员 Queryable的对外暴露对象，这里它同时承担 Queryable和Provider的角色



职责混合问题：
	Query<T> 实际上同时承担了 SQL模型定义 和 编译 2个功能。编译的结果又放在自己的父类那里，让人很疑惑。

	因此，Query<T> 需要拆分，分为 编译部分，和编译好的模型，2种内容。

	分拆后：
		QueryProvider 将编译参数传递给 编译器，直接获取 编译好的 SQL模型结果
		编译器持有一组 编译配置。

	SQL模型包  -- SentenceBag
	编译配置   CompileDependence
	编译环境   ProvideContext