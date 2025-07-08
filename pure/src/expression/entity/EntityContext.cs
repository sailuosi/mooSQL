using mooSQL.data.Mapping;
using System;
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
            this.typeMap = new Dictionary<Type, EntityDictionary> ();
        }
        private Dictionary<Type, EntityDictionary> typeMap;

        private void checkType(Type entity) {
            if (entity == null) { 
                return;
            }
            if (!typeMap.ContainsKey(entity))
            {
                var tar = new EntityDictionary(entity);
                tar.init(analyseFactory);
                typeMap.Add(entity, tar);
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
    }
}
