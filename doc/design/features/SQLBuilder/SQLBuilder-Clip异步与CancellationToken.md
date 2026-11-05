# SQLBuilder / SQLClip · 异步与 CancellationToken

> 状态：**已落地**（2026-11-05；修订：统一 `CancellationToken cancellationToken = default`）  
> 用途：供 sailflow 引擎异步化阶段 2 引用；与同步 API **双轨并存**。  
> 溯源：sailflow `doc/sail/草案-引擎异步化.md` §6.3 / 阶段 1.5。

---

## 1. 结论

| 表面 | API | 令牌 |
|------|-----|------|
| SQLBuilder | `doInsertAsync` / `doUpdateAsync` / `doDeleteAsync` / `doMergeIntoAsync` / `queryAsync` / `queryUniqueAsync` / `queryScalarAsync` / `queryRowAsync` / `queryPagedAsync` / `existAsync` / `exe*Async` / `doUpdateBatchAsync` 等 | **单签名** `CancellationToken cancellationToken = default`（无双轨重载） |
| SQLClip | `queryListAsync` / `queryUniqueAsync` / `doUpdateAsync` / `doDeleteAsync` | 同上，默认 `default` |
| 同步 API | 不变 | — |

取消语义：入口 `ThrowIfCancellationRequested`；写路径与实体多列查询在 `OpenAsync` / `ExecuteReaderAsync` / `ExecuteNonQueryAsync` 传播令牌。

---

## 2. 调用示例

```csharp
// 可不传令牌（兼容旧调用）
await kit.doInsertAsync();
await kit.doUpdateAsync();

// 显式传令牌
await kit.doInsertAsync(ct);
await kit.queryAsync<MyRow>(ct);

// Clip
var list = await clip.select(t).queryListAsync(ct);
await clip.doUpdateAsync(ct);
```

---

## 3. 已知边界

| 项 | 说明 |
|----|------|
| 单列 `queryList` / 尾投影 | `queryListAsync` 入口校验 `ct` 后仍可能同步物化 |
| `queryUnique`/`queryScalar` 底层部分路径 | 入口校验 + 部分仍走无 ct 的 ADO 辅助；后续可继续下沉 |
| DataTable `ExecuteQueryAsync(ct)` | Reader + `Load`；Fill 不可取消段仍同步 |
| 双写路径 | `exeNonQueryAsync(ct)` 遇 DualWrite 仍走同步双写 |

---

## 4. 验收对照

1. ✅ Clip/Builder 常用读/写可 `await` 且可传 `ct`（默认值兼容无参调用）。  
2. ✅ 同步 API 并存。  
3. ✅ SQLite 执行测用例（见 `SQLClipSqliteExecutionTests`）。

---

## 5. 引擎侧接续

sailflow Manager 热点调用 `*Async(ct)`；**勿**绕过 Clip 私自拼 Builder。
