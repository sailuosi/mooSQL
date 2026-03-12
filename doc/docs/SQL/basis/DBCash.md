# 定位
作为一个总入口，内置工厂方法，用于获取各项实例
DBCash 不在核心库中定义，一般是在业务系统侧进行定义。此处需要接入mooSQL根实例的初始化，以及数据库连接位的读取和初始化，和根据情况启用的实体解析器注册、生命周期钩子等事宜

## useRepo

````c#
_sysPosRep = DBCash.useRepo<SysPos>(0);
````
## useUnitOfWork

````c#
var ufw = DBCash.useUnitOfWork(0);
````

## useBus
获取查询表达式的实例
````c#
var db = DBCash.useBus<SysUserConfig>(0);
var dt = db.Count();
````


## useSQL
获取一个SQLBuilder的实例
````c#
var kit = DBCash.useSQL(0);
````

## DBClientBuilder
DBClientBuilder是一个用来构建mooSQL根客户端实例的构造器，它最终构建出的是DBInsCash对象，作为一个全局单例的实例，存放在DBCash类下，作为静态成员。
[点击查看DBClientBuilder API](../configs/dbclientbuilder)
典型的初始化如下
````c#
cash = builder
    .useCache(cache)
    .useEnityAnalyser(new SugarEnitiyParser())
    .doBuild();
````