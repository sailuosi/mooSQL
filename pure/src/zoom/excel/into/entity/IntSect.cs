

namespace mooSQL.excel.context
{
    
    /// <summary>
    /// [2-7]这样的整数区间类。
    /// </summary>
    public class IntSect
    {
        /// <summary>
        /// 最小值
        /// </summary>
        public int? min = null;
        /// <summary>
        /// 最大值
        /// </summary>
        public int? max = null;
        /// <summary>
        /// 一个区间
        /// </summary>
        /// <param name="mi"></param>
        /// <param name="ma"></param>
        public IntSect(int mi, int ma)
        {
            this.min = mi;
            max = ma;
        }
        /// <summary>
        /// 是否闭区间
        /// </summary>
        public bool closed
        {
            get
            {
                return min != null && max != null;
            }
        }
        /// <summary>
        /// 是否包含
        /// </summary>
        /// <param name="val"></param>
        /// <returns></returns>
        public bool Contain(int val)
        {
            bool res = true;
            if (min != null && min != -1) res = res && val >= min;
            if (max != null && max != -1) res = res && val <= max;
            return res;
        }
    }
}
