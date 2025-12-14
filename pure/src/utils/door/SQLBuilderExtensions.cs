using mooSQL.data.clip;
using mooSQL.data.utils;

using mooSQL.linq;
using mooSQL.utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing.Printing;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.InteropServices;
using System.Text;


namespace mooSQL.data
{
    /// <summary>
    /// 内建的SQLBuilder扩展
    /// </summary>
    public static class MooSQLBuilderExtensions
    {


        /// <summary>
        /// 使用某个实体类
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="builder"></param>
        /// <returns></returns>
        public static SQLBuilder<T> use<T>(this SQLBuilder builder) { 
            var tar= new SQLBuilder<T>(builder.DBLive);
            return tar;
        }
        /// <summary>
        /// 使用某个实体类的仓库类。注意！在启用事务时，将继承调用者的事务上下文。
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="builder"></param>
        /// <returns></returns>
        public static SooRepository<T> useRepo<T>(this SQLBuilder builder) where T : class, new()
        {
            var tar = builder.DBLive.useRepo<T>();
            if (builder.Executor != null) { 
                tar.useTransaction(builder.Executor);
            }
            return tar;
        }

        /// <summary>
        /// 默认返回一个新的SQLClip，如果inherit为true则继承当前的上下文。注意！在启用事务时，将继承调用者的事务上下文。
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="inherit"></param>
        /// <returns></returns>
        public static SQLClip useClip(this SQLBuilder builder,bool inherit=false)
        {
            if (inherit)
            {
                return builder.Client.ClientFactory.useClip(builder.DBLive, builder);
            }
            else if(builder.Executor !=null){
                var kit = builder.DBLive.useSQL();
                kit.useTransaction(builder.Executor);
                return builder.Client.ClientFactory.useClip(builder.DBLive, kit);
            }
            var tar = builder.Client.ClientFactory.useClip(builder.DBLive, null);
            return tar;
        }
        /// <summary>
        /// 使用某个SQLClip，执行完毕后会自动释放。默认不会带入当前上下文。
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="clipAction"></param>
        public static R useClip<R>(this SQLBuilder builder,Func<SQLClip,R> clipAction,bool inherit=false)
        {
            var clip = useClip(builder, inherit);
            return clipAction(clip);
        }
        /// <summary>
        /// 增加自定义缓存的
        /// </summary>
        /// <typeparam name="R"></typeparam>
        /// <param name="builder"></param>
        /// <param name="cacheKey"></param>
        /// <param name="clipAction"></param>
        /// <param name="inherit"></param>
        /// <returns></returns>
        public static R useClip<R>(this SQLBuilder builder,string cacheKey, Func<SQLClip, R> clipAction, bool inherit = false)
        {
            if (builder.Client.Cache != null && builder.Client.Cache.ContainsKey(cacheKey)) { 
                return builder.Client.Cache.Get<R>(cacheKey);
            }
            var clip = useClip(builder, inherit);
            return clipAction(clip);
        }

        /// <summary>
        /// 使用某个SQLClip，执行完毕后会自动释放。默认不会带入当前上下文。
        /// </summary>
        /// <typeparam name="R"></typeparam>
        /// <param name="builder"></param>
        /// <param name="clipAction"></param>
        /// <param name="val"></param>
        /// <param name="inherit"></param>
        /// <returns></returns>
        public static SQLBuilder useClip<R>(this SQLBuilder builder, out R val, Func<SQLClip, R> clipAction, bool inherit = false)
        {
            var clip = useClip(builder, inherit);
            val= clipAction(clip);
            return builder;
        }

