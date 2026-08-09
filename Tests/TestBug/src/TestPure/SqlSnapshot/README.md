# SQLBuilder toXxx SQL 快照

以当前稳定 `SQLBuilder` 的 `toXxx` 出口 SQL 为基准，**不执行**数据库。

## 文件

| 文件 | 说明 |
|------|------|
| `SQLBuilderSqlSnapshotCatalog.cs` | 用例目录（where / join / format / 子查询 / CTE / DML / merge…） |
| `SQLBuilderSqlSnapshotTests.cs` | Theory 断言 + `CaptureAllBaselines` |
| `baselines.sqlite.json` | 固化的 SQL 文本（权威基准） |
| `SqlSnap.cs` | 构建 / 导出 / 比对辅助 |

## 命令

```powershell
# 全量比对（日常）
dotnet test Tests/TestBug/mooSQL.Pure.Tests.csproj -f net8.0 --filter "FullyQualifiedName~SQLBuilderSqlSnapshotTests&FullyQualifiedName!~CaptureAllBaselines"

# 全量重写基准（确认当前输出即为正确时）
dotnet test Tests/TestBug/mooSQL.Pure.Tests.csproj -f net8.0 --filter "FullyQualifiedName~SQLBuilderSqlSnapshotTests.CaptureAllBaselines"

# 单条新增用例缺键时：直接跑 Theory，会自动把新 SQL 写入 json
```

参数前缀固定为 `setSeed("s_")`，保证参数名稳定。
