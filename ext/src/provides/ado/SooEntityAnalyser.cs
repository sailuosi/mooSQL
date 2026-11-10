using mooSQL.data.model;
using System;
using System.Collections.Concurrent;
using System.Reflection;

namespace mooSQL.data.Mapping
{
    /// <summary>
    /// mooSQL / Soo 特性默认实体解析器（注册名 <see cref="EntityAnalyserNames.SooDefault"/>）。
    /// 支持继承字段、Join、FK、AutoKey、Shard 等 host 能力；可被子类覆盖命名钩子后同名替换。
    /// </summary>
    public class SooEntityAnalyser : BaseAttrEntityAnalyser<SooTableAttribute>
    {
        /// <summary>
        /// 创建默认 Soo 解析器。
        /// </summary>
        public SooEntityAnalyser()
        {
            this.InheritColumn = true;
            this.Name = EntityAnalyserNames.SooDefault;
        }

        /// <inheritdoc />
        public override EntityColumn ParseColumn(Type entity, PropertyInfo propertyInfo, EntityInfo entityInfo, EntityColumn entityColumn)
        {
            var attrs = propertyInfo.GetCustomAttributes<SooColumnAttribute>();
            if (attrs != null)
            {
                if (entityColumn == null)
                {
                    entityColumn = new EntityColumn(entityInfo);
                    entityColumn.PropertyInfo = propertyInfo;
                    entityColumn.PropertyName = propertyInfo.Name;
                    entityColumn.EntityName = entityInfo.EntityName;
                }
                foreach (var attr in attrs)
                {
                    var hasExplicitName = !string.IsNullOrWhiteSpace(attr.Name);
                    entityColumn.DbColumnName = hasExplicitName
                        ? attr.Name.Trim()
                        : ResolveColumnName(propertyInfo.Name);
                    entityColumn.ColumnDescription = attr.Caption;
                    entityColumn.IsIgnore = attr.SkipOnEntityFetch && attr.SkipOnInsert && attr.SkipOnUpdate;
                    if (attr.IsColumn == false)
                    {
                        entityColumn.IsIgnore = true;
                        entityColumn.DbColumnName = string.Empty;
                        entityColumn.Kind = FieldKind.Fake;
                        continue;
                    }

                    entityColumn.Length = attr.Length;
                    entityColumn.IsPrimarykey = attr.IsPrimaryKey;
                    entityColumn.IsNullable = attr.CanBeNull;
                    //主键字段不能为空
                    if (entityColumn.IsPrimarykey)
                    {
                        entityColumn.IsNullable = false;
                    }
                    if (attr.HasIsIdentity())
                        entityColumn.IsIdentity = attr.IsIdentity;
                    else if (attr.IsIdentity == true)
                        entityColumn.IsIdentity = true;

                    // 与 MooEntityAnalyser 对齐：SkipOnInsert/Update；Identity 默认跳过插入/更新
                    if (attr.HasSkipOnInsert())
                        entityColumn.IsOnlyIgnoreInsert = attr.SkipOnInsert;
                    else if (entityColumn.IsIdentity)
                        entityColumn.IsOnlyIgnoreInsert = true;
                    if (attr.HasSkipOnUpdate())
                        entityColumn.IsOnlyIgnoreUpdate = attr.SkipOnUpdate;
                    else if (entityColumn.IsIdentity)
                        entityColumn.IsOnlyIgnoreUpdate = true;

                    // 应用侧发号（string Guid 等）：须写入 EntityColumn，prepareInsert 才能 ResolveAutoKey
                    if (!string.IsNullOrWhiteSpace(attr.AutoKey))
                        entityColumn.AutoKey = attr.AutoKey.Trim();
                    if (attr.KeyGeneratorType != null)
                        entityColumn.KeyGeneratorType = attr.KeyGeneratorType;
                    if (entityColumn.IsIdentity &&
                        (!string.IsNullOrWhiteSpace(entityColumn.AutoKey) || entityColumn.KeyGeneratorType != null))
                    {
                        throw new InvalidOperationException(
                            "Column '" + entityColumn.PropertyName + "' cannot be both IsIdentity and AutoKey/KeyGeneratorType.");
                    }

                    entityColumn.Scale = attr.Scale;
                    entityColumn.Precision = attr.Precision;
                    entityColumn.DataType = attr.DataType;
                    if (attr.DbType != null)
                    {
                        entityColumn.DbType = parseDbDataType(attr.DbType);
                    }

                    if (attr.Kind != FieldKind.None)
                    {
                        entityColumn.Kind = attr.Kind;
                    }
                    if (!string.IsNullOrWhiteSpace(attr.Alias))
                    {
                        entityColumn.Alias = attr.Alias;
                    }
                    if (!string.IsNullOrWhiteSpace(attr.FreeSQL))
                    {
                        entityColumn.FreeSQL = attr.FreeSQL;
                    }
                    if (!string.IsNullOrWhiteSpace(attr.SrcTable))
                    {
                        entityColumn.SrcTable = attr.SrcTable;
                    }
                    if (!string.IsNullOrWhiteSpace(attr.SrcField))
                    {
                        entityColumn.SrcField = attr.SrcField;
                    }
                    if (!string.IsNullOrWhiteSpace(attr.Dict))
                    {
                        entityColumn.Dict = attr.Dict;
                    }

                    //解析字段排序的设置
                    if (attr.Asc == true || attr.Desc == true || attr.OrderIdx > 0)
                    {
                        var od = new EntityOrder();
                        if (entityColumn.Kind == FieldKind.Base)
                        {
                            od.Nick = entityInfo.Alias;
                            od.Field = entityColumn.DbColumnName;
                        }
                        else if (entityColumn.Kind == FieldKind.Join)
                        {
                            od.Nick = entityColumn.SrcTable;
                            od.Field = entityColumn.SrcField;
                        }

                        if (attr.Asc == true)
                        {
                            od.OType = OrderType.ASC;
                        }
                        else if (attr.Desc == true)
                        {
                            od.OType = OrderType.DESC;
                        }
                        else if (attr.OrderIdx > 0)
                        {
                            od.Idx = attr.OrderIdx;
                            if (od.OType == OrderType.None)
                            {
                                od.OType = OrderType.ASC;
                            }
                        }

                        entityInfo.AddOrderBy(od);
                    }

                    if (attr.Shard && entityColumn != null)
                        ShardRegistration.MarkShardField(entityInfo, entityColumn);
                }
                //设置默认类型，如果设置了字段名称，未设置类型，则为实表字段
                if (!string.IsNullOrWhiteSpace(entityColumn.DbColumnName) && entityColumn.Kind == FieldKind.None)
                {
                    entityColumn.Kind = FieldKind.Base;
                }
            }

            var fks = propertyInfo.GetCustomAttributes<SooFKAttribute>();
            if (fks != null)
            {
                foreach (var fk in fks)
                {
                    entityColumn.IsFK = true;
                    entityColumn.thatTable = fk.thatTable;
                    entityColumn.thatField = fk.thatField;
                }
            }

            return entityColumn;
        }

