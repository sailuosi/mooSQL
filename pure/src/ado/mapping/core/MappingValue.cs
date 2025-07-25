using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data.mapping
{
    /// <summary>
    /// 值的映射，比如数据库Varchar映射为C#的string类型，int映射为Int32等。
    /// </summary>
    public class MappingValue<S,R>
    {
        /// <summary>
        /// 是否可逆，比如数据库的int和C#的Int32是可逆的。
        /// </summary>
        public bool Reversable { get; set; }
        /// <summary>
        /// 名称
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// 映射关系
        /// </summary>
        public ConcurrentDictionary<S, R> map { get; set; }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="name"></param>
        public MappingValue(string name)
        {
            map = new ConcurrentDictionary<S, R>();
            Name = name;

        }
        /// <summary>
        /// 添加映射关系,如果已经存在，则覆盖。
        /// </summary>
        /// <param name="source"></param>
        /// <param name="result"></param>
        public void Add(S source, R result)
        {
            if (map.ContainsKey(source)) {
                map[source] = result; //如果已经存在，则覆盖
                return;
            }
            map.TryAdd(source, result);
        }
        /// <summary>
        /// 获取映射关系，如果不存在则返回默认值。
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        public R Get(S source) {
            if (map.ContainsKey(source)) {
                return map[source];
            }
            return default(R);
        }
        /// <summary>
        /// 尝试获取映射关系，如果不存在则返回false。
        /// </summary>
        /// <param name="source"></param>
        /// <param name="res"></param>
        /// <returns></returns>
        public bool TryGet(S source,out R res)
        {
            if (map.ContainsKey(source))
            {
                res= map[source];
                return true;
            }
            res= default(R);
            return false;
        }
    }
}
