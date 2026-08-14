// 基础功能说明：

using mooSQL.excel.context;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.excel
{
    /// <summary>
    /// Excel 导入过程中的字符串校验与按类型解析的通用工具。
    /// </summary>
    public class BaseUtil
    {
        /// <summary>
        /// 是否为有效字符串
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public bool isValid(string str)
        {
            if (string.IsNullOrWhiteSpace(str))
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        /// <summary>
        /// 按字符串类型名将单元格值解析为 SQL 字面量与托管对象。
        /// </summary>
        /// <param name="invalue">原始字符串。</param>
        /// <param name="type">类型键：free/string/bool/number/date/guid 等。</param>
        /// <param name="caption">列标题，用于错误信息。</param>
        /// <param name="msg">解析过程中的提示或警告。</param>
        /// <param name="resF">解析后的对象值。</param>
        /// <returns>可用于 SQL 拼接的字符串，失败时可能为 null。</returns>
        public string parseValue(string invalue, string type, string caption, out string msg, out Object resF)
        {
            msg = "";
            if (invalue == null)
            {
                resF = null;
                return null;
            }
            string res = "";
            switch (type)
            {
                case "free"://自由模式下用户自主控制，不作处理。
                    res = invalue;
                    resF = invalue;
                    break;
                case "string":
                    res = "'" + invalue + "'";
                    resF = invalue;
                    break;
                case "bool":
                    if (invalue == "true" || invalue == "True" || invalue == "1")
                    {
                        resF = true;
                        res = "1";
                    }
                    else if (invalue == "false" || invalue == "False" || invalue == "0")
                    {
                        resF = false;
                        res = "0";
                    }
                    else
                    {
                        resF = null;
                        return "null";
                    }
                    break;
                case "number":
                    if (invalue == "")
                    {
                        //this.pushInfo(wkinfo.rowinfo + caption + "值为空!<br/>","tip");
                        resF = 0;
                        return "0";
                    }
                    double num;
                    var cans = Double.TryParse((string)invalue, out num);
                    if (!cans)
                    {
                        if (caption != "") msg += caption + "值" + invalue + "转换为数值时失败！将置为空。<br/>";
                        resF = 0;
                        res = "0";
                    }
                    else
                    {
                        resF = num;
                        res = num.ToString();
                    }
                    break;
                case "date":
                    if (invalue == "")
                    {
                        //this.pushInfo(wkinfo.rowinfo + caption + "值为空!<br/>");
                        resF = null;
                        return "''";
                    }
                    DateTime rdate = new DateTime();
                    string[] dateformats = {
                        "yyyyMMdd","yyyy-MM-dd","yyyy-MM","yyyy.MM.dd","yyyy.M.d",
                        "yyyy/M/d","yyyy/MM/dd",
                        "yyyy/M/d tt hh:mm:ss",
                        "yyyy/MM/dd tt hh:mm:ss",
                        "yyyy/MM/dd HH:mm:ss",
                        "yyyy/M/d HH:mm:ss",
                    };
                    bool suc = DateTime.TryParse(invalue.ToString(), out rdate);
                    if (!suc)
                    {
                        if (DateTime.TryParseExact(invalue.ToString(), dateformats, null, System.Globalization.DateTimeStyles.AllowWhiteSpaces, out rdate))
                        {
                            suc = true;
                        }
                        else if (System.Text.RegularExpressions.Regex.IsMatch(invalue.ToString(), @"\d{1,6}$"))
                        {
                            //依然不行时，尝试
                            double nume = 0;
                            if (double.TryParse(invalue.ToString(), out nume))
                            {
                                try
                                {
                                    rdate = DateTime.Parse("1900-01-01 00:00:00.000").AddDays(nume - 2);
                                    msg += caption + "值" + invalue + "不是有效的日期，请核对！将置为空。<br/>";
                                    suc = true;
                                }
                                catch (Exception e)
                                {
                                    suc = false;
                                    res = null;
                                    resF = null;
                                    return null;
                                }


                            }
                        }
                    }
                    //为解决报错：【SqlDateTime 溢出。必须介于 1/1/1753 12:00:00 AM 和 12/31/9999 11:59:59 PM之间】而做的针对性核查。
                    if (rdate.Year < 1900 || rdate.Year > 9999)
                    {
                        suc = false;
                    }
                    if (suc)
                    {
                        resF = rdate;
                        res = "'" + rdate.ToString() + "'";
                    }
                    else
                    {
                        if (caption != "") msg += caption + "值" + invalue + "转换为日期时失败！将置为空。<br/>";
                        resF = null;
                        res = "'" + DateTime.Parse("1900-01-01 00:00:00.000").ToShortDateString() + "'";
                    }

                    // DateTime.Parse(val.ToString()).g
                    break;
                case "guid":
                    if (invalue == "")
                    {
                        //res = "'00000000-0000-0000-0000-000000000000'";
                        resF = null;
                        return null;
                    }
                    else
                    {
                        try
                        {
                            resF = new Guid(invalue);
                        }
                        catch
                        {
                            resF = null;
                            if (caption != "") msg += caption + "值" + invalue + "转换为guid时失败！将置为空。<br/>";
                            return null;
                        }

                        res = "'" + invalue + "'";
                    }

                    break;
                default:
                    resF = invalue;
                    res = "'" + invalue + "'";
                    break;
            }
            return res;
        }

        //*类型转换*/
        /// <summary>
        /// 按 <see cref="valueType"/> 枚举解析单元格值。
        /// </summary>
        /// <param name="invalue">原始字符串。</param>
        /// <param name="type">列值类型枚举。</param>
        /// <param name="caption">列标题。</param>
        /// <param name="msg">提示信息。</param>
        /// <param name="resF">解析结果对象。</param>
        /// <returns>SQL 字面量或 null。</returns>
        public string parseValue(string invalue, valueType type, string caption, out string msg, out Object resF)
        {
            var map = new Dictionary<valueType, string>() {
                {valueType.stringi,"string" },
                {valueType.number,"number" },
                {valueType.date,"date" },
                {valueType.guid,"guid" },
                {valueType.boolean,"bool" },
                {valueType.free,"free" },
            };
            if (map.ContainsKey(type))
            {
                return parseValue(invalue, map[type], caption, out msg, out resF);
            }
            else
            {
                msg = "列类型未知！";
                resF = invalue;
                return invalue;
            }

        }
    }
}