        private DbDataType parseDbDataType(string dbtype)
        {
#if NET6_0_OR_GREATER
            object res;
            if (Enum.TryParse(typeof(DbDataType), dbtype, true, out res))
            {
                return (DbDataType)res;
            }
#else
            DbDataType res;
            if (Enum.TryParse<DbDataType>(dbtype, out res))
            {
                return res;
            }
#endif

            return DbDataType.Undefined;
        }

        /// <inheritdoc />
        protected override EntityInfo AfterReadEntityAttr(Type Entity, EntityInfo result)
        {
            //读取join的配置、where的配置、orderby的配置
            var joinAtts = Entity.GetCustomAttributes<SooJoinAttribute>();
            if (joinAtts != null)
            {
                if (result.Joins == null)
                {
                    result.Joins = new ConcurrentDictionary<string, EntityJoin>();
                }
                int i = result.Joins.Count;
                foreach (var joinAttribute in joinAtts)
                {
                    var join = new EntityJoin();
                    join.Type = joinAttribute.Type;
                    join.To = joinAttribute.To;
                    join.As = joinAttribute.As;
                    join.On = joinAttribute.On;
                    join.OnA = joinAttribute.OnA;
                    join.OnB = joinAttribute.OnB;
                    i++;
                    join.Idx = i;

                    result.Joins.TryAdd(join.UniqueKey, join);
                }
            }

            return result;
        }

        /// <inheritdoc />
        protected override EntityInfo ReadEntityAttr(SooTableAttribute attr, Type entity, EntityInfo info)
        {
            if (attr == null) return info;

            if (info == null)
            {
                info = new EntityInfo();
                info.Type = entity;
                info.EntityName = entity.Name;
            }
            var hasExplicitTable = !string.IsNullOrWhiteSpace(attr.Name);
            if (hasExplicitTable)
            {
                info.DbTableName = attr.Name.Trim();
            }
            else
            {
                info.DbTableName = ResolveTableName(entity.Name);
            }

            if (attr.Schema != null)
            {
                info.SchemaName = attr.Schema;
            }
            if (attr.Database != null)
            {
                info.DatabaseName = attr.Database;
            }
            if (attr.Server != null)
            {
                info.ServerName = attr.Server;
            }

            if (attr.Caption != null)
            {
                info.TableDescription = attr.Caption;
            }
            if (!string.IsNullOrWhiteSpace(attr.Alias))
            {
                info.Alias = attr.Alias;
            }
            info.DType = attr.Type;
            if (!string.IsNullOrWhiteSpace(info.DbTableName) && info.DType == DBTableType.None)
            {
                info.DType = DBTableType.Table;
            }
            if (!string.IsNullOrWhiteSpace(attr.ViewSQL))
                info.ViewSQL = attr.ViewSQL;
            if (info.DType == DBTableType.View || !string.IsNullOrWhiteSpace(info.ViewSQL))
                info.IsView = true;
            info.LiveName = attr.LiveName;
            ShardRegistration.ApplyTableAttribute(info, attr);
            return info;
        }

        /// <inheritdoc />
        public override EntityInfo ParseEntity(Type entity, EntityInfo info)
        {
            info = base.ParseEntity(entity, info);
            ShardRegistration.FinalizeEntityShard(info);
            return info;
        }
    }

    /// <summary>
    /// 兼容旧名；请改用 <see cref="SooEntityAnalyser"/>。
    /// </summary>
    [Obsolete("请使用 SooEntityAnalyser")]
    internal class InternalEntityParser : SooEntityAnalyser
    {
    }
}
