using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data
{
    /// <summary>
    /// JOIN语句构造中间过渡类
    /// </summary>
    public class ClipJoin<T>
    {
        /// <summary>
        /// 根Clip实例引用。
        /// </summary>
        public SQLClip root;
        /// <summary>
        /// JOIN的目标表实例引用。
        /// </summary>
        public object JoinTarget { get; set; }
        /// <summary>
        /// JOIN的类型，例如INNER JOIN、LEFT JOIN等。
        /// </summary>
        public string JoinType { get; set; }
        public ClipJoin(SQLClip roo)
        {
            this.root = roo;
        }
        /// <summary>
        /// 构造JOIN语句的ON条件。
        /// </summary>
        /// <param name="joinCondition"></param>
        /// <returns></returns>
        public SQLClip on(Expression<Func<bool>> joinCondition)
        {
            root.provider.PatchJoin(joinCondition, this);
            return this.root;
        }
    }
}
