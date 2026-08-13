# SQLClip 客户端尾投影 — 性能基线（P-base）

> 对应设计：[`../SQLClip-客户端尾投影.md`](../SQLClip-客户端尾投影.md) §6.5.2  
> 用途：实现/合并时对比纯列路径，防止非投影副作用拖垮性能。

## 环境元数据

| 项 | 值 |
|----|-----|
| 日期 | 2026-08-13 |
| 机器 | win32 10.0.26200（本机开发机） |
| TFM | net8.0 |
| DB | SQLite（`TestDatabaseHelper` / `test_users`，3 行过滤子集） |
| 提交 | 本地 WIP（实施客户端尾投影当期树） |
| 口径 | Stopwatch 微基准（非 BenchmarkDotNet）；暖机后 n=300 |

## 复现命令

```text
dotnet test Tests/TestBug/mooSQL.Pure.Tests.csproj -f net8.0 --filter "FullyQualifiedName~PerfSmoke_PureColumnAnonymous" --verbosity normal
```

控制台应出现：

```text
[SQLClipClientTail baseline] TFM=net8.0 n=300 Clip.Anonymous.meanUs=... Clip.Result.meanUs=...
```

正式跨 ORM 对比仍用：

```text
Tests/TestFast/dbTest → MooSqlClipTest（testQueryResult / testQueryAnonymousResult）
```

## 本机落盘核心耗时（实施期首测）

| 指标键 | 场景 | Mean（µs/次） | 备注 |
|--------|------|---------------|------|
| `Clip.Anonymous` | 纯列匿名 `select(() => new { Id,Name,Age,Email })` + `queryList` | **205.9** | 3 行 where 过滤；非 dbTest Take(100) |
| `Clip.Result` | 整表实体 `select(a)` + `queryList` | **336.0** | 同上过滤 |

## P1 复测（Reader 直读 + 委托缓存后，2026-08-13）

| 指标键 | Mean（µs/次） | 备注 |
|--------|---------------|------|
| `Clip.Anonymous` | **~203.5** | 相对 P-base 同档 |
| `Clip.Result` | **~178.8** | 同档（波动） |
| `Clip.TailG1` | **~292.6** | 字符串尾投影（Id+Length+ToLower+Trim） |
| Tail / Anon | **~1.44×** | 信息项；无死线 |

复现：同上命令，过滤器改为 `PerfSmoke_PureAndTail`（或整类 `SQLClipClientTailProjectionTests`）。

> 说明：本表为 **同一套 Stopwatch 口径** 的门禁基线。与 `doc/test/dbTest-ORM基准测试总结.md` 中 Clip Result ~339µs / Anonymous ~259µs **量级可对照，但不可直接替换**（数据量、连接、BDN 统计不同）。

## 比对规则（提醒）

| 场景 | 规则 |
|------|------|
| 纯列 Anonymous / Result | 相对本表 Mean：同档；稳定回退 >~10% 需调查 §6.4 分流 |
| 尾投影 G1 | 另记倍率；不套用上表红线 |

## 变更记录

| 日期 | 说明 |
|------|------|
| 2026-08-13 | 首版 P-base：尾投影实现同期落盘（G1–G4 已绿） |
