using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data.mapping
{
    /// <summary>
    /// 映射分组，存储同一映射方向的所有映射，比如从表到实体，或者从实体到表的映射
    /// </summary>
    public class MappingGroup
    {

        private ConcurrentDictionary<string, MappingItem> _mappings ;
        /// <summary>
        /// 构造函数，传入分组名称
        /// </summary>
        /// <param name="groupName"></param>
        public MappingGroup(string groupName)
        {
            GroupName = groupName;
            _mappings = new ConcurrentDictionary<string, MappingItem>();
        }

        public string GroupName { get; set; }
        /// <summary>
        /// 添加映射项
        /// </summary>
        /// <typeparam name="F"></typeparam>
        /// <typeparam name="T"></typeparam>
        /// <param name="converter"></param>
        public void Add<F, T>(Func<F, T> converter)
        {
            var tar = MappingItem.Create(converter);

            if (_mappings.ContainsKey(tar.Key))
            {
                _mappings[tar.Key] = tar;
            }
            else
            {
                _mappings.TryAdd(tar.Key, tar);
            }
        }
        public Func<F, T> Get<F, T>()
        {
            var key= typeof(F).FullName + "--" + typeof(T).FullName;

            if (_mappings.ContainsKey(key))
            {
                return _mappings[key].Convertor as Func<F, T>;
            }
            else
            {
                return null;
            }
        }

        public Delegate Get(Type From,Type To )
        {
            var key = From.FullName + "--" + To.FullName;

            if (_mappings.ContainsKey(key))
            {
                return _mappings[key].Convertor;
            }
            else
            {
                return null;
            }
        }
        /// <summary>
        /// 执行转换
        /// </summary>
        /// <typeparam name="TFrom"></typeparam>
        /// <typeparam name="TTo"></typeparam>
        /// <param name="source"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        public TTo Convert<TFrom, TTo>(TFrom source)
        {
            var key = typeof(TFrom).FullName + "--" + typeof(TTo).FullName;
            if (_mappings.TryGetValue(key, out var item))
            {
                return item.Convert<TFrom,TTo>(source);
            }
            throw new InvalidOperationException($"类型转换未注册： {typeof(TFrom)} 转换到 {typeof(TTo)} ");
        }
        public TTo Convert<TTo>(object source)
        {

            var key = source.GetType().FullName + "--" + typeof(TTo).FullName;
            if (_mappings.TryGetValue(key, out var item))
            {
                return (TTo)item.Convertor.DynamicInvoke(source);
            }
            return default(TTo);
        }
        public object Convert(object source,Type tar)
        {

            var key = source.GetType().FullName + "--" + tar.FullName;
            if (_mappings.TryGetValue(key, out var item))
            {
                return item.Convertor.DynamicInvoke(source);
            }
            return default;
        }
        /// <summary>
        /// 检查是否存在转换关系
        /// </summary>
        /// <param name="sourceType"></param>
        /// <param name="targetType"></param>
        /// <returns></returns>
        public bool CanConvert(Type sourceType, Type targetType)
        {
            return _mappings.ContainsKey(sourceType.FullName + "--" + targetType.FullName);
        }
        /// <summary>
        /// 检查是否存在转换关系
        /// </summary>
        /// <typeparam name="S"></typeparam>
        /// <typeparam name="R"></typeparam>
        /// <returns></returns>
        public bool CanConvert<S,R>()
        {
            return _mappings.ContainsKey(typeof(S).FullName + "--" + typeof(R).FullName);
        }
    }


}
