---
outline: deep
---

# 更新迭代记录

## v3 .net6.0 全面支持版

### 更新 v8.0.0.2 2026-1-22
- 废弃【obsolute】
    - 【不再推荐】废弃错别字注册实体解析器方法useEnityAnalyser，建议改为useEntityAnalyser
    - 【不再推荐】BatchSQL类下DBInstance属性，建议改为 DBLive引用；rows属性废弃，不再使用
- 新增功能(SQLBuilder)
    - 增加按主键查存在 findIsExist方法
    - queryRowT 方法落实，等于queryUnique方法
    - SQLBuilder下update/insert/save系列扩展更改为独立执行环境，不再干扰调用者上下文
    - 增加 sinkNot 、sinkNotOR等2个方法，用于定义否定盒子
    - 增加 whereNotLikeOrNull、whereNotLikeLeftOrNull、whereNotInOrNull、whereIsOrNull、whereVsOrNull 等组合可空的语法糖

- 其它优化
    - BatchSQL 类，增加print方法，增加通用事务的支持，允许SQLBuilder事务贯通
    - 增加ClientBuilder的useDialect 方法，以支持便捷的方言注册
    - BaseClientBuilder配置类，增加 useEntityTranslate注册，调整逻辑，允许二级配置可以无序注册
    - 仓储自定义查询，操作符op增加对notin、isnul、notnull、between的支持
    - EntityTranslator 实体转义器增加切面，允许对实体的插入、更新、删除的SQL生成前后插入自定义逻辑
    - DataTable的groupBy 增加二阶聚合重载，允许按2个属性聚合成二层字典。

### 更新 v8.0.0.1 2026-1-8
- bugfix
    - 修正批量写入在大数据量循环时下偶尔写入错误的问题
    - 权限，修正权限为空时的空权限事件执行不一致问题，修正直接绑定权限时的上下级包含错误问题
- 新功能
    - 表达式字段名解析增加缓存策略，增强顶级实体翻译器Translator
    - 新特性：导航加载功能、导航保存功能，includeHis、includeNav方法
    - 驱动层：增加批命令执行功能，增加DBLive的ExeNonQuery批命令版本重载
    - 仓储，增加SaveRange方法
    - 集合扩展增加groupByKV、writeTo方法，增加string.formatSQL扩展，增加DataTable扩展getFieldValues、groupBy，增加reduce/sum扩展，
- SQLBuilder相关
    - 实体扩展，增加save、toSave的批量版本，增加removeByIds，增加findList2个重载，增加findTreeParentOIDs向上查找方法，
    - 增加selectFormat、fromFormat、joinFormat、countLong、whereNotLikeLeft
    - 参数Para增加 toRawSQL方法，便于输出SQL
    - SQLClip，增加distinct方法
- 其它优化
    - excel导入，增加bool类型列的识别判定
    - 树查询构造器TreeSQLBuilder ，增加权限支持


### 更新2025-11-26 
- bugfix
    - 修正fastlinq下表达式解析对变量的处理（识别外部变量）。
- 新增功能
    - 增加表名自定义功能，支持仓储、Clip，允许自定义读取表
    - 增加连接位版本号概念，同个实体匹配不同数据库
    - 导入增加历史数据钩子，用于自定义历史数据加载
    - find系列增加翻页扩展、字段值扩展
    - BatchSQL增加Clip的扩展
    - 增加DB下自定义参数写入cmd逻辑钩子
    - 增加数据库配置下慢SQL的监听功能，增加配置、监听、执行逻辑，增加实例构建时刻多个事件的注册
- 优化
    - 增加对DBNull的特殊兼容，等效为数据库null
    - 合同单元格读取兼容，增强公式的读取

### 更新2025-10-23 (实体SQL增强)
- bugfix
    - 修正SQLServer下带事务执行Bulk插入时问题
- SQLBuilder增强
    - 增加 EntitySaveBase 一组类，用于实体类保存时更强的配置，对标 updatable() 
    - SQLBuilder下新增 updatable、insertable、deleteable一组方法
    - 统一客户端侧配置类，命名为 Client属性。
    - 扩展SQLBuilder.ifs方法

### 更新2025-10-18 (BulkBase增强)
- 增加实体类Bulk插入功能，支持 BulkBase.addList方法
- BulkBase增加事务支持
- 增加useBulk工厂方法
- 增加 SQLBuilder.insertList扩展方法
- 增加一组 groupBy扩展方法
- 增加SQLBuilder.containSetColumn() 用于检测是否set了某个字段

### 更新2025-9-15
- 完善实体类的排序特性
- SQLClip增加Join子查询的支持。
- 扩展SQLClip where语句，
- 仓储列表查询解析增加OnBuildSQL自定义钩子
- 增加默认的树查询构造器 TreeSQLBuilder
- SQLBuilder自动清理的增强，SQLBuilder.configClear()方法
- 增强实体类转SQL时关键字列的自动包裹
- 增加SQLBuilder.whereNotBetween()方法


