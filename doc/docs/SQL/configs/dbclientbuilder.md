---
outline: deep
---

# BaseClientBuilder

mooSQL的用户侧顶级实例构建器

## doBuild
执行构建，返回创建好的DBInsCash实例
````c#
doBuild()
````
## 注册成员
### useCache
注册缓存插件
````c#
useCache(ICache cache)
````

### useDataBase
注册数据库加载方法
````c#
useDataBase(Func<int, DataBase> loadDBByPostion)
//注册某个连接位的加载方法
useDataBase(int position, Func<DataBase> createDBConfig)
//注册一组数据库
useDataBase(List<DBPosition> positions)
````

### useDBXMLConfig
注册XML配置，等待DialectFactory进行解析
````c#
useDBXMLConfig(string confipath)
````

### useSlave
注册主从库
````c#
useSlave(Func<SlaveTeam, SlaveTeam> createTeam)

useSlave(SlaveTeam slaveTeam)
````

### useCashHolder
注册Cash持有者，当继承DBinsCash时使用，自定义实例时使用，一般无须使用
````c#
useCashHolder(DBInsCash insCash)
````

### useDialectFactory
注册方言工厂。当有一个数据库的类型，未支持时，可以通过注册实现自定义支持
````c#
useDialectFactory(IDialectFactory dialectFactory)
````

### useEntityAnalyseFactory
注册实体解析工厂，默认有。
````c#
useEntityAnalyseFactory(IEntityAnalyseFactory entityAnalyseFactory)
````

### useEnityAnalyser
注册自定义的实体解析器，经典的有，注册SqlSugar/UCML的实体类解析器
````c#
useEnityAnalyser(IEntityAnalyser entityAnalyser) 
````

### useWatcher
注册SQL执行的监听器，注册以实现SQL执行日志的处理
````c#
useWatcher(IWatchor watcher)
````

### useLogger
注册日志器，可以实现SQL日志的保存
````c#
useLogger(IExeLog logger)
````

## 事件

### onBeforeExecute
SQL执行前
````c#
onBeforeExecute(Func<ExeContext ,  string,string> handler)
````

### onAfterExecute
执行SQL后
````c#
onAfterExecute(Func<ExeContext, string, string> handler)
````

### onExecuteError
执行SQL发生异常时
````c#
onExecuteError(Func<ExeContext, Exception, string, string> handler)
````

### onBuildSetFrag
设置字段值时
````c#
onBuildSetFrag(Func<SetFrag, SQLBuilder, bool> handler)
````

### onBuildWhereFrag
设置where条件值时
````c#
onBuildWhereFrag(Func<WhereFrag, SQLBuilder, bool> handler)
````

### onCreatedSQL
创建SQL完毕时
````c#
onCreatedSQL(Action<string, SQLBuilder> handler)
````