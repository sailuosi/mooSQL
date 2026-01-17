using System;


namespace mooSQL.utils
{
    /// <summary>
    /// 起止区间类
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class Sect<T>
    {
        //where T : struct 不再限制的值的范围
        /// <summary>
        /// 最小值
        /// </summary>
        public T min;
        /// <summary>
        /// 最大值
        /// </summary>
        public T max;
        /// <summary>
        /// 无效值
        /// </summary>
        public T invalidValue;
        /// <summary>
        /// 左边是闭区间
        /// </summary>
        public bool containLeft = true;
        /// <summary>
        /// 右侧是否是闭区间
        /// </summary>
        public bool containRight = true;
        /// <summary>
        /// 
        /// </summary>
        /// <param name="minValue"></param>
        /// <param name="maxValue"></param>
        public Sect(T minValue, T maxValue)
        {
            this.min = minValue;
            max = maxValue;
        }
        /// <summary>
        /// 返回值是否有效
        /// </summary>

        public Func<T, bool> isValid;
        /// <summary>
        /// 比较2个值的大小，如果-1，则小于，0 =，1>
        /// </summary>
        public Func<T, T, int> compare;

        public bool closed
        {
            get
            {
                return isValid(min) && isValid(max);
            }
        }
        /// <summary>
        /// 是否包含
        /// </summary>
        /// <param name="val"></param>
        /// <returns></returns>
        public bool Contain(T val)
        {
            bool res = true;
            if (isValid(min))
            {
                if (containLeft)
                {
                    if (compare(min, val) > 0)
                    {//最小值比该值大，则不包含
                        return false;
                    }
                }
                else
                {
                    if (compare(min, val) >= 0)
                    {//最小值>=改值。不包含
                        return false;
                    }
                }
            }
            if (isValid(max))
            {
                if (containRight)
                {
                    if (compare(max, val) < 0)
                    {//最大值<val 不包含
                        return false;
                    }
                }
                else
                {
                    if (compare(max, val) <= 0)
                    {//最大值<=val 不包含
                        return false;
                    }
                }
            }
            return res;
        }
    }
}