        /// <summary>
        /// 执行插入，返回-1时为发生异常。
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="builder"></param>
        /// <param name="entity"></param>
        /// <returns></returns>
        public static int insert<T>(this SQLBuilder builder, T entity) {
            builder.clear();
            var res= builder.Client.Translator.prepareInsert(builder, entity,typeof(T));
            if (res.Status) {
                return builder.doInsert();
            }
            builder.Client.Loggor.LogError(res.Message);
            return -1;
        }
        /// <summary>
        /// 执行批量插入，通过BulkBase实现。注意！在启用事务时，将继承调用者的事务上下文。
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="builder"></param>
        /// <param name="entity"></param>
        /// <returns></returns>
        public static int insertList<T>(this SQLBuilder builder,List<T> entity)
        {
            var bk= builder.DBLive.useBulk();
            if (builder.Executor != null) { 
                bk.useTransaction(builder.Executor);
            }
            bk.addList(entity);
            return bk.doInsert();
        }
        /// <summary>
        /// 按照指定的实体类型执行插入，返回-1时为发生异常。
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="entity"></param>
        /// <param name="EntityType"></param>
        /// <returns></returns>
        public static int insertByType(this SQLBuilder builder, object entity,Type EntityType)
        {
            builder.clear();
            var res = builder.Client.Translator.prepareInsert(builder, entity, EntityType);
            if (res.Status)
            {
                return builder.doInsert();
            }
            builder.Client.Loggor.LogError(res.Message);
            return -1;
        }
        /// <summary>
        /// 创建插入命令
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="builder"></param>
        /// <param name="entity"></param>
        /// <returns></returns>
        public static SQLCmd toInsert<T>(this SQLBuilder builder, T entity)
        {
            builder.clear();
            var res= builder.Client.Translator.prepareInsert(builder, entity,typeof(T));
            if (res.Status)
                return builder.toInsert();
            builder.Client.Loggor.LogError(res.Message);
            return null;
        }

        /// <summary>
        /// 自动使用主键作为update条件,返回-1时为发生异常。
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="builder"></param>
        /// <param name="entity"></param>
        /// <returns></returns>
        public static int update<T>(this SQLBuilder builder, T entity)
        {
            builder.clear();
            var res= builder.Client.Translator.prepareUpdate(builder, entity,typeof(T));
            if (res.Status) {
                return builder.doUpdate();
            }
            builder.Client.Loggor.LogError(res.Message);
            return -1;
        }
        /// <summary>
        /// 按照指定的实体类型执行更新，返回-1时为发生异常。
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="entity"></param>
        /// <param name="EntityType"></param>
        /// <returns></returns>
        public static int updateByType(this SQLBuilder builder, object entity,Type EntityType)
        {
            builder.clear();
            var res = builder.Client.Translator.prepareUpdate(builder, entity,EntityType);
            if (res.Status)
            {
                return builder.doUpdate();
            }
            builder.Client.Loggor.LogError(res.Message);
            return -1;
        }
        /// <summary>
        /// 按照指定的实体属性更新
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="builder"></param>
        /// <param name="entity"></param>
        /// <returns></returns>
        public static SQLCmd toUpdate<T>(this SQLBuilder builder, T entity)
        {
            builder.clear();
            var res= builder.Client.Translator. prepareUpdate(builder, entity,typeof(T));
            if (res.Status)
            {
                return builder.toUpdate();
            }
            builder.Client.Loggor.LogError(res.Message);
            return null;
        }

        /// <summary>
        /// 自动使用主键作为update条件
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="builder"></param>
        /// <param name="entity"></param>
        /// <returns></returns>
        public static int updateBy<T>(this SQLBuilder builder, T entity,string Name)
        {
            builder.clear();
            return builder.Client.Translator.updateByFieild(builder, entity, Name);
        }
        /// <summary>
        /// 按照指定的实体属性更新
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="R"></typeparam>
        /// <param name="builder"></param>
        /// <param name="entity"></param>
        /// <param name="updateKey"></param>
        /// <returns></returns>
        public static int updateBy<T,R>(this SQLBuilder builder, T entity, Expression<Func<T,R>> updateKey)
        {
            builder.clear();
            var name = loadMemberName(updateKey.Body);

            return builder.Client.Translator.updateByFieild(builder, entity, name);
        }



