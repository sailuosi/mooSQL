using System;
using System.Collections.Generic;
using System.Linq;
using mooSQL.data.model;

namespace mooSQL.data
{
    public partial class SooRepository<T>
    {
        /// <summary>
        /// 知情启用：普通 <see cref="Insert"/> / insertInner 对 Identity 也取库 Id 并回填实体（返回类型仍为行数语义）。
        /// 默认 false。与 <see cref="MooClient.EnableInsertReturnId"/> 任一为 true 即生效。
        /// </summary>
        public bool EnableInsertReturnId { get; set; }

        /// <summary>Repo 或 Client 是否开启 Insert 路径取 Id。</summary>
        protected bool IsInsertReturnIdEnabled
            => EnableInsertReturnId || (DBLive?.client?.EnableInsertReturnId == true);

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
        /// <c>SkipOnInsert=false</c>（即非 <see cref="EntityColumn.IsOnlyIgnoreInsert"/>）且主键已非 null：
        /// 跳过库取 Id / 二次回填，返回实体已有值。
        /// </summary>
        protected static bool TryGetAssignedIdentitySkipFetch(EntityColumn idCol, object entity, out object existingId)
        {
            existingId = null;
            if (idCol?.PropertyInfo == null || entity == null)
                return false;
            if (idCol.IsOnlyIgnoreInsert)
                return false;
            var val = idCol.PropertyInfo.GetValue(entity);
            if (val == null)
                return false;
            existingId = val;
            return true;
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

            if (TryGetAssignedIdentitySkipFetch(idCol, entity, out var existingId))
            {
                var resAssigned = Translator.prepareInsert(kit, entity, typeof(T), null, tryTableNameLoader(tbname));
                if (!resAssigned.Status)
                {
                    kit.Client.Loggor.LogError(resAssigned.Message);
                    throw new InvalidOperationException(resAssigned.Message ?? "prepareInsert failed.");
                }
                var rows = kit.doInsert();
                OnAfterSave(entity, rows);
                return existingId;
            }

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
        /// 批量插入取 Id。Returning/OUTPUT 方言走多行一次 Reader；否则逐行 <see cref="InsertReturnId"/>。
        /// </summary>
        public List<object> InsertReturnIds(IEnumerable<T> entities)
        {
            var list = new List<object>();
            if (entities == null)
                return list;

            var arr = entities as IList<T> ?? entities.ToList();
            if (arr.Count == 0)
                return list;

            var idCol = FindIdentityColumn();
            if (idCol == null)
                throw new InvalidOperationException(
                    "InsertReturnIds requires an Identity column. For application keys use Insert + AutoKey.");

            var kit = getKit();
            var canBatch = arr.Count > 1
                && kit.Dialect.SupportsMultiRowInsertReturnId(idCol.DbColumnName);

            // 任一行已显式赋值且非 IgnoreInsert → 不能一批取 Id，逐行处理
            if (canBatch)
            {
                foreach (var e in arr)
                {
                    if (TryGetAssignedIdentitySkipFetch(idCol, e, out _))
                    {
                        canBatch = false;
                        break;
                    }
                }
            }

            if (!canBatch)
            {
                foreach (var e in arr)
                    list.Add(InsertReturnId(e));
                return list;
            }

            kit.clear();
            var first = true;
            foreach (var e in arr)
            {
                if (e == null)
                    throw new ArgumentNullException(nameof(entities));
                OnBeforeSave(e);
                if (!first)
                    kit.newRow();
                first = false;
                var res = Translator.prepareInsert(kit, e, typeof(T), null, tryTableNameLoader(tbname));
                if (!res.Status)
                {
                    kit.Client.Loggor.LogError(res.Message);
                    throw new InvalidOperationException(res.Message ?? "prepareInsert failed.");
                }
            }

            var ids = kit.doInsertReturnIds(idCol.DbColumnName);
            if (ids.Count != arr.Count)
                throw new InvalidOperationException(
                    "InsertReturnIds: expected " + arr.Count + " ids, got " + ids.Count + ".");

            for (var i = 0; i < arr.Count; i++)
            {
                SetEntityPkValue(idCol, arr[i], ids[i]);
                OnAfterSave(arr[i], ids[i] != null && !(ids[i] is DBNull) ? 1 : 0);
                list.Add(ids[i]);
            }
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
        /// 批量插入取强类型主键列表（Returning 方言可一批；否则逐行）。
        /// </summary>
        public List<K> InsertReturnIds(IEnumerable<T> insertObjs)
        {
            var raw = ((SooRepository<T>)this).InsertReturnIds(insertObjs);
            var res = new List<K>(raw.Count);
            foreach (var id in raw)
            {
                if (id == null || id is DBNull)
                    throw new InvalidOperationException("InsertReturnIds returned no identity value.");
                if (id is K direct)
                    res.Add(direct);
                else
                    res.Add((K)Convert.ChangeType(id, typeof(K)));
            }
            return res;
        }
    }
}
