using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data.mapping
{
    public class MappingItem
    {
        /// <summary>
        /// 源类型
        /// </summary>
        public Type From { get; set; }
        /// <summary>
        /// 目标类型
        /// </summary>
        public Type To { get; set; }

        /// <summary>
        /// 转换器
        /// </summary>
        public Delegate Convertor { get; set; }

        /// <summary>
        /// 联合键，用于缓存查找。
        /// </summary>
        public string Key
        {
            get {
                return From.FullName + "--" + To.FullName;
            }
        }
        /// <summary>
        /// 私有化，以禁止外部直接实例化。
        /// </summary>
        private MappingItem() { 
        
        }
        /// <summary>
        /// 转换方法。
        /// </summary>
        /// <typeparam name="S"></typeparam>
        /// <typeparam name="R"></typeparam>
        /// <param name="source"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        public R Convert<S,R>(S source)
        {
            var func=Convertor as Func<S, R>;
            if (func == null) { 
                throw new InvalidOperationException("不应该发生的问题,转换器类型不匹配。");
            }
            return func(source);
        }
        /// <summary>
        /// 创建映射项。
        /// </summary>
        /// <typeparam name="F"></typeparam>
        /// <typeparam name="T"></typeparam>
        /// <param name="converter"></param>
        /// <returns></returns>
        public static MappingItem Create<F,T>(Func<F, T> converter){
            var target = new MappingItem();
            target.From = typeof(F);
            target.To = typeof(T);
            target.Convertor = converter;
            return target;
        }
    }
}
