using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using mooSQL.data;
using mooSQL.data.model;
using mooSQL.linq.Linq;

namespace mooSQL.linq.translator;

/// <summary>
/// 导航属性二次查询加载（从 FastLinq LoadNavChilds 移植）。
/// </summary>
internal static class NavColumnLoader
{
    public static void LoadNavChilds<T>(SentenceBag bag, IEnumerable<T> items)
    {
        if (bag.NavColumns == null || !bag.NavColumns.ContainsKey(typeof(T)))
            return;

        var list = items as IList<T> ?? items.ToList();
        LoadNavChilds(bag, list, typeof(T), new HashSet<Type>());
    }

    static void LoadNavChilds<T>(SentenceBag bag, IList<T> items, Type entityType, HashSet<Type> visited)
    {
        if (!visited.Add(entityType))
            return;

        if (!bag.NavColumns.TryGetValue(entityType, out var columns))
            return;

        foreach (var col in columns)
            LoadNavChild(bag, items, col, visited);
    }

    static void LoadNavChild<T>(SentenceBag bag, IList<T> items, EntityColumn col, HashSet<Type> visited)
    {
        var childType = col.Navigat?.ChildType;
        if (childType == null || col.PropertyInfo == null)
            return;

        EntityColumn? pkCol = null;
        if (col.Navigat?.BossKey != null)
            pkCol = col.belongTable?.GetColumn(col.Navigat.BossKey);

        if (pkCol == null)
        {
            var pk = col.belongTable?.GetPK();
            if (pk == null || pk.Count != 1)
                return;
            pkCol = pk[0];
        }

        if (pkCol?.PropertyInfo == null)
            return;

        var pks = LoadEntityFieldValues(items, pkCol);
        if (pks.Count == 0)
            return;

        var slaveKey = col.Navigat!.SlaveKey;
        if (string.IsNullOrEmpty(slaveKey))
            return;

        var method = typeof(NavColumnLoader).GetMethod(nameof(LoadNavChildGeneric), BindingFlags.NonPublic | BindingFlags.Static)!;
        method.MakeGenericMethod(childType).Invoke(null, new object[] { bag, items, col, pkCol, pks, slaveKey, visited });
    }

    static void LoadNavChildGeneric<TChild>(
        SentenceBag bag,
        IList items,
        EntityColumn col,
        EntityColumn pkCol,
        List<object> pks,
        string slaveKey,
        HashSet<Type> visited)
    {
        var db = bag.DBLive;
        var tableName = db.client.EntityCash.getTableName(typeof(TChild));
        var childRows = db.useSQL().from(tableName).whereIn(slaveKey, pks).query<TChild>();

        var fkCol = db.client.EntityCash.getField(typeof(TChild), slaveKey);
        if (fkCol?.PropertyInfo == null || col.PropertyInfo == null)
            return;

        foreach (var row in items)
        {
            if (row == null) continue;
            var pkVal = pkCol.PropertyInfo!.GetValue(row);
            if (pkVal == null) continue;

            var navVal = FilterByForeignKey(childRows, fkCol, pkVal, col.PropertyInfo);
            if (navVal != null)
                col.PropertyInfo.SetValue(row, navVal);
        }

        if (bag.NavColumns.ContainsKey(typeof(TChild)))
        {
            var childList = childRows as IList<TChild> ?? childRows.ToList();
            LoadNavChilds(bag, childList, typeof(TChild), visited);
        }
    }

    static object? FilterByForeignKey<TChild>(IEnumerable<TChild> values, EntityColumn fkCol, object fkVal, PropertyInfo navProperty)
    {
        var fkProp = fkCol.PropertyInfo;
        if (fkProp == null)
            return null;

        var navType = navProperty.PropertyType;
        var isNavCollection = navType != typeof(string)
            && typeof(IEnumerable).IsAssignableFrom(navType)
            && navType.IsGenericType;

        if (isNavCollection)
        {
            var elementType = navType.GetGenericArguments()[0];
            var listType = typeof(List<>).MakeGenericType(elementType);
            var list = (IList)Activator.CreateInstance(listType)!;
            foreach (var item in values)
            {
                var v = fkProp.GetValue(item);
                if (v != null && v.Equals(fkVal))
                    list.Add(item);
            }
            return list.Count > 0 ? list : null;
        }

        foreach (var item in values)
        {
            var v = fkProp.GetValue(item);
            if (v != null && v.Equals(fkVal))
                return item;
        }

        return null;
    }

    static List<object> LoadEntityFieldValues<T>(IEnumerable<T> items, EntityColumn pkCol)
    {
        var pks = new List<object>();
        var prop = pkCol.PropertyInfo;
        if (prop == null)
            return pks;

        foreach (var item in items)
        {
            if (item == null) continue;
            var val = prop.GetValue(item);
            if (val != null)
                pks.Add(val);
        }

        return pks.Distinct().ToList();
    }
}
