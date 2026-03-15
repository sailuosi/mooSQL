# mooSQL.Pure 单元测试项目

## 项目概述

本项目为 mooSQL.Pure 核心库提供完整的单元测试覆盖，使用 xUnit 测试框架，FluentAssertions 进行断言，Moq 进行 Mock 对象创建。

## 项目结构

```
Tests/
├── TestHelpers/           # 测试辅助类
│   ├── TestDatabaseHelper.cs    # 数据库实例创建辅助类
│   └── TestEntity.cs            # 测试用实体类
├── SQLBuilderTests.cs     # SQLBuilder 单元测试
├── SQLClipTests.cs        # SQLClip 单元测试
└── README.md              # 本文件
```

## 测试框架

- **xUnit 2.6.2**: 测试框架
- **FluentAssertions 6.12.0**: 流畅的断言库
- **Moq 4.20.70**: Mock 对象框架
- **coverlet.collector 6.0.0**: 代码覆盖率收集

## 运行测试

### 使用 Visual Studio
1. 打开测试资源管理器（Test Explorer）
2. 运行所有测试或选择特定测试

### 使用命令行
```bash
dotnet test
```

### 生成代码覆盖率报告
```bash
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=opencover
```

## 测试覆盖范围

### SQLBuilder 测试
- ✅ 基础功能（创建、设置表、选择列）
- ✅ WHERE 条件（等于、大于、IN、LIKE、BETWEEN、IS NULL 等）
- ✅ ORDER BY 排序
- ✅ 分页功能
- ✅ INSERT 操作
- ✅ UPDATE 操作
- ✅ DELETE 操作
- ✅ 参数化查询
- ✅ JOIN 操作
- ✅ GROUP BY
- ✅ DISTINCT
- ✅ 事务管理
- ✅ 工具方法（clear、reset、ifs 等）

### SQLClip 测试
- ✅ 基础功能（创建、from、clear）
- ✅ SELECT 操作（单字段、多字段、表达式）
- ✅ WHERE 条件（Lambda 表达式、字段选择器）
- ✅ JOIN 操作（INNER、LEFT、RIGHT）
- ✅ ORDER BY 排序
- ✅ 分页功能
- ✅ DISTINCT
- ✅ 分支条件（sink、sinkOR、rise）
- ✅ 复杂查询组合

## 测试辅助类

### TestDatabaseHelper
提供创建测试用数据库实例的静态方法，支持多种数据库类型。

### TestEntity
提供测试用的实体类：
- `TestUser`: 用户实体
- `TestOrder`: 订单实体

## 注意事项

1. **不连接真实数据库**: 测试使用内存数据库或模拟连接，不依赖真实数据库连接
2. **SQL 验证**: 测试主要验证 SQL 语句的构建是否正确，不执行实际查询
3. **扩展性**: 可以轻松添加新的测试用例和测试辅助类

## 添加新测试

1. 在相应的测试文件中添加新的测试方法
2. 使用 `[Fact]` 特性标记测试方法
3. 使用 FluentAssertions 进行断言
4. 遵循 AAA 模式（Arrange-Act-Assert）

## 示例

```csharp
[Fact]
public void MyNewTest()
{
    // Arrange
    var builder = TestDatabaseHelper.CreateSQLBuilder();
    
    // Act
    var cmd = builder.setTable("users")
        .select("id")
        .select("name")
        .where("id", 1)
        .toSelect();
    
    // Assert
    cmd.Should().NotBeNull();
    cmd.sql.Should().Contain("SELECT");
    cmd.sql.Should().Contain("WHERE");
}
```

## 持续改进

- [ ] 添加更多边界情况测试
- [ ] 添加性能测试
- [ ] 添加集成测试
- [ ] 提高代码覆盖率到 80% 以上
