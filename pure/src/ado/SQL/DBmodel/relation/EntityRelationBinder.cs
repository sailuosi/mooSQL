using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace mooSQL.data
{
    /// <summary>
    /// 将 <see cref="EntityRelationRegistry"/> 中的类型对关系写到导航属性的 <see cref="EntityNavi"/>。
    /// </summary>
    public static class EntityRelationBinder
    {
        /// <summary>
        /// 对已分析的实体应用所有相关关系（自动发现导航属性）。
        /// </summary>
        public static void ApplyPending(EntityContext ctx, EntityInfo entityInfo)
        {
            if (ctx?.Relations == null || entityInfo?.Type == null) return;
            var type = entityInfo.Type;
            foreach (var rel in ctx.Relations.FindInvolving(type).ToList())
            {
                Type related = rel.Type1 == type ? rel.Type2 : rel.Type1;
                // 后置自动绑定：多导航时跳过（不抛），须由 Relation(nav, join) 消歧
                AutoBindOwner(ctx, entityInfo, related, preferredNavName: null, throwOnAmbiguous: false);
            }
        }

        /// <summary>
        /// 在 owner 上绑定指向 <paramref name="relatedType"/> 的导航；
        /// <paramref name="preferredNavName"/> 非空时只绑该属性。
        /// </summary>
        public static void BindOwnerToRelated(
            EntityContext ctx,
            Type ownerType,
            Type relatedType,
            string preferredNavName = null,
            bool throwOnAmbiguous = true)
        {
            if (ctx == null || ownerType == null || relatedType == null) return;
            // 使用已缓存则避免递归；否则分析
            var en = ctx.getEntityInfo(ownerType);
            if (preferredNavName != null)
                BindNamed(ctx, en, relatedType, preferredNavName);
            else
                AutoBindOwner(ctx, en, relatedType, null, throwOnAmbiguous);
        }

        static void AutoBindOwner(
            EntityContext ctx,
            EntityInfo ownerEn,
            Type relatedType,
            string preferredNavName,
            bool throwOnAmbiguous)
        {
            if (preferredNavName != null)
            {
                BindNamed(ctx, ownerEn, relatedType, preferredNavName);
                return;
            }

            var matches = FindNavProperties(ownerEn.Type, relatedType).ToList();
            if (matches.Count == 0)
                return; // 仅 Registry，无导航属性
            if (matches.Count > 1)
            {
                if (!throwOnAmbiguous) return;
                var names = string.Join(", ", matches.Select(p => p.Name));
                throw new InvalidOperationException(
                    $"类型 {ownerEn.Type.Name} 上存在多个指向 {relatedType.Name} 的导航属性（{names}），请使用 Relation(nav, join) 消歧。");
            }
            ApplyNavigat(ctx, ownerEn, matches[0], relatedType);
        }

        static void BindNamed(EntityContext ctx, EntityInfo ownerEn, Type relatedType, string navName)
        {
            var prop = ownerEn.Type.GetProperty(navName, BindingFlags.Instance | BindingFlags.Public);
            if (prop == null)
                throw new InvalidOperationException($"实体 {ownerEn.Type.Name} 未找到导航属性 {navName}。");
            if (!IsNavTo(prop, relatedType))
                throw new InvalidOperationException(
                    $"属性 {ownerEn.Type.Name}.{navName} 的类型不是 {relatedType.Name} 或其集合。");
            ApplyNavigat(ctx, ownerEn, prop, relatedType);
        }

        static void ApplyNavigat(EntityContext ctx, EntityInfo ownerEn, PropertyInfo navProp, Type relatedType)
        {
            var rel = ctx.Relations.Find(ownerEn.Type, relatedType);
            if (rel == null)
                throw new InvalidOperationException(
                    $"未找到关系配置：{ownerEn.Type.Name} → {relatedType.Name}。请先 Relation。");

            var isCollection = IsCollectionNav(navProp.PropertyType);
            var col = ownerEn.GetColumn(navProp.Name);
            if (col == null)
            {
                col = new EntityColumn(ownerEn)
                {
                    PropertyName = navProp.Name,
                    PropertyInfo = navProp,
                    DbColumnName = navProp.Name,
                    IsIgnore = true
                };
                ownerEn.AddColumnInfo(col);
            }
            else
            {
                if (col.PropertyInfo == null) col.PropertyInfo = navProp;
                col.IsIgnore = true;
            }

            col.Navigat = new EntityNavi
            {
                BossKey = rel.Field1Name,
                SlaveKey = rel.Field2Name,
                ChildType = relatedType,
                MappingType = relatedType,
                NavigatType = isCollection ? EnityNaviType.OneToMany : EnityNaviType.ManyToOne
            };

            ctx.SyncFieldCache(ownerEn.Type, col);
        }

        /// <summary>查找指向 relatedType 的导航属性（引用或集合）。</summary>
        public static IEnumerable<PropertyInfo> FindNavProperties(Type ownerType, Type relatedType)
        {
            if (ownerType == null || relatedType == null) yield break;
            foreach (var p in ownerType.GetProperties(BindingFlags.Instance | BindingFlags.Public))
            {
                if (IsNavTo(p, relatedType))
                    yield return p;
            }
        }

        public static bool IsNavTo(PropertyInfo prop, Type relatedType)
        {
            if (prop == null || relatedType == null) return false;
            var pt = prop.PropertyType;
            if (pt == relatedType) return true;
            if (IsCollectionNav(pt))
            {
                var elem = GetCollectionElementType(pt);
                return elem == relatedType;
            }
            return false;
        }

        public static bool IsCollectionNav(Type type)
        {
            if (type == null || type == typeof(string)) return false;
            if (!typeof(IEnumerable).IsAssignableFrom(type)) return false;
            return GetCollectionElementType(type) != null;
        }

        public static Type GetCollectionElementType(Type type)
        {
            if (type == null) return null;
            if (type.IsArray) return type.GetElementType();
            if (type.IsGenericType)
            {
                var args = type.GetGenericArguments();
                if (args.Length == 1) return args[0];
            }
            foreach (var it in type.GetInterfaces())
            {
                if (it.IsGenericType && it.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                    return it.GetGenericArguments()[0];
            }
            return null;
        }
    }
}
