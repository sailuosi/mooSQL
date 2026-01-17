
using mooSQL.data.context;
using mooSQL.data.Mapping;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

using System.Xml;

namespace mooSQL.data
{
    /// <summary>
    /// 数据库的配置信息缓存类。
    /// </summary>
     public class DBInsCash
    {


        /// <summary>
        /// 修改时，可对
        /// </summary>
        public IDialectFactory dialectFactory
        {
            get { 
                return client.dialectFactory;
            }
            set {
                client.dialectFactory=value;
            }
        }
        /// <summary>
        /// 将自动创建一个MooClient实例
        /// </summary>
        public DBInsCash() {
            this.getClient();

        }
        public DBInsCash(MooClient client)
        {
            this.client = client;

        }
        /// <summary>
        /// 获取或创建一个Moosql的实例
        /// </summary>
        /// <returns></returns>
        public MooClient getClient() {
            if (this.client == null) {
                this.client = newClient();
                if (client.Watchor == null)
                {
                    client.useWatchor(getExeWatchor());
                }
                if (client.Loggor == null)
                {
                    client.useLogger(getExeQueryLog());
                }

                client.entityAnalyseFactory = new BaseEntityAnalyseFactory();
            }
            return this.client;
        }
        /// <summary>
        /// 数据库配置的路径，在获取或初始化数据库实例信息时使用。
        /// </summary>
        public string configPath = "";

        private ConcurrentDictionary<int, DBInstance> dbMap = new ConcurrentDictionary<int, DBInstance>();

        private static ConcurrentDictionary<int, DataBase> configMap = null;

        public MooClient client=null;

        public int defaultPosition = 0;
        /// <summary>
        /// 暴露给业务侧的数据库配置信息
        /// </summary>
        public ConcurrentDictionary<int, DataBase> ConnectMap
        {
            get { 
                return configMap;
            }
        }
        /// <summary>
        /// 供外界获取是否有某个连接位的配置；
        /// </summary>
        /// <param name="position"></param>
        /// <returns></returns>
        public bool Contains(int position) { 
            if(dbMap.ContainsKey(position)) return true;
            if(configMap!=null && configMap.ContainsKey(position)) return true;
            return false;
        }

        /// <summary>
        /// 获取一个连接位的数据库实例信息
        /// </summary>
        /// <param name="postion"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public DBInstance getInstance(int postion)
        {
            if (dbMap.ContainsKey(postion))
            {
                return dbMap[postion];
            }
            else
            {
                if (configMap == null) {
                    loadDBCongfig();
                }

                //如果既包含了配置，配置值又是null,
                var tar = getDBConfig(postion);

                if (tar == null)
                {
                    throw new Exception("当前数据库配置中没有该连接位的配置信息！");
                }
                var dbtar= buildInstance(tar);
                dbMap.TryAdd(postion, dbtar);
                return dbtar;
            }
        }
        /// <summary>
        /// 根据名称来获取数据库实例，需要配置时配置name选项
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public DBInstance getInstance(string name) {
            var i=getPostionByName(name);
            if (i != -1) { 
                return getInstance(i);
            }
            return null;

        }

        private int getPostionByName(string name) {
            foreach (var kv in configMap)
            {
                if (kv.Value.name == name)
                {
                    return kv.Key;
                }
            }
            return -1;
        }

        public virtual MooClient newClient() {
            return new MooClient();
        }

        /// <summary>
        /// 获取数据库日志对象
        /// </summary>
        /// <returns></returns>
        public virtual IExeLog getExeQueryLog() { 
            return new MooLoger(); 
        }
        /// <summary>
        /// 获取数据库命令执行的监听器
        /// </summary>
        /// <returns></returns>
        public virtual IWatchor getExeWatchor()
        {
            return new Watchor();
        }

        /// <summary>
        /// 获取数据库的命令执行器
        /// </summary>
        /// <param name="db"></param>
        /// <returns></returns>
        public virtual ICmdExecutor getExeCutor(DBInstance db)
        {
            var cmd= new CmdExecutor(db);
            return cmd;
        }
        /// <summary>
        /// 基于配置，创建数据库执行准备好的实例
        /// </summary>
        /// <param name="config"></param>
        /// <returns></returns>
        public DBInstance buildInstance(DataBase config) {
            if (client ==null)
            {
                this.client = getClient();
                if (client.Watchor == null) { 
                    client.useWatchor(getExeWatchor());
                }
                if (client.Loggor == null)
                {
                    client.useLogger(getExeQueryLog());
                }
            }
            DBInstance db = new DBInstance();
            db.config = config;
            if (config.getDialect != null)
            {
                db.dialect = config.getDialect();
            }
            else {
                db.dialect = dialectFactory.getDialect(config);
            }
            
            db.dialect.dbInstance = db;

            db.client = client;
            
            db.cmd = getExeCutor(db); 
            //触发实例创建事件，用于客户侧注册实例动作
            client.events.FireCreateDBLive(db);
            return db;
        }

        /// <summary>
        /// 代码方式添加一个数据库连接位，存在则覆盖
        /// </summary>
        /// <param name="position"></param>
        /// <param name="db"></param>
        public void addDataBase(int position, DataBase db) {
            if (db == null) {
                return;
            }
            //当用户手动创建的时间，初始化，此时不会再触发自动加载xml
            if (configMap == null)
            {
                configMap = new ConcurrentDictionary<int, DataBase>();
            }

            if (configMap.ContainsKey(position) ) {
                configMap[position] = db;
            }
            else
            {

                configMap.TryAdd(position, db);
            }

        }
        /// <summary>
        /// 
        /// 获取数据库连接
        /// </summary>
        /// <returns></returns>
        public DataBase getDBConfig()
        {
            return getDBConfig(defaultPosition);
        }
        /// <summary>
        /// 获取指定顺位的数据库连接
        /// </summary>
        /// <param name="position"></param>
        /// <returns></returns>
        public DataBase getDBConfig(int position)
        {
            if (configMap == null)
            {
                configMap = new ConcurrentDictionary<int, DataBase> ();
                initDBConfigs(position);
            }
            if (configMap.ContainsKey(position))
            {
                var res= configMap[position];
                if (res != null) { 
                    return res;
                }
                if (res == null)
                {
                    //包含键值，值却是空，正常情况下不应该发生该情况，可能是值被清理或者错误置为null了，重新初始化。
                    configMap = new ConcurrentDictionary<int, DataBase>();
                    //configMap.TryRemove(position);
                    var t= initDBConfigs(position);
                    if(t != null)
                    {
                        return t;
                    }
                }
            }
            //尝试初始化
            var tar = initDBConfigs(position);
            if (tar != null)
            {
                return tar;
            }

            return null;

        }

        private DataBase initDBConfigs(int position) {
            this.loadDBCongfig();
            if(configMap.ContainsKey(position))
            {
                var res= configMap[position];
                if (res != null)
                {
                    return res;
                }
            }
            //尝试从客户端配置中获取
            if (client != null)
            {
                var t = client.loadDBConfig(position);
                if (t != null)
                {
                    configMap[position] = t;
                    return t;
                }
            }
            return null;
        }


        private void loadDBCongfig()
        {
            try
            {
                if (configMap == null)
                {
                    configMap = new ConcurrentDictionary<int, DataBase>();
                }

                var t= dialectFactory.loadDBConfig(this);
                if (t != null) { 
                    foreach (var kv in t) {
                        configMap.TryAdd(kv.Key, kv.Value);
                    }
                
                }

            }
            catch (Exception ex)
            {

            }
        }
    }
}
