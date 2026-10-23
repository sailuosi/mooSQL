using System;
using System.Collections.Generic;
using System.Reflection;
using mooSQL.data.model;

namespace mooSQL.data
{
    public partial class SooRepository<T>
    {
        /// <summary>
        /// 知情启用：普通 <see cref="Insert"/> / insertInner 对 Identity 也取库 Id 并回填实体（返回类型仍为行数语义）。默认 false。
        /// </summary>
        public bool EnableInsertReturnId { get; set; }

        /// <summary>
        /// 查找 Identity 主键列；无则 null。
        /// </summary>
        protected EntityColumn FindIdentityColumn()
        {
            if (En?.Columns == null)
                return null;
            EntityColumn fallback = null;
            foreach (var col in En.Columns)
            {
                if (!col.IsIdentity)
                    continue;
                if (col.IsPrimarykey)
                    return col;
                if (fallback == null)
                    fallback = col;
            }
            return fallback;
        }

        /// <summary>
        /// 将库返回的 Id 写入实体属性。
        /// </summary>
        protected static void SetEntityPkValue(EntityColumn col, object entity, object id)
        {
            if (col?.PropertyInfo == null || entity == null || id == null || id is DBNull)
                return;
            var pt = col.PropertyInfo.PropertyType;
            var targetType = Nullable.GetUnderlyingType(pt) ?? pt;
            object converted = id;
            if (!targetType.IsInstanceOfType(id))
                converted = Convert.ChangeType(id, targetType);
            col.PropertyInfo.SetValue(entity, converted);
        }

        /// <summary>
        /// 插入并取回库生成主键，回填实体后返回 Id（object）。无 Identity 列时抛异常。
        /// </summary>
        public object InsertReturnId(T entity)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));
            var idCol = FindIdentityColumn();
            if (idCol == null)
                throw new InvalidOperationException(
                    "InsertReturnId requires an Identity column. For application keys use Insert + AutoKey.");

            var kit = getKit();
            OnBeforeSave(entity);
            var res = Translator.prepareInsert(kit, entity, typeof(T), null, tryTableNameLoader(tbname));
            if (!res.Status)
            {
                kit.Client.Loggor.LogError(res.Message);
                throw new InvalidOperationException(res.Message ?? "prepareInsert failed.");
            }

            var id = kit.doInsertReturnId(idCol.DbColumnName);
            SetEntityPkValue(idCol, entity, id);
            OnAfterSave(entity, id != null && !(id is DBNull) ? 1 : 0);
            return id;
        }

        /// <summary>
        /// 批量插入取 Id：逐行 <see cref="InsertReturnId"/>（P1 降级；P4 可优化 Returning 多行）。
        /// </summary>
        public List<object> InsertReturnIds(IEnumerable<T> entities)
        {
            var list = new List<object>();
            if (entities == null)
                return list;
            foreach (var e in entities)
                list.Add(InsertReturnId(e));
            return list;
        }
    }

    public partial class SooRepository<T, K>
    {
        /// <summary>
        /// 插入并取回强类型主键，回填实体。
        /// </summary>
        public K InsertReturnId(T insertObj)
        {
            var id = ((SooRepository<T>)this).InsertReturnId(insertObj);
            if (id == null || id is DBNull)
                throw new InvalidOperationException("InsertReturnId returned no identity value.");
            if (id is K direct)
                return direct;
            return (K)Convert.ChangeType(id, typeof(K));
        }

        /// <summary>
        /// 批量插入取强类型主键列表。
        /// </summary>
        public List<K> InsertReturnIds(IEnumerable<T> insertObjs)
        {
            var res = new List<K>();
            if (insertObjs == null)
                return res;
            foreach (var obj in insertObjs)
                res.Add(InsertReturnId(obj));
            return res;
        }
    }
}
