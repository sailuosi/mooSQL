// 基础功能说明：

using mooSQL.data;
using mooSQL.utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace mooSQL.auth
{
    /// <summary>
    /// 扩展类，用于生成where表达示的条件部分。
    /// </summary>
    public static class WorDExtension {
        /// <summary>
        /// 创建where表达示的条件部分
        /// </summary>
        public static SQLBuilder generateCondition(this SQLBuilder builder, Condition filterObj)
        {
            if (filterObj == null || string.IsNullOrWhiteSpace(filterObj.Key) || string.IsNullOrWhiteSpace(filterObj.Contrast) || string.IsNullOrWhiteSpace(filterObj.Text))
                if (filterObj.Paramed == false)
                {
                    //非参数化时，执行SQL注入过滤
                    filterObj.Text = RegxUntils.SqlFilter(filterObj.Text, true);
                }
            //键部分执行严格的SQL过滤。
            filterObj.Key = RegxUntils.SqlFilter(filterObj.Key, false);

            switch (filterObj.Contrast)
            {
                case "==":
                    builder.where(filterObj.Key, filterObj.getValue(), "=", filterObj.Paramed);
                    break;
                case "<=":
                case "<":
                case ">":
                case ">=":
                case "!=":
                    builder.where(filterObj.Key, filterObj.getValue(), filterObj.Contrast, filterObj.Paramed);
                    break;
                case "contains":
                    builder.whereLike(filterObj.Key, filterObj.getValue());
                    break;
                case "in":
                    var inlist = filterObj.getListValue(); //数组
                    builder.whereIn(filterObj.Key, inlist);
                    break;
                case "not in":
                    var notinlist = filterObj.getListValue(); //数组
                    builder.whereIn(filterObj.Key, notinlist);
                    break;
                //交集，使用交集时左值必须时固定的值
                case "between": //交集
                    var btlist = filterObj.getListValue(); //数组
                    var cc = 0;
                    object val1=null;
                    object val2=null;
                    foreach (var item in btlist)
                    {
                        if (cc == 0) { 
                            val1 = item;
                        }
                        else if(cc == 1) { }
                        {
                            val2 = item;
                        }
                        cc++;
                    }
                    if (cc != 2)
                    {
                        return null;
                    }

                    builder.whereBetween(filterObj.Key, val1, val2);
                    break;
            }

            return builder;
        }

    }
}
