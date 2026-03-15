// 基础功能说明：

using mooSQL.data;
using mooSQL.data.Mapping;
using SqlSugar;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace HHNY.NET.Core.MooSQL;
public class SugarEnitiyParser : BaseAttrEntityAnalyser<SugarTable>
{
    public override EntityColumn ParseColumn(Type entity, PropertyInfo propertyInfo, mooSQL.data.EntityInfo entityInfo, EntityColumn entityColumn)
    {
        var attr= propertyInfo.GetCustomAttribute<SugarColumn>();
        if (attr != null) {
            if (attr.IsIgnore) { 
                return null;
            }
            if (entityColumn == null) {
                entityColumn= new EntityColumn(entityInfo);
                entityColumn.PropertyInfo = propertyInfo;
                entityColumn.PropertyName = propertyInfo.Name;
                entityColumn.EntityName = entityInfo.EntityName;
            }
            //当未配置时，使用属性名作为字段名
            var name = attr.ColumnName;
            if (string.IsNullOrWhiteSpace(name)) { 
                name=propertyInfo.Name;
            }


            entityColumn.DbColumnName = name;
            entityColumn.ColumnDescription= attr.ColumnDescription;
            entityColumn.IsIgnore = attr.IsIgnore;
            entityColumn.Length = attr.Length;
            entityColumn.IsPrimarykey=attr.IsPrimaryKey;
            entityColumn.IsNullable=attr.IsNullable;
            
        }
        //检查导航属性
        var nav = propertyInfo.GetCustomAttribute<Navigate>();
        if (nav != null) {
            if (entityColumn == null)
            {
                entityColumn = new EntityColumn(entityInfo);
                entityColumn.PropertyInfo = propertyInfo;
                entityColumn.PropertyName = propertyInfo.Name;
                entityColumn.EntityName = entityInfo.EntityName;
            }
            //导航属性必然不是主表的字段
            entityColumn.IsIgnore = true;
            var navInfo = new EntityNavi();
            //navInfo.BossKey=nav.MappingAId;
            //navInfo.BossKey2 = nav.MappingBId;
            //navInfo.SlaveKey= nav.Name;
            //navInfo.SlaveKey2 = nav.Name2;
            //navInfo.MappingType=nav.MappingType;
            //判断默认值，未设置主表ID时，使用主键

            //检查子类的类型
            if (propertyInfo.PropertyType.IsGenericType && propertyInfo.PropertyType.GenericTypeArguments.Length>0) {
                var childType = propertyInfo.PropertyType.GenericTypeArguments[0];
                navInfo.ChildType = childType;
            }

            entityColumn.Navigat=navInfo;
        }
        return entityColumn;
    }

    protected override mooSQL.data.EntityInfo ReadEntityAttr(SugarTable attr, Type entity, mooSQL.data.EntityInfo info)
    {
        if (info == null) { 
            info= new mooSQL.data.EntityInfo();
            info.Type = entity;
        }
        info.DbTableName = attr.TableName;
        info.TableDescription = attr.TableDescription;
        return info;
    }
}
