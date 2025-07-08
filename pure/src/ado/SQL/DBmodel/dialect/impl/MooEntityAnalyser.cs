
using System;

using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data.Mapping
{
    /// <summary>
    /// mooSQL自带的实体类注解
    /// </summary>
    public class MooEntityAnalyser : BaseAttrEntityAnalyser<SooTableAttribute>
    {


        protected override EntityInfo ReadEntityAttr(SooTableAttribute attr, Type entity, EntityInfo info)
        {
            if (attr == null) { 
                return info;
            }

            info.DbTableName = attr.Name;
            info.SchemaName = attr.Schema;
            info.DatabaseName = attr.Database;
            info.ServerName = attr.Server;

            //info.TableDescription= attr.
            return info;
        }

        public override EntityColumn ParseColumn(Type entity, PropertyInfo propertyInfo, EntityInfo entityInfo, EntityColumn entityColumn)
        {


            var columnAttributes = propertyInfo.GetCustomAttributes(typeof(SooColumnAttribute));
            if (entityColumn == null && columnAttributes.Count()>0) { 
                entityColumn = new EntityColumn(entityInfo);
            }

            foreach (SooColumnAttribute ca in columnAttributes)
            {
                entityColumn.DbColumnName= ca.Name??propertyInfo.Name;
                entityColumn.PropertyName = propertyInfo.Name;
                if (ca.HasLength()) { 
                    entityColumn.Length = ca.Length;
                }
                if (ca.HasPrecision()) entityColumn.Precision = ca.Precision;
                if (ca.HasScale()) entityColumn.Scale = ca.Scale;
            }

            return entityColumn;
        }
    }
}
