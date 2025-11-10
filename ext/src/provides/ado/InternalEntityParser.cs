using mooSQL.data.model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data.Mapping
{
    internal class InternalEntityParser : BaseAttrEntityAnalyser<SooTableAttribute>
    {
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
                foreach (var attr in attrs) { 
                    entityColumn.DbColumnName = (attr.Name==null?propertyInfo.Name:attr.Name);
                    entityColumn.ColumnDescription = attr.Caption;
                    entityColumn.IsIgnore = attr.SkipOnEntityFetch&& attr.SkipOnInsert && attr.SkipOnUpdate;
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
                    
                    entityColumn.Scale = attr.Scale;
                    entityColumn.Precision = attr.Precision;
                    entityColumn.DataType = attr.DataType;
                    if (attr.DbType != null) {
                        entityColumn.DbType = parseDbDataType(attr.DbType);
                    }
                    

                    if (attr.Kind != FieldKind.None)
                    {
                        entityColumn.Kind = attr.Kind;
                    }
                    if (!string.IsNullOrWhiteSpace(attr.Alias)) { 
                        entityColumn.Alias = attr.Alias;
                    }
                    if (!string.IsNullOrWhiteSpace(attr.FreeSQL)) {
                        entityColumn.FreeSQL = attr.FreeSQL;
                    }
                    if (!string.IsNullOrWhiteSpace(attr.SrcTable)) {
                        entityColumn.SrcTable = attr.SrcTable;
                    }
                    if (!string.IsNullOrWhiteSpace(attr.SrcField)) {
                        entityColumn.SrcField = attr.SrcField;
                    }
                    if (!string.IsNullOrWhiteSpace(attr.Dict)) {
                        entityColumn.Dict = attr.Dict;
                    }

                    //解析字段排序的设置
                    if (attr.Asc == true || attr.Desc == true || attr.OrderIdx > 0) {
                        var od= new EntityOrder();
                        if (entityColumn.Kind == FieldKind.Base)
                        {
                            od.Nick = entityInfo.Alias;
                            od.Field = entityColumn.DbColumnName;
                        }
                        else if (entityColumn.Kind == FieldKind.Join) {
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
                        if (entityInfo.OrderBy == null) {
                            entityInfo.OrderBy= new List<EntityOrder>();
                        }
                        entityInfo.OrderBy.Add(od);
                    }
                }
                //设置默认类型，如果设置了字段名称，未设置类型，则为实表字段
                if(!string.IsNullOrWhiteSpace(entityColumn.DbColumnName) && entityColumn.Kind == FieldKind.None){
                    entityColumn.Kind =  FieldKind.Base;
                }

            }
            return entityColumn;
        }

        private DbDataType parseDbDataType(string dbtype) {

#if NET6_0_OR_GREATER
            object res;
            if (Enum.TryParse(typeof(DbDataType), dbtype, true, out res)) {
                return (DbDataType)res;
            }
#else
            DbDataType res;
            if (Enum.TryParse<DbDataType>( dbtype,  out res))
            {
                return res;
            }
#endif

            return DbDataType.Undefined;
        }

        protected override EntityInfo AfterReadEntityAttr(Type Entity, EntityInfo result)
        {
            //读取join的配置、where的配置、orderby的配置
            var joinAtts = Entity.GetCustomAttributes<SooJoinAttribute>();
            if (joinAtts != null) {
                if (result.Joins == null) {
                    result.Joins = new List<EntityJoin>();
                }
                foreach (var joinAttribute in joinAtts) { 
                    var join = new EntityJoin();
                    join.Type = joinAttribute.Type;
                    join.To = joinAttribute.To;
                    join.As = joinAttribute.As;
                    join.On = joinAttribute.On;
                    join.OnA = joinAttribute.OnA;
                    join.OnB = joinAttribute.OnB;

                    result.Joins.Add(join);
                }
            }



            return result;
        }

        protected override EntityInfo ReadEntityAttr(SooTableAttribute attr, Type entity, EntityInfo info)
        {
            if (attr == null) return info;

            if (info == null)
            {
                info = new mooSQL.data.EntityInfo();
                info.Type = entity;
            }
            var name= attr.Name;
            if (string.IsNullOrWhiteSpace(name)) { 
                name = entity.Name;
            }
            if (!string.IsNullOrWhiteSpace(name)) {
                info.DbTableName = name;
            }
            
            if (attr.Schema != null) {
                info.SchemaName = attr.Schema;
            }
            if (attr.Database != null) {
                info.DatabaseName = attr.Database;
            }
            if (attr.Server != null) {
                info.ServerName = attr.Server;
            }

            if (attr.Caption != null) {
                info.TableDescription = attr.Caption;
            }
            if (!string.IsNullOrWhiteSpace(attr.Alias)) { 
                info.Alias = attr.Alias;
            }
            info.DType = attr.Type;
            if(!string.IsNullOrWhiteSpace(info.DbTableName) && info.DType == DBTableType.None)
            {
                info.DType = DBTableType.Table;
            }
            return info;
        }
    }
}
