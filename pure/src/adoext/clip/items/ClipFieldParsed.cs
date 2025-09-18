using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;

namespace mooSQL.data.clip
{
    /// <summary>
    /// 用于缓存字段解析结果的类
    /// </summary>
    internal class ClipFieldParsed
    {
        public int Id { get; set; }
        /// <summary>
        /// 解析出的字段列表
        /// </summary>
        public List<ClipExpField> ClipFields;



        public string GetFieldCondtionSQL(bool needAlias = true)
        {

            var sb = new StringBuilder();
            bool isFirst = true;
            foreach (var item in ClipFields)
            {
                if (!isFirst)
                {
                    sb.Append(",");
                }
                if (needAlias)
                {
                    sb.Append(item.SQLAlias);
                    sb.Append(".");
                    sb.Append(item.SQLField);
                }
                else
                {
                    sb.Append(item.SQLField);
                }
                isFirst = false;

            }
            return sb.ToString();
        }
    }


    internal class ClipLinqParseCache {

        static FrequencyBasedCache<int, ClipFieldParsed> _cache;

        public static FrequencyBasedCache<int, ClipFieldParsed> Cache
        {
            get { 
            
                if (_cache == null)
                {
                    _cache = new FrequencyBasedCache<int, ClipFieldParsed>(TimeSpan.FromMinutes(10));
                }
                return _cache;
            }
        }


    }
}