        private static string loadMemberName(Expression expression) { 
            if(expression is MemberExpression member)
            {
                return member.Member.Name;
            }
            return null;
        }
        /// <summary>
        /// 按照指定的属性名，执行保存
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="builder"></param>
        /// <param name="entity"></param>
        /// <param name="Name"></param>
        /// <returns></returns>
        public static int saveBy<T>(this SQLBuilder builder, T entity, string Name)
        {
            builder.clear();
            var cash = builder.DBLive.client.EntityCash;

            var field= cash.getField(typeof(T),Name);
            var val= field.PropertyInfo.GetValue(entity);
            if (val == null) { 
                throw new Exception("实体的属性对应数据库字段信息未找到！");
            }
            var has= builder.checkExistKey(field.DbColumnName,val,field.DbTableName);
            if (has)
            {
                return builder.Client.Translator.updateByFieild(builder, entity, Name);
            }
            else {
                return insert(builder, entity);
            }

        }
        /// <summary>
        /// 按照指定的属性名，执行保存
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="R"></typeparam>
        /// <param name="builder"></param>
        /// <param name="entity"></param>
        /// <param name="updateKey"></param>
        /// <returns></returns>
        public static int saveBy<T, R>(this SQLBuilder builder, T entity, Expression<Func<T, R>> updateKey)
        {

            var name = loadMemberName(updateKey.Body);

            return saveBy(builder, entity, name);
        }

        /// <summary>
        /// 保存。当禁止更新，则直接插入。当禁止插入，则直接更新。禁止保存时，直接返回0。
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="builder"></param>
        /// <param name="entity"></param>
        /// <returns></returns>
        public static int save<T>(this SQLBuilder builder, T entity) {

            var cmd = toSave(builder, entity);
            if (cmd != null) { 
                return builder.exeNonQuery(cmd);
            }
            return 0;
        }
        public static SQLCmd toSave<T>(this SQLBuilder builder, T entity)
        {

            var en = builder.Client.EntityCash.getEntityInfo<T>();

            if (en.Insertable == false && en.Updatable == false)
            {
                return null;
            }
            //主键短路检测，未设置主键的，直接插入
            var pks = en.GetPK();
            if (pks.Count == 0)
            {
                throw new Exception("无主键定义时，无法自动保存！");
            }
            foreach (var pk in pks)
            {
                if (pk.PropertyInfo.GetValue(entity) == null)
                {
                    return toInsert(builder, entity);
                }
            }
            //检查是否存在
            var ck = builder.DBLive.useSQL();
            ck.from(en.DbTableName);
            builder.Client.Translator.setPKWhere(ck, entity, en);
            var cc = ck.count();
            if (cc > 0 && en.Updatable != false)
            {
                return toUpdate(builder, entity);
            }
            else if(en.Insertable !=false)
            {
                return toInsert(builder, entity);
            }
            return null;
        }
        /// <summary>
        /// 转为保存语句
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="builder"></param>
        /// <param name="entity"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static List<SQLCmd> toSave<T>(this SQLBuilder builder,IEnumerable<T> entity)
        {

            var en = builder.Client.EntityCash.getEntityInfo<T>();

            if (en.Insertable == false && en.Updatable == false)
            {
                return null;
            }
            var cmds= new List<SQLCmd>();
            //检查是否存在
            var ck = builder.DBLive.useSQL();
            builder.Client.Translator.BuildFromPart(builder, en);
            var pks = en.GetPK();
            foreach (var pk in pks) {
                builder.select(pk.DbColumnName);
            }
            builder.Client.Translator.setPKWhere(ck, entity, en);
            var oldDt = ck.query();
            if (oldDt.Rows.Count == 0 && en.Insertable != false)
            {
                foreach (var row in entity) { 
                    cmds.Add(toInsert(builder, row));
                }
                return cmds;
            }

            foreach (var row in entity) {
                var sh = new List<string>();
                foreach (var pk in pks)
                {
                    sh.Add(string.Format("{0}='{1}'", pk.DbColumnName, pk.PropertyInfo.GetValue(row)));
                }
                var shcond = string.Join(" and ", sh);
                var rows = oldDt.Select(shcond);
                if (rows.Length > 0 && en.Updatable !=false) {
                    cmds.Add(toUpdate(builder, en));
                }
                else if (rows.Length == 0 && en.Insertable != false)
                {
                    cmds.Add(toInsert(builder, en));
                }
            }
 
            return cmds;
        }

