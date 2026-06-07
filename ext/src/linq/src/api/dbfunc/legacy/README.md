# Legacy 方言属性归档说明

Phase 4 清理从 `DbFunc.cs` / `DbFunc.Strings.Legacy.cs` / `DbFunc.Math.cs` 移除了 **TestLinq 未覆盖** 的 `[Function]` / `[Expression]` 方言 overload，主要为：

| 方言 | 已移除示例 | 说明 |
|------|-----------|------|
| **ClickHouse** | `trimLeft`/`trimRight`、`leftUTF8`/`rightUTF8`、Stuff、`now`、`roundBankers`、`cosh`/`sinh`、Log 公式 | 主方言走 registry 或默认 `[Function]`；ClickHouse 专用名无矩阵 |
| **SapHana** | `Ceil`、`Ln`、`Log(10,…)`、Truncate `ROUND_DOWN`、`Lpad` Space、`CURRENT_UTCTIMESTAMP` | DateDiff 等仍由 `SapHanaExpress` 提供 |

**Registry 化移除的方法级属性：**

| API | 已移除 | 替代 |
|-----|--------|------|
| **CharIndex** | DB2/MySQL/Firebird `Locate`/`Position` `[Function]` | `DbFuncRegistry` + 方言 `charIndex()` |
| **IsNullOrWhiteSpace** | 11 个 `[Extension]` Builder 类 | registry + 方言 `isNullOrWhiteSpace()`（MSSQL/Oracle/Npgsql 等 Express override） |

**恢复方式**：若产品需重新支持某方言，优先在对应 `*Express.cs` 增加 `SQLExpression` 片段并在 `DbFuncRegistryBootstrap` 注册；其次才恢复方法级属性 overload。

**主方言**（SQLite / SQL Server / MySQL / PostgreSQL / Oracle + 矩阵覆盖）：保留 `[Function]` fallback 或 registry-first 无属性路径。
