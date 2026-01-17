using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;
using mooSQL.auth;
using mooSQL.data.clip;
using mooSQL.data.model;
using mooSQL.linq;

namespace mooSQL.data
{
    /// <summary>
    /// just so,一个平平无奇的仓储模式实现
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public partial class SooRepository<T> : ISooRepository<T> where T : class, new()
    {
        /// <summary>
        /// 数据库实例
        /// </summary>
        protected DBInstance DBLive;

        private Action<string> _onPrint;
        /// <summary>
        /// 事务执行器，用于执行SQL语句。
        /// </summary>
        public DBExecutor Executor { get; private set; }
        /// <summary>
        /// 实体转换器，用于将数据库字段转换为实体属性
        /// </summary>
        public EntityTranslator Translator { get; set; }
        /// <summary>
        /// 基础仓储实现
        /// </summary>
        /// <param name="DB"></param>
        public SooRepository(DBInstance DB) { 
            this.DBLive = DB;
            this.Translator = DBLive.client.ClientFactory.getEntityTranslator(); 
        }
        /// <summary>
        /// 打印SQL语句的回调函数，可用于调试
        /// </summary>
        /// <param name="onPrint"></param>
        /// <returns></returns>
        public SooRepository<T> print(Action<string> onPrint) { 
            this._onPrint = onPrint;
            return this;
        }
        /// <summary>
        /// 注册一个事务上下午，将被后续的执行所使用
        /// </summary>
        /// <param name="executor"></param>
        /// <returns></returns>
        public SooRepository<T> useTransaction(DBExecutor executor)
        {
            this.Executor = executor;
            return this;
        }

        /// <summary>
        /// 获取SQL构建器实例，用于构建SQL语句。
        /// </summary>
        /// <returns></returns>
        protected virtual SQLBuilder getKit() { 
            var kit = DBLive.useSQL();
            if (this._onPrint != null) {
                kit.print(this._onPrint);
            }
            if (this.Executor != null)
                kit.useTransaction(this.Executor);
            return kit;
        }
        /// <summary>
        /// 计数
        /// </summary>
        /// <param name="whereExpression"></param>
        /// <returns></returns>
        public int Count(Expression<Func<T, bool>> whereExpression)
        {
            var q = DBLive.useDbBus<T>();
            return q.Where(whereExpression).Count();
        }
        /// <summary>
        /// 检查是否存在
        /// </summary>
        /// <param name="whereExpression"></param>
        /// <returns></returns>
        public bool IsAny(Expression<Func<T, bool>> whereExpression)
        {
            var q = DBLive.useDbBus<T>();
            var res= q.Where(whereExpression).Count();
            return res > 0;
        }
        /// <summary>
        /// 删除
        /// </summary>
        /// <param name="deleteObj"></param>
        /// <returns></returns>
        public bool Delete(T deleteObj)
        {
            var kit= getKit();
            var res= kit.delete(deleteObj);
            return res>0;
        }
        /// <summary>
        /// 批量删除
        /// </summary>
        /// <param name="deleteObjs"></param>
        /// <returns></returns>
        public int Delete(IEnumerable<T> deleteObjs)
        {
            var kit = getKit();
            return kit.delete(deleteObjs);
        }

        /// <summary>
        /// 删除指定条件的数据
        /// </summary>
        /// <param name="whereExpression"></param>
        /// <returns></returns>
        public int Delete(Expression<Func<T, bool>> whereExpression)
        {
            var q = DBLive.useDbBus<T>();
            var res = q.Where(whereExpression).DoDelete();
            return res;
        }
        /// <summary>
        /// 按ID删除
        /// </summary>
        /// <typeparam name="K"></typeparam>
        /// <param name="id"></param>
        /// <returns></returns>
        /// <exception cref="NotSupportedException"></exception>
        public bool DeleteById<K>(K id)
        {
            var kit = getKit();
            var en = kit.DBLive.client.EntityCash.getEntityInfo(typeof(T));
            var pks = en.GetPK();
            if (pks.Count != 1) {
                throw new NotSupportedException("当前实体的主键信息不匹配！");
            }
            var pk= pks[0];

            var cc= kit.setTable(en.DbTableName)
                .where(pk.DbColumnName, id)
                .doDelete();

            return cc > 0;
        }
        /// <summary>
        /// 批量删除
        /// </summary>
        /// <typeparam name="K"></typeparam>
        /// <param name="ids"></param>
        /// <returns></returns>
        /// <exception cref="NotSupportedException"></exception>
        public int DeleteByIds<K>(IEnumerable<K> ids)
        {
            var kit = getKit();
            var en = kit.DBLive.client.EntityCash.getEntityInfo(typeof(T));
            var pks = en.GetPK();
            if (pks.Count != 1)
            {
                throw new NotSupportedException("当前实体的主键信息不匹配！");
            }
            var pk = pks[0];
            var cc = kit.setTable(en.DbTableName)
                .whereIn(pk.DbColumnName, ids)
                .doDelete();
            return cc;
        }
        /// <summary>
        /// 单个ID查询
        /// </summary>
        /// <typeparam name="K"></typeparam>
        /// <param name="id"></param>
        /// <returns></returns>
        /// <exception cref="NotSupportedException"></exception>
        public T GetById<K>(K id)
        {
            var kit = getKit();
            var en = kit.DBLive.client.EntityCash.getEntityInfo(typeof(T));
            var pks = en.GetPK();
            if (pks.Count != 1)
            {
                throw new NotSupportedException("当前实体的主键信息不匹配！");
            }
            var pk = pks[0];
            Translator.BuildSelectFrom(kit, en);
            Translator.BeforeBuildWhere(kit, en, QueryAction.QueryOne);
            var pkname = pk.DbColumnName;
            if (!string.IsNullOrWhiteSpace(en.Alias)) { 
                pkname = string.Format("{0}.{1}", en.Alias, pkname);
            }
            var tow = kit
                .where(pkname, id)
                .queryUnique<T>();
            return tow;
        }

        public R GetFieldValueById<R>(object id,Expression<Func<T,R>> fieldselector)
        {

            var kit = getKit();
            return kit.findFieldValue<T, R>(id, fieldselector);

        }

        /// <summary>
        /// 按ID批量查询
        /// </summary>
        /// <typeparam name="K"></typeparam>
        /// <param name="ids"></param>
        /// <returns></returns>
        /// <exception cref="NotSupportedException"></exception>
        public List<T> GetByIds<K>(List<K> ids)
        {
            var kit = getKit();
            var en = kit.DBLive.client.EntityCash.getEntityInfo(typeof(T));
            var pks = en.GetPK();
            if (pks.Count != 1)
            {
                throw new NotSupportedException("当前实体的主键信息不匹配！");
            }
            var pk = pks[0];
            Translator.BuildSelectFrom(kit, en);
            Translator.BeforeBuildWhere(kit, en, QueryAction.QueryOne);
            var pkname = pk.DbColumnName;
            if (!string.IsNullOrWhiteSpace(en.Alias))
            {
                pkname = string.Format("{0}.{1}", en.Alias, pkname);
            }
            var tow = kit
                .whereIn(pkname, ids)
                .query<T>();
            return tow.ToList();
        }
        /// <summary>
        /// 按ID批量查询
        /// </summary>
        /// <param name="ids"></param>
        /// <returns></returns>
        /// <exception cref="NotSupportedException"></exception>
        public List<T> GetByIds(IEnumerable ids)
        {
            var en = DBLive.client.EntityCash.getEntityInfo(typeof(T));
            var pks = en.GetPK();
            if (pks.Count != 1)
            {
                throw new NotSupportedException("当前实体的主键信息不唯一！");
            }
            var pk = pks[0];
            var kit = DBLive.useSQL();
            Translator.BuildSelectFrom(kit, en);
            Translator.BeforeBuildWhere(kit, en, QueryAction.QueryOne);
            var pkname = pk.DbColumnName;
            if (!string.IsNullOrWhiteSpace(en.Alias))
            {
                pkname = string.Format("{0}.{1}", en.Alias, pkname);
            }
            var tow = kit
                .whereIn(pkname, ids)
                .query<T>();
            return tow.ToList();
        }
        /// <summary>
        /// 按ID批量查询
        /// </summary>
        /// <typeparam name="K"></typeparam>
        /// <param name="ids"></param>
        /// <returns></returns>
        public List<T> GetByIds<K>(params K[] ids) { 
            return GetByIds(ids);
        }
        /// <summary>
        /// 查询全部数据
        /// </summary>
        /// <returns></returns>
        public List<T> GetList()
        {
            var kit = getKit();
            var en = kit.DBLive.client.EntityCash.getEntityInfo(typeof(T));
            Translator.BuildSelectFrom(kit, en);
            Translator.BeforeBuildWhere(kit, en, QueryAction.QueryList);
            var tow = kit.query<T>();
            return tow.ToList();
        }
        /// <summary>
        /// 查询前N条数据
        /// </summary>
        /// <param name="top"></param>
        /// <returns></returns>
        public List<T> GetList(int top)
        {
            var kit = getKit();
            var en = kit.DBLive.client.EntityCash.getEntityInfo(typeof(T));
            Translator.BuildSelectFrom(kit, en);
            Translator.BeforeBuildWhere(kit, en, QueryAction.QueryList);
            var tow = kit.top(top)
                .query<T>();
            return tow.ToList();
        }
        /// <summary>
        /// 自定义查询条件，获取列表数据。
        /// </summary>
        /// <param name="onBuildSQL"></param>
        /// <returns></returns>
        public List<T> GetList(Action<SQLBuilder> onBuildSQL)
        {
            var kit = getKit();
            var en = kit.DBLive.client.EntityCash.getEntityInfo(typeof(T));
            Translator.BuildSelectFrom(kit, en);
            Translator.BeforeBuildWhere(kit, en, QueryAction.QueryList);
            if (onBuildSQL != null) {
                onBuildSQL(kit);
            }
            
            var tow = kit
                .query<T>();
            return tow.ToList();
        }
        /// <summary>
        /// 自定义查询条件，获取列表数据。支持自定义SQL片段。
        /// </summary>
        /// <param name="filterClip"></param>
        /// <returns></returns>
        public List<T> GetList(Action<SQLClip, T> filterClip)
        {
            var q = DBLive.useClip();
            q.from<T>(out var a);

            if (filterClip != null)
            {
                filterClip(q, a);
                var res = q.select(a)
                    .queryList()
                    .ToList();
                return res;
            }
            return new List<T>();
        }
        /// <summary>
        /// 获取下级列表数据，支持最多50层递归查询。
        /// </summary>
        /// <typeparam name="R"></typeparam>
        /// <param name="keySelector"></param>
        /// <param name="parentVal"></param>
        /// <returns></returns>
        /// <exception cref="NotSupportedException"></exception>
        public List<T> GetChildList<R>(Expression<Func<T, R>> keySelector, R parentVal, Action<SQLClip, T> filterMore = null) {
            //先获取关联定义，然后递归查询所有下级
            //var cont = new FastCompileContext();
            var kit = DBLive.useSQL();
            //cont.initByBuilder(kit);
            //var fiedv = new FieldVisitor(cont, false);
            //var fid = fiedv.FindField(keySelector);
            //更改为从缓存中读取
            var fid = DBLive.FindFieldName(keySelector);

            if (string.IsNullOrWhiteSpace(fid)) { 
                throw new NotSupportedException("定义的字段过滤条件无效！未找到相应的数据库字段！");
            }

            var en = DBLive.client.EntityCash.getEntityInfo(typeof(T));
            var pks = en.GetPK();
            if (pks.Count != 1)
            {
                throw new NotSupportedException("当前实体的主键信息未定义或者不唯一！只支持单个主键的实体！");
            }
            var lv = 0;
            var fkVals = new List<R>() { parentVal };
            var res = new List<T>();
            var pk = pks[0];
            //为防止自循环，过滤已加载的ID
            var loadedIds = new List<R>() { parentVal };

            while (fkVals.Count > 0) { 
                var lvlist= this.LoadChildValues(kit,  en.DbTableName, fid,  fkVals,filterMore);
                if(lvlist==null || lvlist.Count==0)
                    break;
                res.AddRange(lvlist);
                //获取主键值

                var lvPks= new List<R>();
                foreach (var lvitem in lvlist) { 
                    var pkVal= (R)pk.PropertyInfo.GetValue(lvitem) ;
                    if (pkVal != null && !loadedIds.Contains(pkVal)) {
                        lvPks.Add(pkVal);
                        loadedIds.Add(pkVal);
                    }
                }
                fkVals = lvPks;
                lv++;
                if (lv > 50) throw new NotSupportedException("递归层级过多，可能存在无限循环！最大支持50层！");
            }
            return res;
        }

        private List<T> LoadChildValues<R>(SQLBuilder kit,string tbname, string fk, List<R> pkVal, Action<SQLClip,T> filterMore = null) {
            kit.clear();
            if (filterMore != null) {
                //自定义过滤条件，使用Clip模式执行SQL语句。此时，性能稍差，但提供强类型的过滤。
                return kit.useClip((c) =>
                {
                    c.from<T>(out var a)
                    .useSQL((k) => {
                        k.whereIn(fk, pkVal);
                    });
                    filterMore(c, a);
                    return c.select(a)
                    .queryList();
                }).ToList();
            }
            return kit
                .from(tbname)
                .whereIn(fk, pkVal)
                .query<T>()
                .ToList();
        }


        /// <summary>
        /// 获取树形列表数据，支持最多50层递归查询。
        /// </summary>
        /// <typeparam name="R"></typeparam>
        /// <param name="keySelector"></param>
        /// <param name="parentVal"></param>
        /// <returns></returns>
        /// <exception cref="NotSupportedException"></exception>
        public TreeListOutput<T> GetTreeList<R>(Expression<Func<T, R>> keySelector, R parentVal, Action<SQLClip, T> filterMore =null)
        {
            //先获取关联定义，然后递归查询所有下级

            var kit = DBLive.useSQL();
            //var cont = new FastCompileContext();
            //cont.initByBuilder(kit);
            //var fiedv = new FieldVisitor(cont, false);
            //var fid = fiedv.FindField(keySelector);
            var fie=DBLive.FindField(keySelector);
            var fid = fie.ToSQLField(false,DBLive);
            if (string.IsNullOrWhiteSpace(fid))
            {
                throw new NotSupportedException("定义的字段过滤条件无效！未找到相应的数据库字段！");
            }
            var fk = fie.Column;

            var en = kit.DBLive.client.EntityCash.getEntityInfo(typeof(T));
            var pks = en.GetPK();
            if (pks.Count != 1)
            {
                throw new NotSupportedException("当前实体的主键信息未定义或者不唯一！只支持单个主键的实体！");
            }
            var lv = 0;
            var fkVals = new List<R>() { parentVal };
            var res = new TreeListOutput<T>();
            var pk = pks[0];
            var topNodes= new List<TreeNodeOutput<T>>();


            var preLvNodes = topNodes;
            var total = 0;
            //为防止自循环和无限循环，每次取下级时，如果其主键在之前取过的父级中，则忽略它。
            var paPks = new List<R>() { parentVal };

            while (fkVals.Count > 0)
            {
                var lvlist = this.LoadChildValues(kit, en.DbTableName, fid, fkVals,filterMore);
                if (lvlist == null || lvlist.Count == 0)
                    break;

                //获取主键值
                var lvNodes= new List<TreeNodeOutput<T>>();
                var lvPks = new List<R>();
                foreach (var lvitem in lvlist)
                {
                    //获取主键值
                    var pkObj = pk.PropertyInfo.GetValue(lvitem);
                    var pkVal = (R)pkObj;
                    if (pkVal != null  && !paPks.Contains(pkVal))
                    {
                        lvPks.Add(pkVal);
                        paPks.Add(pkVal);
                    }
                    //外键值
                    var fkVal = fk.PropertyInfo.GetValue(lvitem);
                    var fkR= (R)fkVal; 
                    var nodeVal = new TreeNodeOutput<T>()
                    {
                        Record = lvitem,
                        Children = new List<TreeNodeOutput<T>>(),
                        Level = lv + 1,
                        PKValue = pkVal,
                        FKValue = fkR
                    };
                    lvNodes.Add(nodeVal);
                    bool isTop = true;
                    foreach (var node in preLvNodes) {

                        if (node.PKValue.ToString() == fkR.ToString()) {
                            node.Children.Add(nodeVal);
                            total++;
                            isTop = false;
                            break;
                        }
                    }
                    if (isTop && fkR.ToString()==parentVal.ToString()) { 
                        topNodes.Add(nodeVal);
                        total++;
                    }
                }
                fkVals = lvPks;
                lv++;
                //初始化下一层的节点集合
                preLvNodes= lvNodes;
                if (lv > 50) throw new NotSupportedException("递归层级过多，可能存在无限循环！最大支持50层！");
            }
            res.Nodes = topNodes;
            return res;
        }

        /// <summary>
        /// 获取列表查询结果。
        /// </summary>
        /// <param name="para"></param>
        /// <returns></returns>
        public List<T> GetList(QueryPara para)
        {
            var kit = getKit();
            var en = kit.DBLive.client.EntityCash.getEntityInfo(typeof(T));
            Translator.PatchSQLByQueryPara(para, kit, en);
            var t = kit.query<T>().ToList();
            return t;
        }


        /// <summary>
        /// 获取分页查询结果
        /// </summary>
        /// <param name="para"></param>
        /// <returns></returns>
        public PageOutput<T> GetPageList(QueryPara para)
        {
            
            para = CheckQueryPara(para);
            var kit = getKit();
            var en = kit.DBLive.client.EntityCash.getEntityInfo(typeof(T));
            Translator.PatchSQLByQueryPara(para, kit, en);

            //如果无排序条件，则默认按主键降序
            if (kit.current.orderPart.Count == 0) { 
                var pks = en.GetPK();
                if (pks !=null) { 
                    foreach (var pk in pks) {
                        var name = pk.DbColumnName;
                        if(!string.IsNullOrWhiteSpace(en.Alias))
                            name = string.Format("{0}.{1}", en.Alias, name);
                        
                        kit.orderBy(name+" desc");
                    }
                    
                }
            }

            var t = kit.queryPaged<T>();
            return t;
        }
        /// <summary>
        /// 获取分页查询结果。
        /// </summary>
        /// <param name="para"></param>
        /// <returns></returns>
        public DataTable GetPageData(QueryPara para)
        {

            para = CheckQueryPara(para);
            var kit = getKit();
            var en = kit.DBLive.client.EntityCash.getEntityInfo(typeof(T));
            Translator.PatchSQLByQueryPara(para, kit, en);

            //如果无排序条件，则默认按主键降序
            if (kit.current.orderPart.Count == 0)
            {
                var pks = en.GetPK();
                if (pks != null)
                {
                    foreach (var pk in pks)
                    {
                        var name = pk.DbColumnName;
                        if (!string.IsNullOrWhiteSpace(en.Alias))
                            name = string.Format("{0}.{1}", en.Alias, name);

                        kit.orderBy(name + " desc");
                    }

                }
            }

            var t = kit.query();
            return t;
        }

        /// <summary>
        /// 获取分页查询结果，支持自定义查询条件(clip)。
        /// </summary>
        /// <param name="pageSize"></param>
        /// <param name="pageNum"></param>
        /// <param name="filterClip"></param>
        /// <returns></returns>
        public PageOutput<T> GetPageList(int pageSize,int pageNum,Action<SQLClip, T> filterClip=null)
        {
            var q = DBLive.useClip();
            q.from<T>(out var a);

            if (filterClip != null)
            {
                filterClip(q, a);
            }
            var tar= q.select(a)
                .setPage(pageSize, pageNum)
                .queryPage();
            return tar;
        }

        /// <summary>
        /// 检查查询参数，并修正默认值。
        /// </summary>
        /// <param name="para"></param>
        /// <returns></returns>
        protected virtual QueryPara CheckQueryPara(QueryPara para)
        {
            //检查翻页参数
            if (para == null) { 
                para = new QueryPara();
                para.pageNum = 1;
                para.pageSize = 50;
            }
            if (para.pageNum==null|| para.pageNum<=0)
            {
                para.pageNum = 1;
            }
            if (para.pageSize==null|| para.pageSize <= 0) { 
                para.pageSize = 50;
            }
            return para;
        }

        /// <summary>
        /// 自定义查询条件，获取分页数据。
        /// </summary>
        /// <param name="onBuildSQL"></param>
        /// <returns></returns>
        public PageOutput<T> GetPageList(Action<SQLBuilder> onBuildSQL)
        {
            var kit = getKit();
            var en = kit.DBLive.client.EntityCash.getEntityInfo(typeof(T));
            if (onBuildSQL != null)
            {
                onBuildSQL(kit);
            }
            var t = kit.queryPaged<T>();
            return t;
        }
        /// <summary>
        /// 自定义查询条件，获取分页数据。
        /// </summary>
        /// <param name="para"></param>
        /// <param name="onBuildSQL"></param>
        /// <returns></returns>
        public PageOutput<T> GetPageList(QueryPara para, Action<SQLBuilder,EntityInfo> onBuildSQL)
        {
            para = CheckQueryPara(para);
            var kit = getKit();
            var en = kit.DBLive.client.EntityCash.getEntityInfo(typeof(T));
            Translator.PatchSQLByQueryPara(para, kit, en);
            if (onBuildSQL != null) {
                onBuildSQL(kit,en);
            }
            var t = kit.queryPaged<T>();
            return t;
        }

        /// <summary>
        /// 按条件查询
        /// </summary>
        /// <param name="whereExpression"></param>
        /// <returns></returns>
        public List<T> GetList(Expression<Func<T, bool>> whereExpression)
        {
            var q = DBLive.useDbBus<T>();
            var t= q.Where(whereExpression)
                .ToList();
            return t;
        }
        /// <summary>
        /// 获取第一个
        /// </summary>
        /// <param name="whereExpression"></param>
        /// <returns></returns>
        public T GetFirst(Expression<Func<T, bool>> whereExpression)
        {
            var q = DBLive.useDbBus<T>();
            var t = q.Where(whereExpression)
                .Take(1)
                .ToList();
            if (t.Count > 0) { 
                return t[0];
            }
            return default(T);
        }
        /// <summary>
        /// 获取第一个，并自定义查询条件。
        /// </summary>
        /// <param name="filterClip"></param>
        /// <returns></returns>
        public T GetFirst(Action<SQLClip,T> filterClip)
        {
            var q = DBLive.useClip();
            q.from<T>(out var a);

            if (filterClip != null) { 
                filterClip(q,a);
                var res= q.select(a)
                    .queryList()
                    .ToList();
                if (res.Count > 0) { 
                    return res[0];
                }
            }
            return default(T);
        }
        /// <summary>
        /// 获取第一个，并自定义查询条件。查不到时返回默认值。
        /// </summary>
        /// <typeparam name="R"></typeparam>
        /// <param name="filterClip"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public T GetFirst<R>(Expression<Func<T, R>> filterClip, R value) {

            var kit = DBLive.useSQL();
            //var cont = new FastCompileContext();
            //cont.EntityType = typeof(T);
            //cont.initByBuilder(kit);
            //var fiedv= new FieldVisitor(cont,false);
            //var fid = fiedv.FindField(filterClip);
            var fid = DBLive.FindFieldName(filterClip);
            if (!string.IsNullOrWhiteSpace(fid)) { 
                var tbname= DBLive.client.EntityCash.getTableName(typeof(T));
                kit.from(tbname);
                kit.where(fid,value);
                return kit.queryFirst<T>();
            }
            return default(T);
        }

