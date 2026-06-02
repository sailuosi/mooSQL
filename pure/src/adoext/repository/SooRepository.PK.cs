using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data
{
    /// <summary>
    /// 增加了强类型主键约束的仓储
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <typeparam name="K"></typeparam>
    public class SooRepository<T, K> : SooRepository<T> where T : class, new()
    {
        /// <summary>
        /// 初始化 SooRepository（构造）。
        /// </summary>
        public SooRepository(DBInstance DB) : base(DB)
        {
        }

        private Func<T,K> _loadPK { get; set; }
        /// <summary>
        /// 是否自动主键
        /// </summary>
        protected bool? autoPK { get; set; }
        /// <summary>
        /// 设置主键取值方式
        /// </summary>
        /// <param name="loader"></param>
        /// <returns></returns>
        public SooRepository<T, K> OnGetPKValue(Func<T, K> loader) {
            this._loadPK = loader;
            return this;
        }

        /// <summary>
        /// 获取PKValue。
        /// </summary>
        public virtual K GetPKValue(T entity) {
            if (this._loadPK != null) {
                return _loadPK(entity);
            }
            var pk = this.En.GetPK();
            if (pk.Count != 1) return default(K);
            var pku= pk[0];
            PropertyInfo property = pku.PropertyInfo;
            if (property?.PropertyType == typeof(K))
            {
                return (K)property.GetValue(entity);
            }
            throw new ArgumentException("类型不匹配");
        }

        /// <summary>
        /// 插入WithPK。
        /// </summary>
        public K InsertWithPK(T insertObj)
        {
            var kit = getKit();
            var res = insertInner(insertObj, kit);
            if (res > 0) {
                return GetPKValue(insertObj);
            }
            return default(K);
        }
        /// <summary>
        /// 批量插入
        /// </summary>
        /// <param name="insertObjs"></param>
        /// <returns></returns>
        public List<K> InsertRangeWithPK(IEnumerable<T> insertObjs)
        {
            var kit = getKit();
            var cc = 0;
            var res= new List<K>();
            foreach (var obj in insertObjs)
            {
                var c = insertInner(obj, kit);
                if (c > 0)
                {
                    //执行失败的返回为-1，不能直接累计
                    cc += c;
                    res.Add(GetPKValue(obj));
                }
            }

            return res;
        }
        /// <summary>
        /// 更新
        /// </summary>
        /// <param name="updateObj"></param>
        /// <returns></returns>
        public K UpdateWithPK(T updateObj)
        {
            var kit = getKit();
            var res = updateInner(updateObj, kit);
            if (res > 0)
            {
                return GetPKValue(updateObj);
            }
            return default(K);
        }
        /// <summary>
        /// 批量更新
        /// </summary>
        /// <param name="updateObjs"></param>
        /// <returns></returns>
        public List<K> UpdateRangeWithPK(IEnumerable<T> updateObjs)
        {
            var kit = getKit();
            var cc = 0;
            var res = new List<K>();
            foreach (var obj in updateObjs)
            {
                var c = updateInner(obj, kit);
                if (c > 0)
                {
                    //执行失败的返回为-1，不能直接累计
                    cc += c;
                    res.Add(GetPKValue(obj));
                }
            }

            return res;
        }
        /// <summary>
        /// 返回保存好的主键
        /// </summary>
        /// <param name="updateObjs"></param>
        /// <returns></returns>
        public List<K> SaveRangeWithPK(IEnumerable<T> updateObjs)
        {
            var kit = getKit();
            var cc = 0;
            var res = new List<K>();
            foreach (var obj in updateObjs)
            {
                var c = SaveInner(obj, kit);
                if (c > 0)
                {
                    //执行失败的返回为-1，不能直接累计
                    cc += c;
                    res.Add(GetPKValue(obj));
                }
            }

            return res;
        }
    }
}