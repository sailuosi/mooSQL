


linq功能点纪要


LINQ模型  到 SQL模型  的编译：

	ExpressionBuilder.Build<T>()


SQL模型对象 到 SQL脚本的编译：

	DataConnectRunner.TranslateCmds()



关键类


	SQLCmds   -- 承载编译好的可执行的SQL脚本

	QueryInfo -- 承载 SQL模型对象信息

	DataConnectRunner -- 执行SQL模版到SQL脚本的翻译；

	ExpressionBuilder -- 执行Linq Expression 对象 到 SQL模型 的翻译；

	Query -- 承载一组 QueryInfo

	Query<T> -- linq编译的入口，调用ExpressionBuilder 执行翻译，输出 SQL模型对象 Query

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