        /// <summary>
        /// 批量保存
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="builder"></param>
        /// <param name="entity"></param>
        /// <returns></returns>
        public static int save<T>(this SQLBuilder builder, IEnumerable<T> entity) {
            var cmds = toSave(builder, entity);
            if (cmds.Count > 0) {
                return builder.exeNonQuery(cmds);
            }
            return 0;
        }


        /// <summary>
        /// 按照主键执行删除
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="builder"></param>
        /// <param name="entity"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static int delete<T>(this SQLBuilder builder, T entity)
        {
            builder.clear();
            builder.Client.Translator.prepareDelete<T>(builder, entity);
            return builder.doDelete();
        }
        /// <summary>
        /// 返回删除命令，但不执行
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="builder"></param>
        /// <param name="entity"></param>
        /// <returns></returns>
        public static SQLCmd toDelete<T>(this SQLBuilder builder, T entity)
        {
            builder.clear();
            builder.Client.Translator.prepareDelete<T>(builder, entity);
            return builder.toDelete();
        }
        /// <summary>
        /// 按照类型执行删除，但不执行
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="entity"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public static SQLCmd toDeleteByType(this SQLBuilder builder, object entity,Type type)
        {
            builder.clear();
            builder.Client.Translator.prepareDelete(builder, entity,type);
            return builder.toDelete();
        }
        /// <summary>
        /// 按照类型执行删除
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="entity"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public static int deleteByType(this SQLBuilder builder, object entity, Type type)
        {
            builder.clear();
            builder.Client.Translator.prepareDelete(builder, entity, type);
            return builder.doDelete();
        }
        /// <summary>
        /// 批量删除的命令，如果联合主键，返回多个SQL，否则返回一个SQL。
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="builder"></param>
        /// <param name="entitys"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static List<SQLCmd> toDelete<T>(this SQLBuilder builder, IEnumerable<T> entitys)
        {
            builder.clear();
            var res= new List<SQLCmd>();
            var en = builder.DBLive.client.EntityCash.getEntityInfo(typeof(T));

            bool gotWhere = false;
            var pks = en.GetPK();
            if (pks.Count == 0)
            {
                throw new Exception("无法删除！未找到主键！");
            }
            if (pks.Count == 1)
            {
                builder.setTable(en.DbTableName);
                var pk = pks[0];
                var ids = new List<Object>();
                foreach (var row in entitys)
                {
                    var val = pk.PropertyInfo.GetValue(row);
                    if (val != null)
                    {
                        ids.Add(val);
                    }
                }
                builder.whereIn(pk.DbColumnName, ids);
                res.Add(builder.toDelete());
                return res;
            }
            else
            {
                //联合主键的情况
                var bsql = new BatchSQL(builder.DBLive);
                var ids = new List<Object>();
                foreach (var row in entitys)
                {
                    builder.clear().setTable(en.DbTableName);
                    int kcc = 0;
                    foreach (var k in pks)
                    {
                        var val = k.PropertyInfo.GetValue(row);
                        if (val == null)
                        {
                            break;
                        }
                        builder.where(k.DbColumnName, val);
                        kcc++;
                    }
                    if (kcc != pks.Count)
                    {
                        continue;
                    }
                    res.Add(builder.toDelete());
                }
                return res;
            }

        }
        /// <summary>
        /// 批量删除
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="builder"></param>
        /// <param name="entitys"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static int delete<T>(this SQLBuilder builder, IEnumerable<T> entitys)
        {
            builder.clear();
            //联合主键的情况
            var bsql = builder.Client.ClientFactory.useBatchSQL(builder.DBLive);
            var sql=toDelete(builder, entitys);
            bsql.addSQLs(sql);
            return bsql.exeNonQuery();

        }


