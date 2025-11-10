using mooSQL.data.Mapping;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data
{
    /// <summary>
    /// 实体解析环境
    /// </summary>
    public class EntityContext
    {
        private IEntityAnalyseFactory analyseFactory;
        /// <summary>
        /// 实体解析环境
        /// </summary>
        /// <param name="analyseFactory"></param>
        public EntityContext(IEntityAnalyseFactory analyseFactory) { 
            this.analyseFactory = analyseFactory;
            this.typeMap = new ConcurrentDictionary<Type, EntityDictionary> ();
        }
        private ConcurrentDictionary<Type, EntityDictionary> typeMap;

        private void checkType(Type entity) {
            if (entity == null) { 
                return;
            }
            if (!typeMap.ContainsKey(entity))
            {
                var tar = new EntityDictionary(entity);
                tar.init(analyseFactory);
                typeMap.TryAdd(entity, tar);
            }
        }
        /// <summary>
        /// 获取字段明
        /// </summary>
        /// <param name="memberInfo"></param>
        /// <returns></returns>
        public string getFieldName(MemberInfo memberInfo, Type motherType = null) {
            if (motherType == null) {
                motherType = memberInfo.DeclaringType;
            }
            
            checkType(motherType);

            var name =memberInfo.Name;
            var dic = typeMap[motherType];
            if (dic.Fields.ContainsKey(name)) { 
                var col= dic.Fields[name];
                if (!string.IsNullOrWhiteSpace(col.DbColumnName)) { 
                    return col.DbColumnName;
                }
            }
            return name;
        }
        public EntityColumn getFieldCol(MemberInfo memberInfo, Type motherType = null)
        {
            if (motherType == null)
            {
                motherType = memberInfo.DeclaringType;
            }

            checkType(motherType);
            if (typeMap.ContainsKey(motherType) == false) { 
                return null;
            }
            var name = memberInfo.Name;
            var dic = typeMap[motherType];
            if (dic.Fields.ContainsKey(name))
            {
                var col = dic.Fields[name];

                 return col;
                
            }
            return null;
        }
        /// <summary>
        /// 获取字段明
        /// </summary>
        /// <param name="memberInfo"></param>
        /// <returns></returns>
        public string getFieldName(Type entityType,string propertyName)
        {

            checkType(entityType);

            var dic = typeMap[entityType];
            if (dic.Fields.ContainsKey(propertyName))
            {
                var col = dic.Fields[propertyName];
                if (!string.IsNullOrWhiteSpace(col.DbColumnName))
                {
                    return col.DbColumnName;
                }
            }
            return propertyName;
        }

        /// <summary>
        /// 获取字段明
        /// </summary>
        /// <param name="memberInfo"></param>
        /// <returns></returns>
        public EntityColumn getField(Type entityType, string propertyName)
        {

            checkType(entityType);

            var dic = typeMap[entityType];
            if (dic.Fields.ContainsKey(propertyName))
            {
                var col = dic.Fields[propertyName];
                if (!string.IsNullOrWhiteSpace(col.DbColumnName))
                {
                    return col;
                }
            }
            return null;
        }
        /// <summary>
        /// 获取数据库表名
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public string getTableName(Type type) {
            checkType(type);
            var dic = typeMap[type];
            return dic.EntityInfo.DbTableName;
        }

        public EntityInfo getEntityInfo(Type type)
        {
            checkType(type);
            var dic = typeMap[type];
            return dic.EntityInfo;
        }

        public EntityInfo getEntityInfo<T>()
        {
            var t= typeof(T);
            return getEntityInfo(t);
        }

        /// <summary>
        /// 注册表名解析器。
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="interceptor"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public EntityContext useParser<T>(ITableNameInterceptor interceptor, string name = null) {
            var en = getEntityInfo<T>();
            if (string.IsNullOrWhiteSpace(name)) {
                name = en.EntityName;
            }
            en.UseNameParser(name, interceptor);
            return this;
        }
        /// <summary>
        /// 注册表名解析器。用表达式的形式
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="name"></param>
        /// <param name="nameParser"></param>
        /// <returns></returns>
        public EntityContext useParser<T>(string name,Func<T,string> nameParser)
        {
            var en = getEntityInfo<T>();
            if (string.IsNullOrWhiteSpace(name))
            {
                name = en.EntityName;
            }
            var interceptor = new FuncTableNameCepter(nameParser);
            en.UseNameParser(name, interceptor);
            return this;
        }
        /// <summary>
        /// 注册表名解析器。用表达式的形式
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="nameParser"></param>
        /// <returns></returns>
        public EntityContext useParser<T>(Func<T, string> nameParser)
        {
            useParser<T>(null, nameParser);
            return this;
        }
    }
}
