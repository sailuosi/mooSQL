# SQLBuilder 两级编译（SQLMold）方案

> 落地：`pure/src/ado/builder/mold/`、`SQLBuilder.mold.cs`  
> 衔接：[Queryable低性能深度分析与优化方案.md](./Queryable低性能深度分析与优化方案.md)

## 目标

循环内同一形状的链式调用 → L1 模版一次 + L2 反复填参（接近 `string.Format`）。

## 生命周期

- 每个 `SQLBuilder` **常驻**一个 `MoldSession`（`init` / 字段初始化）。
- `clear()`：`MoldSession.Clear()`（清空 Mask/Vars，**不销毁**会话）+ 清 `_prebuiltSelectCmd`。
- **无需** `moldReuse`；默认可缓存路径走 Mold L2。
- `pin` / `where(string)`：传入的是**固定 SQL**（防注入约定：禁止动态拼接），不视为形参，**不改变**缓存策略。
- 子查询 `getBrotherBuilder`：与父级共用事务、`ps` 与 **同一 `MoldSession`**（形参并入同一 PathKey）。
- `union` 多分支：暂不走 Mold L2，回退现行拼装。

## 掩码（判定前建档）

入口在 null / `opened` / `paraRule` **之前** `BeginPara`，分支：

| MaskBits | 含义 |
|----------|------|
| 0 | 未纳入 SQL（跳过） |
| 1 | 标量纳入 |
| 2 | whereIn 列表（+ arity/chunks） |
| 3 | Format（模板指纹 + Present 位） |

## Format

`selectFormat` / `fromFormat` / `joinFormat` / `whereFormat` 与 whereIn 一样：`ParaMold` + `Processor`（`SqlMoldFormatExpand`），L2 展开；null 实参 → SQL ` null `（Present 分键）。

## 用法

```csharp
foreach (var id in ids)
{
    db.useSQL()
        .select("id,name").from("users")
        .where("id", id)
        .whereIn("tag_id", tags)
        .queryRow<User>();
}
```

## 测试

`Tests/TestBug/src/TestPure/SQLBuilderMoldTests.cs`  
Loop：`MooSqlBuilderTest.testQueryLoop`（默认启用 Mold）。
