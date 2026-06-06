# DbFunc 翻译矩阵维护规则

新增迁移至 Pure `SQLExpression` / `DbFuncRegistry` 的函数时：

1. 在 [`DbFuncRegistryBootstrap`](../../ext/src/linq/translator/DbFuncRegistryBootstrap.cs) 注册 SQL 模板
2. 在 [`DbFuncTranslationMatrixTests`](../TestExt/DbFuncTranslationMatrixTests.cs) 追加 **一条** compile-only 断言
3. 更新 [`ext/CHANGELOG.md`](../../ext/CHANGELOG.md)

运行：`dotnet test Tests/TestLinq.csproj -f net6.0 --filter "FullyQualifiedName~DbFuncTranslationMatrix"`
