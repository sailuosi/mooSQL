


using mooSQL.data.context;

using mooSQL.data.Mapping;
using mooSQL.data.slave;
using System;
using System.Collections.Generic;
using System.Linq;

using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data
{
    /// <summary>
    /// moosql的单体实例。
    /// </summary>
    public class MooClient
    {
        private IDialectFactory _dialectFactory;
        /// <summary>
        /// 方言工厂。必须在初始化前设置。
        /// </summary>
        public IDialectFactory dialectFactory
        {
            get { return _dialectFactory; }
            set { _dialectFactory = value; }
        }
        private DBClientFactory _clientFactory;
        /// <summary>
        /// 数据库客户端工作类工厂。
        /// </summary>
        public DBClientFactory ClientFactory { 
            get { 
                if(_clientFactory == null)
                {
                    _clientFactory = new DBClientFactory();
                }
                return _clientFactory;
            }
            private set { _clientFactory = value; }
                
        }

        private MooEvents _events = new MooEvents();
        /// <summary>
        /// SQL编织器的配置
        /// </summary>
        public SQLBuilderOption builderOption= new SQLBuilderOption();
        /// <summary>
        /// 事件注册器。
        /// </summary>
        public MooEvents events { get { return _events; } }
        private IWatchor watchor = null;
        /// <summary>
        /// 监听器实例
        /// </summary>
        public IWatchor Watchor
        {
            get
            {
                if (watchor == null)
                {
                    watchor = new Watchor();
                }
                return watchor;
            }
        }

        private IExeLog log = null;
        /// <summary>
        /// 日志实例
        /// </summary>
        public IExeLog Loggor
        {
            get
            {
                if (log == null)
                {
                    log = new MooLoger();
                }
                return log;
            }
        }
        private ISooCache _cache = null;

        /// <summary>
        /// 缓存实例
        /// </summary>
        public ISooCache Cache
        {
            get
            {
                if (_cache == null)
                {
                    _cache = new HashCache();
                }
                return _cache;
            }
        }
        public ModifyMediator modifyMediator;
        /// <summary>
        /// 数据库设置委托。
        /// </summary>
        private Dictionary<int,Func<DataBase>> dbConfigs = new Dictionary<int, Func<DataBase>>();

        private List< Func<int, DataBase>> _dbloaders;
        /// <summary>
        /// 实体解析工厂
        /// </summary>
        public IEntityAnalyseFactory entityAnalyseFactory;

        private EntityTranslator _Translator;
        /// <summary>
        /// 实体类转换器
        /// </summary>
        public EntityTranslator Translator
        {
            get
            {
                if (_Translator == null) _Translator = ClientFactory.getEntityTranslator();
                return _Translator;
            }

        }
        public MooClient() { 
            //默认添加内置实体类解析器。此处被业务侧覆盖的概率较小。
            this.entityAnalyseFactory= new BaseEntityAnalyseFactory();

        }

        private EntityContext _entityContext;
        /// <summary>
        /// 实体类信息持有
        /// </summary>
        public EntityContext EntityCash
        {
            get
            {
                if (_entityContext == null)
                {
                    _entityContext = new EntityContext(entityAnalyseFactory);
                }
                return _entityContext;
            }
        }

        /// <summary>
        /// 已注册的数据库连接信息
        /// </summary>
        public Dictionary<int, Func<DataBase>> DataBaseMap { get { return dbConfigs; } }


        /// <summary>
        /// 启用日志
        /// </summary>
        /// <param name="log"></param>
        /// <returns></returns>
        public MooClient useLogger(IExeLog log)
        {
            this.log = log;
            return this;
        }
        /// <summary>
        /// 启用监听器
        /// </summary>
        /// <param name="watchor"></param>
        /// <returns></returns>
        public MooClient useWatchor(IWatchor watchor)
        {
            this.watchor = watchor;
            return this;
        }
        /// <summary>
        /// 启用cache实例，默认情况下，使用自带的hashcache
        /// </summary>
        /// <param name="cache"></param>
        /// <returns></returns>
        public MooClient useCache(ISooCache cache)
        {
            this._cache = cache;
            return this;
        }
        /// <summary>
        /// 注册一个数据库连接配置
        /// </summary>
        /// <param name="loadDBByPostion"></param>

        /// <returns></returns>
        public MooClient useDataBase(Func<int , DataBase> loadDBByPostion) {
            if(loadDBByPostion == null)
            {
                return this;
            }
            if (_dbloaders == null) { 
                _dbloaders= new List<Func<int , DataBase>>();
            }
            if (_dbloaders.Contains(loadDBByPostion)==false) { 
                _dbloaders.Add(loadDBByPostion);
            }
            return this;
        }
        /// <summary>
        /// 注册一个客户类工厂
        /// </summary>
        /// <param name="clientFactory"></param>
        /// <returns></returns>
        public MooClient useClientFactory(DBClientFactory clientFactory) {
            this.ClientFactory = clientFactory;
            return this;
        }

        /// <summary>
        /// 注册一个数据库连接配置
        /// </summary>
        /// <param name="position"></param>
        /// <param name="createDBConfig"></param>
        /// <returns></returns>
        public MooClient useDataBase(int position, Func<DataBase> createDBConfig)
        {
            this.dbConfigs[position] = createDBConfig;
            return this;
        }

        /// <summary>
        /// 初始化主从注册器
        /// </summary>
        public void initModifyMediator(System.Func<SlaveTeam, SlaveTeam> createTeam) {
            this.modifyMediator = SlaveFactory.createBase();
            var slave = SlaveFactory.CreateSlave();
            slave=createTeam(slave);
            modifyMediator.signModify(slave);
        }

        public DataBase loadDBConfig(int position) {
            if (dbConfigs.ContainsKey(position))
            {
                var t = dbConfigs[position];
                if (t != null) {
                    var res= t();
                    if (res != null) { 
                        return res;
                    }
                }
            }
            if(_dbloaders !=null && _dbloaders.Count>0)
            {
                for(var i=0; i< _dbloaders.Count; i++)
                {
                    var t = _dbloaders[i];
                    if (t != null)
                    {
                        var res = t(position);
                        if (res != null)
                        {
                            return res;
                        }
                    }
                }
            }
            return null;
        }

        #region 事件调用


        /// <summary>
        ///  事件调用
        /// </summary>
        /// <param name="context"></param>
        /// <param name="operation"></param>
        /// <returns></returns>

        internal string fireOnBeforeExecute(ExeContext context, string operation) {
            if (events.onBeforeExecuteHandlers.Count > 0) {
                var res = "";
                foreach (var handler in events.onBeforeExecuteHandlers)
                {
                    if (handler != null) {
                        res += handler(context, operation);
                    }
                    
                }
                return res;
            } 
            else return null;
        }

        internal  string fireOnAfterExecute(ExeContext context, string operation)
        {
            if (events.onAfterExecuteHandlers.Count > 0)
            {
                var res = "";
                foreach (var handler in events.onAfterExecuteHandlers)
                {
                    if (handler != null)
                    {
                        res += handler(context, operation);
                    }

                }
                return res;
            }
            else return null;
        }

        internal  string fireOnExecuteError(ExeContext context, Exception ex, string operation)
        {
            if (events.onExecuteErrorHandlers.Count > 0)
            {
                var res = "";
                foreach (var handler in events.onExecuteErrorHandlers)
                {
                    if (handler != null)
                    {
                        res += handler(context, ex,operation);
                    }

                }
                return res;
            }
            else return null;
        }


        internal bool fireBuildSetFrag(SetFrag frag,SQLBuilder kit)
        {
            if (events.onBuildSetFragHandlers.Count > 0)
            {

                foreach (var handler in events.onBuildSetFragHandlers)
                {
                    if (handler != null)
                    {
                        var t = handler(frag, kit);
                        if (t == false) { 
                            return false;
                        }
                    }

                }
                return true;
            }
            return true;
        }

        internal bool fireBuildWhereFrag(WhereFrag frag, SQLBuilder kit)
        {
            if (events.onBuildWhereFragHandlers.Count > 0)
            {

                foreach (var handler in events.onBuildWhereFragHandlers)
                {
                    if (handler != null)
                    {
                        var t = handler(frag, kit);
                        if (t == false)
                        {
                            return false;
                        }
                    }

                }
                return true;
            }
            return true;
        }
        internal void fireCreatedSQL(string SQL, SQLBuilder kit)
        {
            if (events.onCreatedSQLHandlers.Count > 0)
            {

                foreach (var handler in events.onCreatedSQLHandlers)
                {
                    if (handler != null)
                    {
                        handler(SQL, kit);
                    }

                }
            }

        }
        #endregion

    }
}
