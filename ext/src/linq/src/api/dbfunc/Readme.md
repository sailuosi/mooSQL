# mooSQL DbFunc — LINQ 可翻译数据库函数

`DbFunc` 是 Ext LINQ 在 Lambda 表达式中使用的**数据库函数静态类**，对标 EF 的 `EF.Functions`、SqlSugar 的 `SqlFunc`。

## 用法

```csharp
using mooSQL.linq;

var q = db.useQueryable<Order>()
    .Where(o => DbFunc.Between(o.Amount, 100, 500))
    .Where(o => o.Name != null && DbFunc.Like(o.Name, "%test%"));
```

编译时，`ClauseFieldVisitor` / `MemberTranslatorResolver` 将 `DbFunc.*` 调用翻译为 Pure 层 `SelectQueryClause` 中的 `FunctionWord` / `ExpressionWord`。

## 属性

| 属性 | 说明 |
|------|------|
| `[DbFunc.Expression("...")]` | 自定义 SQL 表达式模板 |
| `[DbFunc.Function("name")]` | 映射到数据库函数名 |
| `[DbFunc.Extension(...)]` | 复杂扩展（如 `Between`） |

`DbFuncExpressionAttribute` 是 `[DbFunc.Expression]` 的推荐别名，长期将合并进 Pure 层 `SQLExpression` 方言实例。

## 与 Pure 的关系

- **当前**：函数 stub 在 `ext/src/linq/src/api/dbfunc/`，属性与翻译基础设施在 `api/translation/`
- **目标**：常用函数逐步迁入 `pure/src/ado/data/dialect/SQLExpression.*`，Ext 仅查 Pure 注册表

### 已 registry-first（Bootstrap 注册 + 矩阵覆盖）

Like、Between/NotBetween、In/NotIn、Substring、Concat、DateAdd、Length、Lower/Upper/Trim、**NullIf**（`IsNullIfPredicate`，无 `[Expression]` R17）、**Coalesce**（无 `[Expression]` R16）、Count/Sum/Avg、RowNumber、**DateDiff**（`IsDateDiffPredicate` + 方言 `dateDiff*`；**全方言 Builder 已删 R16**）。

`api/dbfunc/` 删除（D.9）进行中：**DateDiff 全方言 registry-only**；**Coalesce.cs 已物理删除**（R16）；**NullIf 无 Expression**（R17）；Between/NotBetween/OrderItemBuilder 已删。

## 自定义扩展

```csharp
public static partial class MyDbFunc
{
    [DbFunc.Expression("NULLIF({0}, {1})", PreferServerSide = true)]
    public static T? NullIf<T>(T? value, T compareTo) where T : struct
        => value;
}
```

详见 [`ext/src/linq/core/ClauseCompile-Glossary.md`](../../core/ClauseCompile-Glossary.md)。
