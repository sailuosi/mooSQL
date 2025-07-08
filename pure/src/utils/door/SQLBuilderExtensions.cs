using mooSQL.data.utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;


namespace mooSQL.data
{
    /// <summary>
    /// 内建的SQLBuilder扩展
    /// </summary>
    public static class MooSQLBuilderExtensions
    {

        private static EntityTranslator _Translator;

        private static EntityTranslator Translator { 
            get { 
                if (_Translator == null) _Translator = new EntityTranslator();
                return _Translator;
            }
        
        }

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
        /// 默认返回一个新的SQLClip，如果inherit为true则继承当前的上下文。
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="inherit"></param>
        /// <returns></returns>
        public static SQLClip useClip(this SQLBuilder builder,bool inherit=false)
        {
            if (inherit) { 
                return builder.MooClient.ClientFactory.useClip(builder.DBLive, builder);
            }
            var tar= builder.MooClient.ClientFactory.useClip(builder.DBLive,null);
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
            var res= Translator.prepareInsert(builder, entity,typeof(T));
            if (res.Status) {
                return builder.doInsert();
            }
            builder.MooClient.Loggor.LogError(res.Message);
            return -1;
        }
        public static int insertByType(this SQLBuilder builder, object entity,Type EntityType)
        {
            var res = Translator.prepareInsert(builder, entity, EntityType);
            if (res.Status)
            {
                return builder.doInsert();
            }
            builder.MooClient.Loggor.LogError(res.Message);
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
            var res= Translator.prepareInsert(builder, entity,typeof(T));
            if (res.Status)
                return builder.toInsert();
            builder.MooClient.Loggor.LogError(res.Message);
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
            var res= Translator.prepareUpdate(builder, entity,typeof(T));
            if (res.Status) {
                return builder.doUpdate();
            }
            builder.MooClient.Loggor.LogError(res.Message);
            return -1;
        }
        public static int updateByType(this SQLBuilder builder, object entity,Type EntityType)
        {
            var res = Translator.prepareUpdate(builder, entity,EntityType);
            if (res.Status)
            {
                return builder.doUpdate();
            }
            builder.MooClient.Loggor.LogError(res.Message);
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
            var res= Translator. prepareUpdate(builder, entity,typeof(T));
            if (res.Status)
            {
                return builder.toUpdate();
            }
            builder.MooClient.Loggor.LogError(res.Message);
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
            return Translator.updateByFieild(builder, entity, Name);
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

            var name = loadMemberName(updateKey.Body);

            return Translator.updateByFieild(builder, entity, name);
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
            var cash = builder.DBLive.client.EntityCash;

            var field= cash.getField(typeof(T),Name);
            var val= field.PropertyInfo.GetValue(entity);
            if (val == null) { 
                throw new Exception("实体的属性对应数据库字段信息未找到！");
            }
            var has= builder.checkExistKey(field.DbColumnName,val,field.DbTableName);
            if (has)
            {
                return Translator.updateByFieild(builder, entity, Name);
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

            var en = builder.MooClient.EntityCash.getEntityInfo<T>();

            if (en.Insertable == false && en.Updatable == false) {
                return 0;
            }
            if (en.Insertable == false)
            {
                return update(builder, entity);
            }
            else if (en.Updatable == false) { 
                //禁止更新，则直接插入
                return insert(builder, entity);
            }
            //检查是否存在
            var ck = builder.DBLive.useSQL();
            ck.from(en.DbTableName);
            Translator.setPKWhere(ck, entity, en);
            var cc = ck.count();
            if (cc > 0)
            {
                return update(builder, entity);
            }
            else {
                return insert(builder, entity);
            }
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
            Translator.prepareDelete<T>(builder, entity);
            return builder.doDelete();
        }
        public static SQLCmd toDelete<T>(this SQLBuilder builder, T entity)
        {
            Translator.prepareDelete<T>(builder, entity);
            return builder.toDelete();
        }
        public static SQLCmd toDeleteByType(this SQLBuilder builder, object entity,Type type)
        {
            Translator.prepareDelete(builder, entity,type);
            return builder.toDelete();
        }
        public static int deleteByType(this SQLBuilder builder, object entity, Type type)
        {
            Translator.prepareDelete(builder, entity, type);
            return builder.doDelete();
        }
        public static List<SQLCmd> toDelete<T>(this SQLBuilder builder, IEnumerable<T> entitys)
        {
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

            //联合主键的情况
            var bsql = builder.MooClient.ClientFactory.useBatchSQL(builder.DBLive);
            var sql=toDelete(builder, entitys);
            bsql.addSQLs(sql);
            return bsql.exeNonQuery();

        }
    }
}
