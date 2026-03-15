---
outline: deep
---

# 快速开始

## 安装

mooSQL是一个自研的类库，安装包在内部gitlab中发布。目前可以通过gitlab仓库获取到安装包，或者从其它途径获取安装包。

### 从gitlab仓库获取安装包

1. 登录gitlab仓库，找到u8common项目。
2. 点击项目的“pack6.0.0”文件夹。
3. 双击打开安装工具进行发布版本的安装包。

### 从其它途径获取安装包

1. 联系项目维护人员，请求获取安装包。
2. 等待项目维护人员回复，获取安装包。

### 执行安装或更新

1. 双击安装包内的安装工具程序，打开后点击【执行】按钮
2. 安装完成后，在解决方案中，右键点击解决方案根目录，选择【清理解决方案】或者【重新生成解决方案】。
3. 清理完成后，重新编译运行项目。

## 包的使用

### 信息

- mooSQL.pure是核心纯净包，无方言定义，极少的的依赖。
- mooSQL.Ext 是扩展包，包含了方言定义，如各数据库的支持。依赖了常见的包JSON/NPOI等。

一般项目直接依赖mooSQL.Ext即可。

使用nuget进行搜索包时，在本地包路径进行搜索。
- 本地包路径一般为：C:\Users\Administrator\.nuget\packages
使用其它操作系统用户时， 包的路径为：C:\Users\用户名\.nuget\packages

### 配置和集成

为了兼容性，mooSQL自身不依赖业务框架，数据库配置参数的读取，以及业务系统中的集成使用入口，都是通过业务系统自己实现的。
目前常规约定是在业务系统侧定义一个静态类DBCash，用于读取数据库配置参数。其核心代码为初始化一个DBInsCash类的全局实例。

````c#
public partial class DBCash
{
    private static DBInsCash cash = null;


    /// <summary>
    /// 根据索引获取数据库连接位
    /// </summary>
    /// <param name="position"></param>
    /// <returns></returns>
    public static DBInstance GetDBInstance(int position) {
        if (cash == null) {
            initFactory();

        }
        try {
            return cash.getInstance(position);
        }
        catch(Exception ex) 
        {
            loadDBConfig();
            return cash.getInstance(position);
        }
        
    }

    private static void initFactory() {
        var builder= new DBClientBuilder();
        var cache = new MooCache();

        cash = builder
            .useCache(cache)
            .useEnityAnalyser(new SugarEnitiyParser())
            .doBuild();

        loadDBConfig();

    }

    /// <summary>
    /// 根据字符串名称获取数据库连接位
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    public static DBInstance GetDBInstance(string name)
    {
        if (cash == null)
        {
            initFactory();
        }
        try
        {
            return cash.getInstance(name);
        }
        catch (Exception ex)
        {
            loadDBConfig();
            return cash.getInstance(name);
        }

    }


    private static int loadDBConfig() {

        var myConfigs = App.GetOptions<DBCoonfigOptions>();
        if (myConfigs != null && myConfigs.Connections !=null && myConfigs.Connections.Count > 0) {
            cash.addConfig(myConfigs.Connections);
        }

        return cc;
    }



    public static SQLKit useSQL(int position)
    {

        var db= GetDBInstance(position);
        var kit = new SQLKit();
        kit.setDBInstance(db);
        return kit;
    }

    public static SQLKit newKit(string name)
    {

        var db = GetDBInstance(name);
        var kit = new SQLKit();
        kit.setDBInstance(db);
        return kit;
    }

    public static ITable<T> useEntity<T>(int position) { 
        var db = GetDBInstance(position);

        //var connect = new DataContext(db);

        return new Table<T>(db);

    }

    public static DbBus<T> useBus<T>(int position)
    {
        var db = GetDBInstance(position);

        var connect = new DbContext();
        var fac = new EntityVisitFactory();
        connect.Factory = fac;
        connect.DB = db;
        return new EnDbBus<T>(connect,typeof(T), fac);
    }


    public static BulkTable newBulk(string tableName, int position) { 
        BulkTable bk= new BulkTable(tableName,GetDBInstance(position));
        bk.getBulkTable();
        return bk;
    }

    public static BatchSQL newBatchSQL(int position)
    {

        var db = GetDBInstance(position);
        var kit = new SQLKit();
        kit.setDBInstance(db);
        var res = new BatchSQL(kit);
        return res;
    }
    public static SooRepository<T> useRepo<T>(int position) where T : class, new()
    {
        var db = GetDBInstance(position);
        return db.useRepo<T>();
    }

    public static SooUnitOfWork useUnitOfWork(int position)
    {
        var db = GetDBInstance(position);
        return db.useWork();
    }
    public static SooUnitOfWork useWork(int position)
    {
        var db = GetDBInstance(position);
        return db.useWork();
    }

    public static SQLClip useClip(int position)
    {
        var db = GetDBInstance(position);
        return db.useClip();
    }
}

````