        #region 快捷的查询方法扩展
        /// <summary>
        /// 快速查询某个对象，按主键查询，借用仓储实现
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="builder"></param>
        /// <param name="ids"></param>
        /// <returns></returns>
        public static List<T> findListByIds<T>(this SQLBuilder builder, IEnumerable ids) where T : class, new()
        {
            var tow= builder.DBLive.useRepo<T>().GetByIds(ids);
            return tow.ToList();
        }
        /// <summary>
        /// 快速查询某个对象，按主键查询
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="K"></typeparam>
        /// <param name="builder"></param>
        /// <param name="ids"></param>
        /// <returns></returns>
        public static List<T> findListByIds<T, K>(this SQLBuilder builder, params K[] ids) where T : class, new()
        { 
            return findListByIds<T>(builder, ids);
        }
        /// <summary>
        /// 快速查询单个对象
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="builder"></param>
        /// <param name="doClipFilting"></param>
        /// <returns></returns>
        public static List<T> findList<T>(this SQLBuilder builder, Action<SQLClip,T> doClipFilting) where T : class, new()
        {
            var clip = builder.useClip();
            clip.from<T>(out var t);
            doClipFilting(clip, t);
            return clip.select(t)
                .queryList()
                .ToList();
        }
        /// <summary>
        /// 快速查询使用，手动指定表名，用于动态分表时使用。
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="builder"></param>
        /// <param name="tableName"></param>
        /// <param name="doClipFilting"></param>
        /// <returns></returns>
        public static List<T> findList<T>(this SQLBuilder builder,string tableName, Action<SQLClip, T> doClipFilting) where T : class, new()
        {
            var clip = builder.useClip();
            clip.from<T>(tableName,out var t);
            doClipFilting(clip, t);
            return clip.select(t)
                .queryList()
                .ToList();
        }
        /// <summary>
        /// 快速查询某个对象
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="R"></typeparam>
        /// <param name="builder"></param>
        /// <param name="doClipFilting"></param>
        /// <returns></returns>
        public static List<R> findList<T,R>(this SQLBuilder builder, Func<SQLClip, T, SQLClip<R>> doClipFilting) where T : class, new()
        {
            var clip = builder.useClip();
            clip.from<T>(out var t);
            var tar= doClipFilting(clip, t);
            return tar
                .queryList()
                .ToList();
        }

        /// <summary>
        /// 快速查询某个对象，分页查询。
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="builder"></param>
        /// <param name="pageSize"></param>
        /// <param name="pageNum"></param>
        /// <param name="doClipFilting"></param>
        /// <returns></returns>
        public static PageOutput<T> findPageList<T>(this SQLBuilder builder,int pageSize,int pageNum, Action<SQLClip, T> doClipFilting) where T : class, new()
        {
            var clip = builder.useClip();
            clip.from<T>(out var t);
            doClipFilting(clip, t);
            return clip.select(t)
                .setPage(pageSize, pageNum)
                .queryPage();
        }
        /// <summary>
        /// 快速查询某个对象，分页查询。手动指定表名，用于动态分表时使用。
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="builder"></param>
        /// <param name="pageSize"></param>
        /// <param name="pageNum"></param>
        /// <param name="tableName"></param>
        /// <param name="doClipFilting"></param>
        /// <returns></returns>
        public static PageOutput<T> findPageList<T>(this SQLBuilder builder, int pageSize, int pageNum, string tableName, Action<SQLClip, T> doClipFilting) where T : class, new()
        {
            var clip = builder.useClip();
            clip.from<T>(tableName, out var t);
            doClipFilting(clip, t);
            return clip.select(t)
                .setPage(pageSize, pageNum)
                .queryPage();
        }
        /// <summary>
        /// 快速查询某个对象，分页查询。手动指定表名，用于动态分表时使用。
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="R"></typeparam>
        /// <param name="builder"></param>
        /// <param name="pageSize"></param>
        /// <param name="pageNum"></param>
        /// <param name="doClipFilting"></param>
        /// <returns></returns>
        public static PageOutput<R> findPageList<T, R>(this SQLBuilder builder, int pageSize, int pageNum, Func<SQLClip, T, SQLClip<R>> doClipFilting) where T : class, new()
        {
            var clip = builder.useClip();
            clip.from<T>(out var t);
            var tar = doClipFilting(clip, t);
            return tar
                .setPage(pageSize, pageNum)
                .queryPage();
        }
        /// <summary>
        /// 根据主键快速查询，借用仓储实现
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="builder"></param>
        /// <param name="PK"></param>
        /// <returns></returns>
        public static T findRowById<T>(this SQLBuilder builder, object PK) where T : class, new()
        {
            var repo = builder.useRepo<T>();
            return repo.GetById(PK);
        }