### 更新2025-8-31
- 增加SQLBuilder下find+modify+remove 系列快捷使用的SQLClip扩展方法。
- SQLClip增加Join子查询的支持。
- 增强匿名类的解析
- SQLBuilder增加withSelect语句，增强with as的便利性
### 更新2025-8-20
- merge into语句的支持增强，允许在mysql中自动降级为update/insert语句
- 增加merge into专用SQL构造器，支持更复杂的写法。
- 优化update from语句的构造过程，优化mysql下的兼容性。
- SQLBuilder下的pivot增强，更改为允许多次转置调用。
- 修复自带特性的忽略列的解析问题。

### 更新2025-8-8

- 增强翻页语句的构建逻辑，加入数据库版本号的判断，对高版本数据库使用更优的SQL语句,适配了mySQL/sqlServer/postgreSQL等库。

### 更新2025-7-31

- 参数化paramter增强，增加对mybatis类似语法#{id} 的SQL模版支持，调整内部参数化方式，增加SQL对异构库执行的参数化前缀兼容支持

### 更新2025-7-28

- Clip表达式，新增字段解析缓存的支持，多数场景下字段解析由3700Ticks减少到300Ticks左右，提升10倍

### 更新 2025-7-15

- 底层执行层的全面事务支持，执行器DBExecutor独立。
- 业务侧SQLBuilder添加全面的事务支持：beginTransaction,commit,rollback

### 更新 2025-7-7 ！非兼容性变更

- 缓存类由易冲突的ICache 更名为 SooCache。
- LogLevel类更名为 LogLv
- 增加新特性 【SooLink】用于支持外键关联类的定义
- 仓储、clip功能大幅扩展，增加一组实用方法，如仓储的GetTreeList/GetChildList

### 更新 2025-6-15

- 增加Client工厂，统一核心工作类如SQLBuilder/仓储、clip的获取方式从工厂获取

- 增加大模型调用功能LLMCash

### 更新 2025-6-2

- 完成扩展内容中的linq外延清理，移除无用的linq解析。

### 更新 2025-5-22

- 新增SQLClip工作类，实体类模式下SQLBuilder

### 更新 2025-1-3

- 增加仓储功能 SooRepository

- 增加工作单元 SooUnitOfWork

- 增加fastLinq功能，工作类DbBus

### 更新 2024-11-12

- 表达式函数支持，支持Queryable

### 更新 2024-10-16

- 增加主从库功能

- 权限增强

- 实体解析功能增强

### 更新 2024-9-21

- 打通实体类解析功能

- 打通查询执行器

- 兼容sqlsugar特性

### 更新 2024-7-10

- CTE支持

- 启动linq功能开发

### 更新 2024-4-20

- where条件功能增强

- 新增权限通用功能，(词条、资源、访客)

- 新增配置的链式语法

### 更新 2023-2-3
- 适配mysql语法方言
- union问题处理
- SQLBuilder增加join 方法

### 更新 2023-12-29
- 增加查询结果转实体功能 queryT
- 方言家族增加 DialectSentence 语句方言子类

### 更新 2023-11-30
- 增加whereIn/addPara
- 修复mergeinto问题
- 增加SQLCreator
- 增加whereInGuid、whereNotExist、whereGuid、whereBetween、whereNotExist、whereOR
- 增加withAs 编织功能
- 增加参数校验事件 onParaValueCheck
- 增加TypeAs工具类
- 增加DataTable的ToList系列扩展、DataRow的getString系列扩展
- 增加客户端类MooClient,用作切面处理，增加事件的注册功能


### 更新 2023-9-22
- 增加pivot exits的适配
- where增加委托自由项
- 增加where下的and /or 方法
- 整理项目依赖，拆除newtonsoft依赖
- 导入功能重构、拆借Excel操作部分逻辑

### 更新 2023-7-31
- 增加方言对批量插入buildInsert的支持
- 添加mergeInto构造支持
- MatchBulk功能修正
- BulkBase/EditTable增强

### 更新 2023-7-3

- 启动通用化的mooSQL构建

## v2 framework U7支持版

- 完成基础执行器的构建

- 完成方言架构的基础实现

- 完成SQLBuilder核心功能的实现

### v1 变革期

由于strSQLMaker在语法上不够流利，存在对事务支持性较差，同时仅支持了SQLServer等多个问题，决定重新构建一个新的项目。



## v0 strSQLMaker

* 更新 2021-12-17 增加判断数据存在、查询行数据、根据主键获取行数据的3个方法。
* 更新 2021-10-18 增加自动判断更新插入的MatchBulk类，使用BulkTable和ModifyHelper进行处理
* 更新 2021-9-26   modifyHelper类增加addKV的多态方法
* 更新 2021-9-24  增加连接池清空相关功能。缓存链接。
* 更新 2021-9-6    modifyHelper类增加事务和自定义的SQL语句功能
* 更新 2021-9-1   批量更新 updateTable类增加 更新列黑名单功能。修改compareValue的字符串含空格时不一样的Bug.
* 更新 2021-8-31  ModifyHelper类增加命令参数的自定义。增加错误日志路径读取环境的检测。增加xml文件自行设置的检测
* 更新 2021-8-6   增加matchTable的保存方法，控制是否插入、更新、删除的属性
* 更新 2021-8-4   增加空日期校验方法 ,优化日期的解析功能
* 更新 2021-7-30  ValueItem类增加自定义参数名功能，同时适应修改modifyHelper生成SQL命令的方法
* 更新 2021-7-28  修复modifyHelper类在创建更新语句 from部分时错误的问题。