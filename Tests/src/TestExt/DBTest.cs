// 基础功能说明：


using HHNY.NET.Core.MooSQL;
using mooSQL.data;
using mooSQL.linq;
using mooSQL.linq.core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestMooSQL.src;
public class DBTest
{

    private static DBInsCash cash = null;

    private static LinqReadyBook book = null;
    /// <summary>
    /// 根据索引获取数据库连接位
    /// </summary>
    /// <param name="position"></param>
    /// <returns></returns>
    public static DBInstance GetDBInstance(int position)
    {
        if (cash == null)
        {
            initFactory();

        }
        try
        {
            return cash.getInstance(position);
        }
        catch (Exception ex)
        {
            loadDBConfig();
            return cash.getInstance(position);
        }

    }

    private static void initFactory()
    {
        var builder = new DBClientBuilder();
        //var cache = new MooCache();

        cash = builder
            //.useCache(cache)
            .useEntityAnalyser(new SugarEnitiyParser())
            .doBuild();

        loadDBConfig();

    }

    private static int loadDBConfig()
    {
        int cc = 0;

        
            var db1 = new DataBase();
            db1.dbType = DataBaseType.MSSQL;
            db1.DBConnectStr = "Enlist=false;Data Source=localhost;Database=netapi;User Id=test;Password=123456;Encrypt=True;TrustServerCertificate=True;";
            db1.name ="0";
            db1.version ="13.0";
            db1.versionNumber = 13.0;
            //db1.databaseName = "ZHXT_Tar";

            cash.addDataBase(0, db1);
            cc++;
        



        return cc;
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
    public static SQLBuilder useSQL(int position)
    {

        var db = GetDBInstance(position);
        return db.useSQL();
    }


    public static DbBus<T> useBus<T>(int position)
    {
        var db = GetDBInstance(position);

        var connect = new DbContext();
        var fac = new EntityVisitFactory();
        connect.Factory = fac;
        connect.DB = db;
        return new EnDbBus<T>(connect, typeof(T), fac);
    }

    public static DbBus<T> useDb<T>(int position)
    {
        if (book == null)
        {
            book = new LinqReadyBook();
        }
        var cont = book.Get(position);
        if (cont != null)
        {
            return new EnDbBus<T>(cont, typeof(T), cont.Factory);
        }
        var db = GetDBInstance(position);

        var connect = new DbContext();
        var fac = new FastLinqFactory();
        connect.Factory = fac;
        connect.DB = db;
        book.Add(position, connect);
        return new EnDbBus<T>(connect, typeof(T), fac);
    }



    public static BatchSQL newBatchSQL(int position)
    {

        var db = GetDBInstance(position);
        return db.useBatchSQL();
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