        /// <summary>
        /// 快速查询某个实体，不唯一时返回null
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="builder"></param>
        /// <param name="doClipFilting"></param>
        /// <returns></returns>
        public static T findRow<T>(this SQLBuilder builder, Action<SQLClip, T> doClipFilting) where T : class, new()
        {
            var clip = builder.useClip();
            clip.from<T>(out var t);
            doClipFilting(clip, t);
            return clip.select(t)
                .queryUnique();
        }
        /// <summary>
        /// 快速查询某个实体，并获取自定义的结果，不唯一时返回null。注意：使用本方法时，Func请写全变量类型，则可以不用写泛型参数，如findField((SQLClip c, SysMapdata w) =>c.where(() => w.No, No).select(() => w.VSTOExcelFile));
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="R"></typeparam>
        /// <param name="builder"></param>
        /// <param name="doClipFilting"></param>
        /// <returns></returns>
        public static R findField<T,R>(this SQLBuilder builder, Func<SQLClip, T,SQLClip<R>> doClipFilting) where T : class, new()
        {
            var clip = builder.useClip();
            clip.from<T>(out var t);
            var tar= doClipFilting(clip, t);
            return tar.queryUnique();
        }

        /// <summary>
        /// 根据主键值，查找某个字段的值。
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="R"></typeparam>
        /// <param name="builder"></param>
        /// <param name="PKValue"></param>
        /// <param name="doClipSelect"></param>
        /// <returns></returns>
        public static R findFieldValue<T, R>(this SQLBuilder builder,object PKValue, Func<SQLClip, T, SQLClip<R>> doClipSelect) where T : class, new()
        {
            var clip = builder.useClip();
            clip.from<T>(out var t);
            clip.setFromAsName("t");
            clip.wherePKIs<T>(PKValue);
            var tar = doClipSelect(clip, t);
            return tar.queryUnique();
        }
        /// <summary>
        /// 自定义执行条件和字段选择，返回列表值
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="R"></typeparam>
        /// <param name="builder"></param>
        /// <param name="doClipFilting"></param>
        /// <returns></returns>
        public static List<R> findFieldValues<T, R>(this SQLBuilder builder, Func<SQLClip, T, SQLClip<R>> doClipFilting) where T : class, new()
        {
            var clip = builder.useClip();
            clip.from<T>(out var t);
            var tar = doClipFilting(clip, t);
            return tar.queryList().ToList();
        }
        /// <summary>
        /// 快速查询某个实体的数量，自定义条件
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="builder"></param>
        /// <param name="doClipFilting"></param>
        /// <returns></returns>
        public static int countBy<T>(this SQLBuilder builder, Action<SQLClip, T> doClipFilting) where T : class, new()
        {
            var clip = builder.useClip();
            clip.from<T>(out var t);
            doClipFilting(clip, t);
            return clip.count();
        }

