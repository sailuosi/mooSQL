using mooSQL.config;
using mooSQL.data.context;
using mooSQL.data.Mapping;
using mooSQL.data.slave;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data
{
    /// <summary>
    /// mooSQL的用户侧顶级实例构建器
    /// </summary>
    public class BaseClientBuilder
    {
        /// <summary>
        /// mooSQL的客户端构建器
        /// </summary>
        public BaseClientBuilder() { 
            client= new MooClient();
            InitClientBase();
        }
        /// <summary>
        /// 客户实例
        /// </summary>
        protected MooClient client;
        /// <summary>
        /// 初始化构造器
        /// </summary>
        protected virtual void InitClientBase() {
            client.modifyMediator = SlaveFactory.createBase();
        }
        /// <summary>
        /// 构建业务侧顶级实例
        /// </summary>
        /// <returns></returns>
        public virtual DBInsCash doBuild() {
            return buildCashInner();
        }
        /// <summary>
        /// 执行构建
        /// </summary>
        /// <returns></returns>
        protected virtual DBInsCash buildCashInner()
        {
            if (this.youCash == null)
            {
                this.youCash = new DBInsCash(client);
            }
            else
            {
                youCash.client = client;
            }
            if (client.Watchor == null)
            {
                client.useWatchor(youCash.getExeWatchor());
            }
            if (client.Loggor == null)
            {
                client.useLogger(youCash.getExeQueryLog());
            }
            if (client.ClientFactory == null) { 
                client.useClientFactory(new DBClientFactory());
            }
            if (this.youCash.dialectFactory == null)
            {
                useDialectFactory(new DialectFactoryBase());
            }
            buildingCash();
            if (DBPositions != null) {
                youCash.addConfig(DBPositions);
            }
            //对于二级属性，允许超前注册、滞后加载
            if (parses != null) {
                foreach (var p in parses) {
                    client.entityAnalyseFactory.register(p);
                }
            }
            //超前注册的方言入库
            if (diadic != null) {
                foreach (var dd in diadic) {
                    client.dialectFactory.useDialect(dd.Key,dd.Value);
                }
            }
            //配置实体翻译器的应用
            if (_configET != null) {
                _configET(client.Translator);
            }
            youCash.configPath = DBXMLConfig;

            return this.youCash;
        }
        /// <summary>
        /// 基础构建完毕钩子，由子类使用
        /// </summary>
        protected virtual void buildingCash() {
            //子类使用
        }
        private Action<EntityTranslator> _configET;
        /// <summary>
        /// 自定义实体的处理切面
        /// </summary>
        /// <param name="act"></param>
        /// <returns></returns>
        public BaseClientBuilder useEntityTranslate(Action<EntityTranslator> act)
        {
            _configET = act;
            return this;
        }

        /// <summary>
        /// 注册方言工厂
        /// </summary>
        /// <param name="dialectFactory"></param>
        /// <returns></returns>
        public BaseClientBuilder useDialectFactory(IDialectFactory dialectFactory) { 
            client.dialectFactory = dialectFactory;
            return this;
        }
        private Dictionary<DataBaseType, Func<Dialect>> diadic= new Dictionary<DataBaseType, Func<Dialect>>(); 
        /// <summary>
        /// 注册方言，注意！如果自定义工厂，必须先调用useDialectFactory,再调用本方法
        /// </summary>
        /// <param name="dbType"></param>
        /// <param name="creator"></param>
        /// <returns></returns>
        public BaseClientBuilder useDialect(DataBaseType dbType, Func<Dialect> creator)
        {
            diadic[dbType] = creator;
            return this;
        }
        /// <summary>
        /// 注册实体解析工厂，默认有。
        /// </summary>
        /// <param name="entityAnalyseFactory"></param>
        /// <returns></returns>
        public BaseClientBuilder useEntityAnalyseFactory(IEntityAnalyseFactory entityAnalyseFactory) { 
            client.entityAnalyseFactory = entityAnalyseFactory;
            return this;
        }

        private List<IEntityAnalyser> parses = new List<IEntityAnalyser>();

        /// <summary>
        /// 【已废弃，存在错别字】注册自定义的实体解析器
        /// </summary>
        /// <param name="entityAnalyser"></param>
        /// <returns></returns>
        [Obsolete("拼写错误，将在新版本废弃")]
        public BaseClientBuilder useEnityAnalyser(IEntityAnalyser entityAnalyser) {
            parses.Add(entityAnalyser);
            return this;
        }
        /// <summary>
        /// 注册自定义的实体解析器
        /// </summary>
        /// <param name="entityAnalyser"></param>
        /// <returns></returns>
        public BaseClientBuilder useEntityAnalyser(IEntityAnalyser entityAnalyser)
        {
            parses.Add(entityAnalyser);
            return this;
        }
        /// <summary>
        /// 注册业务实体工厂
        /// </summary>
        /// <param name="clientFactory"></param>
        /// <returns></returns>
        public BaseClientBuilder useClientFactory(DBClientFactory clientFactory)
        {
            client.useClientFactory( clientFactory);
            return this;
        }

        /// <summary>
        /// 注册SQL执行的监听器
        /// </summary>
        /// <param name="watcher"></param>
        /// <returns></returns>
        public BaseClientBuilder useWatcher(IWatchor watcher) { 
            client.useWatchor(watcher);
            return this;
        }
        /// <summary>
        /// 注册日志器
        /// </summary>
        /// <param name="logger"></param>
        /// <returns></returns>
        public BaseClientBuilder useLogger(IExeLog logger)
        {
            client.useLogger(logger);
            return this;
        }
        /// <summary>
        /// 注册缓存插件
        /// </summary>
        /// <param name="cache"></param>
        /// <returns></returns>
        public BaseClientBuilder useCache(ISooCache cache)
        {
            client.useCache(cache);
            return this;
        }
        /// <summary>
        /// 注册数据库加载方法
        /// </summary>
        /// <param name="loadDBByPosition"></param>
        /// <returns></returns>
        public BaseClientBuilder useDataBase(Func<int, DataBase> loadDBByPosition) { 
            client.useDataBase(loadDBByPosition);
            return this;
        }
        /// <summary>
        /// 注册某个连接位的加载方法
        /// </summary>
        /// <param name="position"></param>
        /// <param name="createDBConfig"></param>
        /// <returns></returns>
        public BaseClientBuilder useDataBase(int position, Func<DataBase> createDBConfig) {
            client.useDataBase(position, createDBConfig);
            return this;
        }
        private List<DBPosition> DBPositions;
        /// <summary>
        /// 注册数据库
        /// </summary>
        /// <param name="positions"></param>
        /// <returns></returns>
        public BaseClientBuilder useDataBase(List<DBPosition> positions)
        {
            this.DBPositions = positions;
            return this;
        }
        private string DBXMLConfig;
        /// <summary>
        /// 注册XML配置，等待DialectFactory进行解析
        /// </summary>
        /// <param name="configPath"></param>
        /// <returns></returns>
        public BaseClientBuilder useDBXMLConfig(string configPath) {
            this.DBXMLConfig = configPath;
            return this;
        }
        
        /// <summary>
        /// 注册主从库
        /// </summary>
        /// <param name="createTeam"></param>
        /// <returns></returns>
        public BaseClientBuilder useSlave(Func<SlaveTeam, SlaveTeam> createTeam)
        {
            var slave = SlaveFactory.CreateSlave("");
            slave = createTeam(slave);
            client.modifyMediator.signModify(slave);
            return this;
        }
        /// <summary>
        /// 注册主从库
        /// </summary>
        /// <param name="slaveTeam"></param>
        /// <returns></returns>
        public BaseClientBuilder useSlave(SlaveTeam slaveTeam)
        {
            client.modifyMediator.signModify(slaveTeam);
            return this;
        }
        /// <summary>
        /// 配置管理实例
        /// </summary>
        protected DBInsCash youCash;
        /// <summary>
        /// 自定义实例时使用，一般无须使用
        /// </summary>
        /// <param name="insCash"></param>
        /// <returns></returns>
        public BaseClientBuilder useCashHolder(DBInsCash insCash) { 
            this.youCash = insCash;
            return this;
        }

        /// <summary>
        /// SQL执行前
        /// </summary>
        /// <param name="handler"></param>
        /// <returns></returns>
        public BaseClientBuilder onBeforeExecute(Func<ExeContext ,  string,string> handler)
        {
            client.events.onBeforeExecute(handler);
            return this;
        }
        /// <summary>
        /// 慢SQL处置
        /// </summary>
        /// <param name="handler"></param>
        /// <returns></returns>
        public BaseClientBuilder onWatchSlowSQL(Action<ExeContext, TimeSpan, string> handler)
        {
            client.events.OnWatchingSlowSQL+=handler;
            return this;
        }
        /// <summary>
        /// 数据库实例创建时刻
        /// </summary>
        /// <param name="handler"></param>
        /// <returns></returns>
        public BaseClientBuilder onDBLiveCreated(Action<DBInstance> handler)
        {
            client.events.OnDBLiveCreated += handler;
            return this;
        }
        /// <summary>
        /// 执行SQL后
        /// </summary>
        /// <param name="handler"></param>
        /// <returns></returns>
        public BaseClientBuilder onAfterExecute(Func<ExeContext, string, string> handler)
        {
            client.events.onAfterExecute(handler);
            return this;
        }
        /// <summary>
        /// 执行SQL发生异常时
        /// </summary>
        /// <param name="handler"></param>
        /// <returns></returns>
        public BaseClientBuilder onExecuteError(Func<ExeContext, Exception, string, string> handler)
        {
            client.events.onExecuteError(handler);
            return this;
        }
        /// <summary>
        /// 设置字段值时
        /// </summary>
        /// <param name="handler"></param>
        /// <returns></returns>
        public BaseClientBuilder onBuildSetFrag(Func<SetFrag, SQLBuilder, bool> handler)
        {
            client.events.onBuildSetFrag(handler);
            return this;
        }
        /// <summary>
        /// 设置where条件值时
        /// </summary>
        /// <param name="handler"></param>
        /// <returns></returns>
        public BaseClientBuilder onBuildWhereFrag(Func<WhereFrag, SQLBuilder, bool> handler)
        {
            client.events.onBuildWhereFrag(handler);
            return this;
        }
        /// <summary>
        /// 创建SQL完毕时
        /// </summary>
        /// <param name="handler"></param>
        /// <returns></returns>
        public BaseClientBuilder onCreatedSQL(Action<string, SQLBuilder> handler)
        {
            client.events.onCreatedSQL(handler);
            return this;
        }


    }
}
