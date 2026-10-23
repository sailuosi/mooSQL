using mooSQL.data.model;
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


        /// <summary>
        /// ReadEntityAttr 方法（返回 EntityInfo）。
        /// </summary>
        protected override EntityInfo ReadEntityAttr(SooTableAttribute attr, Type entity, EntityInfo info)
        {
            if (attr == null) { 
                return info;
            }

            info.DbTableName = attr.Name;
            info.SchemaName = attr.Schema;
            info.DatabaseName = attr.Database;
            info.ServerName = attr.Server;
            info.LiveName = attr.LiveName;
            ShardRegistration.ApplyTableAttribute(info, attr);

            return info;
        }

        /// <summary>
        /// 解析Entity。
        /// </summary>
        public override EntityInfo ParseEntity(Type entity, EntityInfo info)
        {
            info = base.ParseEntity(entity, info);
            ShardRegistration.FinalizeEntityShard(info);
            return info;
        }

        /// <summary>
        /// 解析Column。
        /// </summary>
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
                entityColumn.PropertyInfo = propertyInfo;
                if (ca.HasLength()) { 
                    entityColumn.Length = ca.Length;
                }
                if (ca.HasPrecision()) entityColumn.Precision = ca.Precision;
                if (ca.HasScale()) entityColumn.Scale = ca.Scale;
                if (ca.HasIsPrimaryKey())
                    entityColumn.IsPrimarykey = ca.IsPrimaryKey;
                if (ca.HasCanBeNull())
                    entityColumn.IsNullable = ca.CanBeNull;
                if (ca.DataType != DataFam.Undefined)
                    entityColumn.DataType = ca.DataType;
                if (!string.IsNullOrWhiteSpace(ca.Caption))
                    entityColumn.ColumnDescription = ca.Caption;
                if (ca.Kind != FieldKind.None)
                    entityColumn.Kind = ca.Kind;
                else if (entityColumn.Kind == FieldKind.None)
                    entityColumn.Kind = FieldKind.Base;
                if (ca.HasIsIdentity())
                    entityColumn.IsIdentity = ca.IsIdentity;
                if (ca.HasSkipOnInsert())
                    entityColumn.IsOnlyIgnoreInsert = ca.SkipOnInsert;
                else if (entityColumn.IsIdentity)
                    entityColumn.IsOnlyIgnoreInsert = true;
                if (ca.HasSkipOnUpdate())
                    entityColumn.IsOnlyIgnoreUpdate = ca.SkipOnUpdate;
                else if (entityColumn.IsIdentity)
                    entityColumn.IsOnlyIgnoreUpdate = true;
                if (!string.IsNullOrWhiteSpace(ca.AutoKey))
                    entityColumn.AutoKey = ca.AutoKey.Trim();
                if (ca.KeyGeneratorType != null)
                    entityColumn.KeyGeneratorType = ca.KeyGeneratorType;
                if (entityColumn.IsIdentity &&
                    (!string.IsNullOrWhiteSpace(entityColumn.AutoKey) || entityColumn.KeyGeneratorType != null))
                {
                    throw new InvalidOperationException(
                        "Column '" + entityColumn.PropertyName + "' cannot be both IsIdentity and AutoKey/KeyGeneratorType.");
                }
                if (!ca.IsColumn)
                    entityColumn.IsIgnore = true;
                if (ca.Shard && entityColumn != null)
                    ShardRegistration.MarkShardField(entityInfo, entityColumn);
            }

            return entityColumn;
        }
    }
}