        /// <summary>
        /// 快速修改实体，按照自定义条件，需要手写set、where部分。
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="builder"></param>
        /// <param name="doClipFilting"></param>
        /// <returns></returns>
        public static int modifyBy<T>(this SQLBuilder builder, Action<SQLClip, T> doClipFilting) where T : class, new()
        {
            var clip = builder.useClip();
            clip.setTable<T>(out var t);
            doClipFilting(clip, t);
            return clip.doUpdate();
        }
        /// <summary>
        /// 快速删除实体，按照自定义条件，需要手写where部分。
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="builder"></param>
        /// <param name="doClipFilting"></param>
        /// <returns></returns>
        public static int removeBy<T>(this SQLBuilder builder, Action<SQLClip, T> doClipFilting) where T : class, new()
        {
            var clip = builder.useClip();
            clip.setTable<T>(out var t);
            doClipFilting(clip, t);
            return clip.doDelete();
        }
        /// <summary>
        /// 按主键删除
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="builder"></param>
        /// <param name="ids"></param>
        /// <returns></returns>
        public static int removeByIds<T>(this SQLBuilder builder, IEnumerable ids) where T : class, new()
        {
            var b = builder.useSQL();
            var en = builder.Client.EntityCash.getEntityInfo<T>();
            builder.Client.Translator.prepareDelete(b, en,ids);
            return b.doDelete();
        }
        /// <summary>
        /// 批量实体更新，传递事务
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="kit"></param>
        /// <param name="row"></param>
        /// <returns></returns>
        public static EnUpdatable<T> updatable<T>(this SQLBuilder kit, T row) {
            var tool = new EnUpdatable<T>(kit.DBLive);
            if (kit.Executor != null) {
                tool.useTransaction(kit.Executor);
            }
            tool.useEntity(row);
            return tool;
        }

        /// <summary>
        /// 批量实体更新，传递事务
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="kit"></param>
        /// <param name="row"></param>
        /// <returns></returns>
        public static EnUpdatable<T> updatable<T>(this SQLBuilder kit,IEnumerable<T> row)
        {
            var tool = new EnUpdatable<T>(kit.DBLive);
            if (kit.Executor != null)
            {
                tool.useTransaction(kit.Executor);
            }
            tool.useEntitys(row);
            return tool;
        }

        /// <summary>
        /// 批量实体插入，传递事务
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="kit"></param>
        /// <param name="row"></param>
        /// <returns></returns>
        public static EnInsertable<T> insertable<T>(this SQLBuilder kit, T row)
        {
            var tool = new EnInsertable<T>(kit.DBLive);
            if (kit.Executor != null)
            {
                tool.useTransaction(kit.Executor);
            }
            tool.useEntity(row);
            return tool;
        }

        /// <summary>
        /// 批量实体插入，传递事务
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="kit"></param>
        /// <param name="row"></param>
        /// <returns></returns>
        public static EnInsertable<T> insertable<T>(this SQLBuilder kit, IEnumerable<T> row)
        {
            var tool = new EnInsertable<T>(kit.DBLive);
            if (kit.Executor != null)
            {
                tool.useTransaction(kit.Executor);
            }
            tool.useEntitys(row);
            return tool;
        }

        /// <summary>
        /// 批量实体删除，传递事务
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="kit"></param>
        /// <param name="row"></param>
        /// <returns></returns>
        public static EnDeletable<T> deletable<T>(this SQLBuilder kit, T row)
        {
            var tool = new EnDeletable<T>(kit.DBLive);
            if (kit.Executor != null)
            {
                tool.useTransaction(kit.Executor);
            }
            tool.useEntity(row);
            return tool;
        }

        /// <summary>
        /// 批量实体删除，传递事务
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="kit"></param>
        /// <param name="row"></param>
        /// <returns></returns>
        public static EnDeletable<T> deletable<T>(this SQLBuilder kit, IEnumerable<T> row)
        {
            var tool = new EnDeletable<T>(kit.DBLive);
            if (kit.Executor != null)
            {
                tool.useTransaction(kit.Executor);
            }
            tool.useEntitys(row);
            return tool;
        }
        /// <summary>
        /// 查找字段值，按主键查找 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="R"></typeparam>
        /// <param name="srcKIt"></param>
        /// <param name="id"></param>
        /// <param name="fieldselector"></param>
        /// <returns></returns>
        /// <exception cref="NotSupportedException"></exception>
        public static R findFieldValue<T,R>(this SQLBuilder srcKIt, object id, Expression<Func<T, R>> fieldselector)
        {

            var kit = srcKIt.useSQL();
            var en = kit.DBLive.client.EntityCash.getEntityInfo(typeof(T));
            var tbname = kit.DBLive.client.EntityCash.getTableName(typeof(T));
            kit.from(tbname);
            var pks = en.GetPK();
            if (pks.Count != 1)
            {
                throw new NotSupportedException("当前实体的主键信息缺失或存在多个，只有唯一主键时可用本方法！");
            }
            var pk = pks[0];
            var pkname = pk.DbColumnName;
            kit.where(pkname, id);

            //var cont = new FastCompileContext();
            //cont.EntityType = typeof(T);
            //cont.initByBuilder(kit);
            //var fiedv = new FieldVisitor(cont, false);
            //var fid = fiedv.FindField(fieldselector);
            var fid = kit.DBLive.FindFieldName(fieldselector);
            if (string.IsNullOrWhiteSpace(fid))
            {
                throw new NotSupportedException("未找到属性对应的数据库字段！请检查实体和字段信息");
            }

            kit.select(fid);
            var res = kit.queryScalar<R>();
            return res;

        }

