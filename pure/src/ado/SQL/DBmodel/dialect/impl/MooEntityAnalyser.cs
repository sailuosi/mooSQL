using mooSQL.data.model;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

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
                // Name 为 null/空白时回退属性名（"" ?? PropName 仍是 ""，会导致 INSERT 空列名）
                var hasExplicitName = !string.IsNullOrWhiteSpace(ca.Name);
                entityColumn.DbColumnName = hasExplicitName ? ca.Name.Trim() : propertyInfo.Name;
                if (string.IsNullOrWhiteSpace(entityColumn.DbColumnName))
                    entityColumn.IsIgnore = true;

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

                // 字典/集合非标量：未显式指定列名时默认忽略，避免误进 INSERT（空列 + Dictionary.ToString）
                // 显式 Name（如 JSON 列）仍可映射
                if (!hasExplicitName && IsNonScalarCollection(propertyInfo.PropertyType))
                    entityColumn.IsIgnore = true;

                if (ca.Shard && entityColumn != null)
                    ShardRegistration.MarkShardField(entityInfo, entityColumn);
            }

            return entityColumn;
        }

        /// <summary>
        /// 非标量集合/字典（string、byte[] 除外）。无显式列名时不应当普通列插入。
        /// </summary>
        public static bool IsNonScalarCollection(Type type)
        {
            if (type == null)
                return false;
            type = Nullable.GetUnderlyingType(type) ?? type;
            if (type == typeof(string) || type == typeof(byte[]))
                return false;
            if (typeof(IDictionary).IsAssignableFrom(type))
                return true;
            if (type.IsGenericType)
            {
                var def = type.GetGenericTypeDefinition();
                if (def == typeof(IDictionary<,>)
                    || def == typeof(Dictionary<,>)
                    || def == typeof(IReadOnlyDictionary<,>))
                    return true;
            }
            return typeof(IEnumerable).IsAssignableFrom(type);
        }
    }
}