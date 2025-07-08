using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data.Mapping
{
    /// <summary>
    /// 依据特性进行实体类解析的解析器
    /// </summary>
    public abstract class BaseAttrEntityAnalyser<T> : IEntityAnalyser where T : Attribute
    {
        public virtual bool FailBacked => false;
        /// <summary>
        /// 是否同时读取父类的字段
        /// </summary>
        public virtual bool InheritColumn { get; protected set; } =false;
        /// <summary>
        /// 是否可解析，默认必须直接定义了特性，才能解析
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public virtual bool CanParse(Type type)
        {
            if (type.IsDefined(typeof(T), false)) {
                return true;
            }
            return false;
        }
        /// <summary>
        /// 解析字段
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="propertyInfo"></param>
        /// <param name="entityInfo"></param>
        /// <param name="entityColumn"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public abstract EntityColumn ParseColumn(Type entity, PropertyInfo propertyInfo, EntityInfo entityInfo, EntityColumn entityColumn);

        /// <summary>
        /// 解析实体
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="info"></param>
        /// <returns></returns>
        public virtual EntityInfo ParseEntity(Type entity, EntityInfo info)
        {
            info = this.parseAllEntity(entity,info);
            return info;
        }



        /// <summary>
        /// 读取特性的值
        /// </summary>
        /// <param name="attr"></param>
        /// <param name="entity"></param>
        /// <param name="info"></param>
        /// <returns></returns>
        protected abstract EntityInfo ReadEntityAttr(T attr, Type entity, EntityInfo info);

        private EntityInfo parseAllEntity(Type Entity, EntityInfo result)
        {
            if (result == null) { 
                result = new EntityInfo();
                result.Type = Entity;          
            }

            //增加对第三方特性的支持，如sqlsugar/freesql/efcore等
            var attributes = Entity.GetCustomAttributes(typeof(T), true);
            foreach (T attr in attributes)
            {
                result = ReadEntityAttr(attr, Entity, result);
            }
            AfterReadEntityAttr(Entity, result);
            SetColumns(Entity,result);

            //结束前，处理一下排序字段的顺序
            if (result.OrderBy != null) { 
                result.OrderBy = result.OrderBy.OrderBy(x => x.Idx).ToList();
            }
            //为了兼容以前的写法，如果没有指定表类型，但是指定了表名，则默认为Table
            if (!string.IsNullOrWhiteSpace(result.DbTableName) && result.DType == DBTableType.None) { 
                result.DType = DBTableType.Table;
            }
            return result;
        }
        /// <summary>
        /// 读取特性后执行的方法，可以在这里做一些额外的处理工作
        /// </summary>
        /// <param name="Entity"></param>
        /// <param name="result"></param>
        protected virtual EntityInfo AfterReadEntityAttr(Type Entity, EntityInfo result) { 
            return result;
        }

        private void SetColumns(Type entity, EntityInfo result)
        {
            foreach (var property in entity.GetProperties())
            {
                var col=result.GetColumn(property.Name);
                EntityColumn column = this.ParseColumn(entity,property,result, col);
                //_onParseEntityColumn(property, result);
                result.AddColumnInfo(column);
            }
            if (this.InheritColumn) {
                if (entity.BaseType != null) {
                    SetColumns(entity.BaseType, result);
                }
            }
        }


    }
}
