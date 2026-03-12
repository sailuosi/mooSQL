# 配置
## 1、配置的功能

此处配置的核心用处，在于将不同系统的数据库定义方式与核心逻辑分开，由具体业务系统定义基于xml或者json等配置读入数据库连接的配置，创建数据库实例。

## 2、数据库连接的配置

    核心库 提供的配置相关类

         DBInsCash  ---- 数据库实例缓存类。

         DBInstance  ---- 数据库实例类

         SQLBuilder   ---- SQL编织器类

         BulkBase       ---- BulkInsert批量插入的操作类



## 3、多端一致性的惯性定义子类。

DBCash   --- 持有工厂类，定义数据配置的读取方式，生产可供直接使用的 SQLBuilder/BatchSQL等实例。



        









99



## 配置案例-UCML
0、各类数据库配置示例：

SQLServer 数据库:   MSSQL
````xml
Enlist=false;Data Source=137.12.*.*;Database=**;User Id=***;Password=***;Encrypt=True;TrustServerCertificate=True;
````
OceanBase数据库:  OceanBase
````xml
server=137.12.7.*;database=****;user Id=xxxx@xxx#xx;password=xxxxxxxx;pooling=true;Port=8088;Charset=utf8mb4;Convert Zero Datetime=True;
````
MySQL数据库：  MySQL
````xml
server=10.16.10.*;database=xxx;user Id=xxxx;password=xxxxx;pooling=true;Port=3306;Charset=utf8mb4;Convert Zero Datetime=True;
````
Postgre数据库  PostgreSQL
````xml
PORT=5432;DATABASE=xxx;HOST=137.12.7.**;PASSWORD=xxxxxxxxxx;USER ID=xxxxx;
````

## 独立配置
现在mooSQL已支持独立的数据库配置，以便快速进行数据库配置（自2024.11版本开始）。配置核心项为 Connections数组，具体属性含义如下：
````json
    "Connections": [
      { // 数据库1 --
        "Position": 1,//索引编码，整型，在DBCash中被使用
        "Name": "UcmlTar",//字符型名称
        "DbType": "MSSQL", // MSSQL/Oracle/Access/PostgreSQL/MySQL/OceanBase/Taos/GBase8a
        "ConnectString": "server=*.*.*.*;database=*;user Id=**;password=**;pooling=true;Port=3306;Charset=utf8mb4;Convert Zero Datetime=True;", // 库连接字符串
        "Version": "13.0.0",// 数据库版本号
        "VersionNumber":13.0 //数值型版本号
      },
      { // 数据库2 --
        "Position": 2,
        "Name": "Device",
        "DbType": "MySQL", // 
        "ConnectString": "server=*.*.*.*;database=*;user Id=**;password=**;pooling=true;Port=3306;Charset=utf8mb4;Convert Zero Datetime=True;", // 库连接字符串
        "Version": "5.7.21"
      }
````
##  DBCash
````c#
    public partial class DBCash
    {
        private static DBInsCash cash = null;
        
        public static DBInstance GetDBInstance(int position) {
            if (cash == null) {
                createCash();
            }
            return cash.getInstance(position);
        }
        private static void createCash() {
            cash = new DBInsCash();
            MooEventHandler handler = new MooEventHandler();
            string str = getCurPath();
            var tar = (str + "/bin/ucmlconf.xml");
            cash.configPath = tar;
            cash.client.events.onBuildSetFrag((SetFrag frag, SQLBuilder builder)=>
            {
                var pair = frag.values[builder.InsertRowIndex];
                if (pair.paramed)
                {
                    var val = pair.value;
                    if (val is JValue)
                    {
                        pair.value = val.ToString();
                    }
                }
                return true;
            });
            cash.client.events.onBuildWhereFrag((WhereFrag frag, SQLBuilder builder)=>
        {
                if (frag.paramed)
                {
                    var val = frag.value;
                    if (val is JValue)
                    {
                        frag.value = val.ToString();
                    }
                }
                return true;
            });
        }
        private static string getCurPath()
        {
            if (UCMLCommon.UCMLInitEnv.Server != null) {
                return UCMLCommon.UCMLInitEnv.Server.MapPath("~");
            }
            var cur= Assembly.GetExecutingAssembly().Location;
            var i=cur.LastIndexOf('\\');
            var path=cur.Substring(0,i);
            return path;
        }
        public static SQLKit newKit(int position)
        {
            var db= GetDBInstance(position);
            var kit = new SQLKit();
            kit.setDBInstance(db);
            return kit;
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
    }
````

## 日志输出

方式1  重写监听器类
````c#
    public class QueryWatchor : Watchor {
        public override string onAfterExecuteSetError(string oprationId, ExeContext context, Exception ex, string operation)
        {
            var sql = context.cmd.cmdText;
            
            var paras = context.cmd.para;
            var msg = "";
            foreach (var para in paras.value)
            {
                sql = sql.Replace(para.Key, para.Value.val.ToString());
                msg += string.Format("参数{0}:{1},", para.Key, para.Value.val);
            }
            var info = string.Format("SQL执行期间发生错误({1})：{2}\nSQL:{0},\n", sql, DateTime.Now.ToString(),ex.Message);
            saveFileLog(info);
            return info;
        }
        private void saveFileLog(string message)
        {
            //未能找到路径“D:\PXXT\getHRManSql.txt”的一部分。
            string str = UCMLCommon.UCMLInitEnv.Server.MapPath("~");
            var tar = (str + "/log/moosql/");
            var filepath = tar;
            //子路径不存在时创建
            System.IO.DirectoryInfo dir = new DirectoryInfo(filepath);
            if (!dir.Exists)
            {
                dir.Create();
            }
            //名称格式 年月日 时分秒 随机码
            var dand = new Random();
            var fileName = string.Format("errSql{0}_{1}_{2}.txt", DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day);
            File.AppendAllText(filepath+fileName, message);
        }
    }
````
        然后重写工厂类，然后在DBCash中调用
````c#
    public class DBInsFactory:DBInsCash { 
    
        public DBInsFactory()
        {
        }
        public override IExeLog getExeQueryLog()
        {
            return new QueryLog();
        }
        public override IWatchor getExeWatchor()
        {
            return new QueryWatchor();
        }
    }
````

## 参数格式转换

````c#
    public class MooEventHandler {
        public  bool onCheckSetVal(SetFrag frag, SQLBuilder builder)
        {
            var pair = frag.values[builder.InsertRowIndex];
            if (pair.paramed)
            {
                var val = pair.value;
                if (val is JValue)
                {
                    pair.value = val.ToString();
                }
            }
            return true;
        }
        public bool onCheckWhereVal(WhereFrag frag, SQLBuilder builder)
        {
            if (frag.paramed)
            {
                var val = frag.value;
                if (val is JValue)
                {
                    frag.value = val.ToString();
                }
            }
            return true;
        }
    }
````



## 关于调试
1、打断点

SQLBuilder 的 do...系列方法，内部都会调用 to...系列方法来创建SQL。 因此，可以在 to方法中打断点来查看SQL。

      

2、事件。

DBCash中，提供多个SQL执行生命期的事件，可以在事件注册方法中打断点。同样可以查询SQL。

3、异常

注册异常事件。一般会在DBCash中注册异常事件，并输出SQL错误的日志，便于后续核查。