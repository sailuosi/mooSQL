using System;
using System.Collections.Generic;
using System.Reflection;

namespace mooSQL.data
{
    /// <summary>
    /// 增加了强类型主键约束的仓储。
    /// 写入并返回主键的方法与 SQLBuilder 命名对齐：<c>InsertReturnId</c> / <c>UpdateReturnId</c>。
    /// </summary>
    public partial class SooRepository<T, K> : SooRepository<T> where T : class, new()
    {
        /// <summary>
        /// 初始化 SooRepository（构造）。
        /// </summary>
        public SooRepository(DBInstance DB) : base(DB)
        {
        }

        private Func<T, K> _loadPK { get; set; }
        /// <summary>
        /// 是否自动主键
        /// </summary>
        protected bool? autoPK { get; set; }

        /// <summary>
        /// 设置主键取值方式
        /// </summary>
        public SooRepository<T, K> OnGetPKValue(Func<T, K> loader)
        {
            this._loadPK = loader;
            return this;
        }

        /// <summary>
        /// 获取PKValue。
        /// </summary>
        public virtual K GetPKValue(T entity)
        {
            if (this._loadPK != null)
            {
                return _loadPK(entity);
            }
            var pk = this.En.GetPK();
            if (pk.Count != 1) return default(K);
            var pku = pk[0];
            PropertyInfo property = pku.PropertyInfo;
            if (property?.PropertyType == typeof(K))
            {
                return (K)property.GetValue(entity);
            }
            throw new ArgumentException("类型不匹配");
        }

        /// <summary>
        /// 插入并返回强类型主键（对齐 SQLBuilder <c>doInsertReturnId</c>）。
        /// Identity：取库生成 Id 并回填实体；应用发号：插完读实体已有 PK。
        /// </summary>
        public new K InsertReturnId(T insertObj)
        {
            if (insertObj == null)
                throw new ArgumentNullException(nameof(insertObj));

            if (FindIdentityColumn() != null)
            {
                var idObj = ((SooRepository<T>)this).InsertReturnId(insertObj);
                return ConvertToKey(idObj);
            }

            var kit = getKit();
            var res = insertInner(insertObj, kit);
            if (res > 0)
                return GetPKValue(insertObj);
            throw new InvalidOperationException("InsertReturnId failed: insert affected 0 rows.");
        }

        /// <summary>
        /// 批量插入并返回强类型主键列表（对齐 <c>doInsertReturnIds</c>）。
        /// </summary>
        public new List<K> InsertReturnIds(IEnumerable<T> insertObjs)
        {
            if (insertObjs == null)
                throw new ArgumentNullException(nameof(insertObjs));

            if (FindIdentityColumn() != null)
            {
                var raw = ((SooRepository<T>)this).InsertReturnIds(insertObjs);
                var typed = new List<K>(raw.Count);
                foreach (var id in raw)
                    typed.Add(ConvertToKey(id));
                return typed;
            }

            var kit = getKit();
            var res = new List<K>();
            foreach (var obj in insertObjs)
            {
                if (obj == null)
                    throw new ArgumentNullException(nameof(insertObjs));
                var c = insertInner(obj, kit);
                if (c <= 0)
                    throw new InvalidOperationException("InsertReturnIds failed: insert affected 0 rows.");
                res.Add(GetPKValue(obj));
            }
            return res;
        }

        /// <summary>
        /// 更新并返回实体主键（命名对齐 InsertReturnId；更新不取库生成值）。
        /// </summary>
        public K UpdateReturnId(T updateObj)
        {
            if (updateObj == null)
                throw new ArgumentNullException(nameof(updateObj));
            var kit = getKit();
            var res = updateInner(updateObj, kit);
            if (res > 0)
                return GetPKValue(updateObj);
            throw new InvalidOperationException("UpdateReturnId failed: update affected 0 rows.");
        }

        /// <summary>
        /// 批量更新并返回主键列表。
        /// </summary>
        public List<K> UpdateReturnIds(IEnumerable<T> updateObjs)
        {
            if (updateObjs == null)
                throw new ArgumentNullException(nameof(updateObjs));
            var kit = getKit();
            var res = new List<K>();
            foreach (var obj in updateObjs)
            {
                if (obj == null)
                    throw new ArgumentNullException(nameof(updateObjs));
                var c = updateInner(obj, kit);
                if (c <= 0)
                    throw new InvalidOperationException("UpdateReturnIds failed: update affected 0 rows.");
                res.Add(GetPKValue(obj));
            }
            return res;
        }

        /// <summary>
        /// 保存（有则更新/无则插入）并返回主键列表。
        /// </summary>
        public List<K> SaveReturnIds(IEnumerable<T> objs)
        {
            if (objs == null)
                throw new ArgumentNullException(nameof(objs));
            var kit = getKit();
            var res = new List<K>();
            foreach (var obj in objs)
            {
                if (obj == null)
                    throw new ArgumentNullException(nameof(objs));
                var c = SaveInner(obj, kit);
                if (c <= 0)
                    throw new InvalidOperationException("SaveReturnIds failed: save affected 0 rows.");
                res.Add(GetPKValue(obj));
            }
            return res;
        }

        static K ConvertToKey(object id)
        {
            if (id == null || id is DBNull)
                throw new InvalidOperationException("InsertReturnId returned no identity value.");
            if (id is K direct)
                return direct;
            return (K)Convert.ChangeType(id, typeof(K));
        }

        #region Obsolete aliases（旧 WithPK 命名 → ReturnId）

        /// <summary>已更名为 <see cref="InsertReturnId"/>，与 SQLBuilder 对齐。</summary>
        [Obsolete("Use InsertReturnId instead (aligned with SQLBuilder doInsertReturnId).")]
        public K InsertWithPK(T insertObj) => InsertReturnId(insertObj);

        /// <summary>已更名为 <see cref="InsertReturnIds"/>。</summary>
        [Obsolete("Use InsertReturnIds instead.")]
        public List<K> InsertRangeWithPK(IEnumerable<T> insertObjs) => InsertReturnIds(insertObjs);

        /// <summary>已更名为 <see cref="UpdateReturnId"/>。</summary>
        [Obsolete("Use UpdateReturnId instead.")]
        public K UpdateWithPK(T updateObj) => UpdateReturnId(updateObj);

        /// <summary>已更名为 <see cref="UpdateReturnIds"/>。</summary>
        [Obsolete("Use UpdateReturnIds instead.")]
        public List<K> UpdateRangeWithPK(IEnumerable<T> updateObjs) => UpdateReturnIds(updateObjs);

        /// <summary>已更名为 <see cref="SaveReturnIds"/>。</summary>
        [Obsolete("Use SaveReturnIds instead.")]
        public List<K> SaveRangeWithPK(IEnumerable<T> updateObjs) => SaveReturnIds(updateObjs);

        #endregion
    }
}
