目录说明

- ado 核心的数据库驱动层
  
  - builder 核心SQL构建器
  - cache   缓存支持
  - call    方法调用定义
  - data    数据库驱动
  	- bulk    批量写入
    - context 执行上下文
    - database 数据库信息定义
    - dialect  核心方言定义
    - executor 执行器
  - DDL     数据操作定义
  - mapping 映射定义
  - SQL     SQL结构化和数据库结构化定义
  - typeHandle 查询结果类型处理
  - utils   工具类

- adoext 数据库驱动的上层建筑 
  
  - clip SQLClip 数据库操作类
  - ddl  数据库结构操作类
  - dto  查询实体
  - ext  扩展方法
  - nav  导航功能
  - repository 仓储功能
  - savable 比仓库个性化能力更强的实体保存功能
  - tree 树查询支持

- aop 核心切面

- auth 鉴权授权模块
  
  - dialect 权限方言定义
  - entity  权限实体定义
  - util    权限工具类

- expression 表达式基础功能
  
  - entity 实体定义

- linq LINQ 支持 
  
  - basis  基础建筑
  - ext    扩展方法
  - fast   快速构建LINQ组件
  - queryable  IQueryable 支持
  - translator  表达式翻译器

- meta 元数据模块

- utils 工具模块
  -

- zoom 其它扩展功能