        #endregion

        #region 导航扩展
        /// <summary>
        /// 加载子表集合的原始方法，自定义主表主键获取，主表子表集合选择，子表外键选择，子表过滤条件
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="K"></typeparam>
        /// <typeparam name="Child"></typeparam>
        /// <param name="kit"></param>
        /// <param name="list"></param>
        /// <param name="findListPKValue"></param>
        /// <param name="childSelector"></param>
        /// <param name="childFKSelector"></param>
        /// <param name="childFilter"></param>
        /// <exception cref="Exception"></exception>
        public static NavQueryGuide<T, Child> includeHis<T, Child, K>(this SQLBuilder kit, IEnumerable<T> list, Func<T, ICollection<Child>> childSelector, Func<T, K> findListPKValue, Expression<Func<Child, K>> childFKSelector, Action<SQLBuilder> childFilter = null)
        {
            var fk = kit.DBLive.FindFieldName(childFKSelector);
            var childFunc = childFKSelector.Compile();
            return includeHis<T, Child, K>(kit, list, childSelector, findListPKValue, childFunc, fk, childFilter);
        }
        /// <summary>
        /// 核心的加载子表集合方法
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="Child"></typeparam>
        /// <typeparam name="K"></typeparam>
        /// <param name="kit"></param>
        /// <param name="list"></param>
        /// <param name="childSelector"></param>
        /// <param name="findListPKValue"></param>
        /// <param name="childFKSelector"></param>
        /// <param name="childFKName"></param>
        /// <param name="childFilter"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static NavQueryGuide<T, Child> includeHis<T, Child, K>(this SQLBuilder kit, IEnumerable<T> list, Func<T, ICollection<Child>> childSelector, Func<T, K> findListPKValue, Func<Child, K> childFKSelector, string childFKName, Action<SQLBuilder> childFilter)
        {
            var gide = new NavQueryGuide<T, Child>(kit, list);
            return gide.include<K>(childSelector, findListPKValue, childFKSelector, childFKName, childFilter);
        }
        /// <summary>
        /// 按导航特性进行加载子集合
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="Child"></typeparam>
        /// <param name="builder"></param>
        /// <param name="list"></param>
        /// <param name="childSelector"></param>
        /// <param name="childFilter"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static NavQueryGuide<T, Child> includeNav<T, Child>(this SQLBuilder builder, IEnumerable<T> list, Expression<Func<T, ICollection<Child>>> childSelector, Action<SQLBuilder> childFilter = null)
        {
            var gide = new NavQueryGuide<T, Child>(builder, list);
            return gide.includeNav(childSelector, childFilter);
        }
        /// <summary>
        /// 使用保存导航
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="builder"></param>
        /// <param name="list"></param>
        /// <returns></returns>
        public static NavGuideSave<T> useNavSave<T>(this SQLBuilder builder, IEnumerable<T> list) { 
        
            var guide= new NavGuideSave<T>(builder, list);
            return guide;
        }
        public static NavGuideSave<T> useNavSave<T>(this SQLBuilder builder, T row)
        {
            var list= new List<T>() { row};
            var guide = new NavGuideSave<T>(builder, list);
            return guide;
        }
        #endregion


        #region 表创建

        #endregion
    }
}