        /// <summary>
        /// 在实体进行保存前，执行的操作。
        /// </summary>
        /// <param name="entity"></param>
        protected virtual void OnBeforeSave(T entity) { }
        /// <summary>
        /// 在实体进行保存后，执行的操作。
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="res"></param>
        protected virtual void OnAfterSave(T entity,int res) { }
        /// <summary>
        /// 单个插入
        /// </summary>
        /// <param name="insertObj"></param>
        /// <returns></returns>
        public bool Insert(T insertObj)
        {
            OnBeforeSave(insertObj);
            var kit = getKit();
            var res = kit.insert(insertObj);
            OnAfterSave(insertObj,res);
            return res>0;
        }
        /// <summary>
        /// 批量插入
        /// </summary>
        /// <param name="insertObjs"></param>
        /// <returns></returns>
        public int InsertRange(IEnumerable<T> insertObjs)
        {
            var kit = getKit();
            var cc = 0;
            foreach (var obj in insertObjs) {
                OnBeforeSave(obj);
                var c= kit.clear().insert(obj);
                OnAfterSave(obj,cc);
                if (c > 0) {
                    //执行失败的返回为-1，不能直接累计
                    cc += c;
                }
                
            }
            
            return cc ;
        }
        /// <summary>
        /// 更新
        /// </summary>
        /// <param name="updateObj"></param>
        /// <returns></returns>
        public bool Update(T updateObj)
        {
            OnBeforeSave(updateObj);
            var kit = getKit();
            var res = kit.update(updateObj);
            OnAfterSave(updateObj,res);
            return res > 0;
        }
        /// <summary>
        /// 批量更新
        /// </summary>
        /// <param name="updateObjs"></param>
        /// <returns></returns>
        public int UpdateRange(IEnumerable<T> updateObjs)
        {
            var kit = getKit();
            var cc = 0;
            foreach (var obj in updateObjs)
            {
                OnBeforeSave(obj);
                var c= kit.clear().update(obj);
                OnAfterSave(obj,cc);
                if (c > 0) {
                    //执行失败的返回为-1，不能直接累计
                    cc += c;
                }
            }

            return cc;
        }
        /// <summary>
        /// 批量更新
        /// </summary>
        /// <param name="updateObjs"></param>
        /// <returns></returns>
        public int UpdateRange(T[] updateObjs)
        {
            var kit = getKit();
            var cc = 0;
            foreach (var obj in updateObjs)
            {
                OnBeforeSave(obj);
                var c = kit.clear().update(obj);
                OnAfterSave(obj,cc);
                if (c > 0) {
                    //执行失败的返回为-1，不能直接累计
                    cc += c;
                }
            }

            return cc;
        }
        /// <summary>
        /// 执行更新
        /// </summary>
        /// <param name="columns"></param>
        /// <param name="whereExpression"></param>
        /// <returns></returns>
        public bool Update(Expression<Func<T, T>> columns, Expression<Func<T, bool>> whereExpression)
        {
            var q = DBLive.useDbBus<T>();

            var cc= q.Set(columns)
                .Where(whereExpression)
                .DoUpdate();
            return cc > 0;
        }
        /// <summary>
        /// 保存
        /// </summary>
        /// <param name="updateObj"></param>
        /// <returns></returns>
        public bool Save(T updateObj)
        {
            OnBeforeSave(updateObj);
            var kit = getKit();
            var res = kit.save(updateObj);
            OnAfterSave(updateObj,res);
            return res > 0;
        }
        /// <summary>
        /// 批量保存
        /// </summary>
        /// <param name="updateObjs"></param>
        /// <returns></returns>
        public int SaveRange(IEnumerable<T> updateObjs)
        {
            var kit = getKit();
            var cc = 0;
            foreach (var obj in updateObjs)
            {
                OnBeforeSave(obj);
                var c= kit.clear().save(obj);
                OnAfterSave(obj,c);
                if (c > 0) {
                    //执行失败的返回为-1，不能直接累计
                    cc += c;
                }
            }

            return cc;
        }
    }
}
