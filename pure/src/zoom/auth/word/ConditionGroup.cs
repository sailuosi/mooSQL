// 基础功能说明：

using mooSQL.data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.auth
{
    /// <summary>
    /// 一组条件
    /// </summary>
    public class ConditionGroup
    {
        /// <summary>
        /// 根权限范围定义
        /// </summary>
        public WordTranslator translator;
        /// <summary>
        /// 名称
        /// </summary>
        public string Text{  get; set; }
        /// <summary>
        /// 识别名
        /// </summary>
        public string key {  get; set; }
        /// <summary>
        /// or /and
        /// </summary>
        public string Operation { get; set; }
        /// <summary>
        /// 条件集合
        /// </summary>
        public Condition[] Filters { get; set; }
        /// <summary>
        /// 子条件
        /// </summary>
        public ConditionGroup[] Children { get; set; }
        /// <summary>
        /// 执行器
        /// </summary>
        public Action<ConditionGroup, SQLBuilder> nextWordBuildInvoker;
        /// <summary>
        /// 应用到SQL中
        /// </summary>
        /// <param name="builder"></param>
        /// <returns></returns>
        public SQLBuilder ApplyToSQL( SQLBuilder builder)
        {
            if (Filters == null || Filters.Count() == 0)
            {
                return builder;
            }

            builder.sink(Operation);
            foreach (var filter in Filters)
            {
                translator.dialect.pipeline.readAuthWordPara(filter,translator.para);
                builder.generateCondition(filter);
            }
            if(Children != null && Children.Length>0)
            {
                foreach (var child in Children)
                {
                    child.translator= translator;
                    if (nextWordBuildInvoker != null) {
                        child.nextWordBuildInvoker = nextWordBuildInvoker;
                    }
                    
                    if (child.nextWordBuildInvoker != null)
                    {
                        child.nextWordBuildInvoker(child, builder);
                    }
                    else {
                        child.ApplyToSQL(builder);
                    }
                    
                }
            }

            builder.rise();

            return builder;
        }

    }
}
