using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace mooSQL.utils
{
    /// <summary>
    /// 更新 2021-8-11 增加 格式为regx=开头的正则模式。
    /// </summary>
    public class myUntils
    {
        /// <summary>
        /// SQL注入过滤，使用正则表达式。
        /// </summary>
        /// <param name="source"></param>
        /// <param name="onlyWrite"></param>
        /// <returns></returns>
        public static string SqlFilter(string source, bool onlyWrite)
        {
            //sql注入过滤
            if (!onlyWrite)
            {
                //半角括号替换为全角括号
                source = source.Replace("'", "'''");
                source = Regex.Replace(source, "(select|from)", "", RegexOptions.IgnoreCase);
            }

            //去除执行SQL语句的命令关键字
            source = Regex.Replace(source, "(insert|update|delete|drop|truncate|declare|xp_cmdshell|exec|execute)", "", RegexOptions.IgnoreCase);
            source = Regex.Replace(source, "/add", "", RegexOptions.IgnoreCase);
            source = Regex.Replace(source, "net user", "", RegexOptions.IgnoreCase);
            //去除系统存储过程或扩展存储过程关键字
            source = Regex.Replace(source, "xp_", "x p_", RegexOptions.IgnoreCase);
            source = Regex.Replace(source, "sp_", "s p_", RegexOptions.IgnoreCase);
            //防止16进制注入
            source = Regex.Replace(source, "0x", "0 x", RegexOptions.IgnoreCase);
            return source;
        }
        public void mapAdd<K, T>(Dictionary<K, T> map, K key, T value)
        {
            if (value == null) { return; }
            if (map.ContainsKey(key))
            {
                map[key] = value;
            }
            else
            {
                map.Add(key, value);
            }
        }
        public void ListAdd<T>(List<T> list, T value)
        {
            if (list.Contains(value) == false) list.Add(value);
        }
        public V getMapValue<K,V>(Dictionary<K, V> map, K key)
        {
            if (key == null) return default(V);
            if (map.ContainsKey(key))
            {
                return map[key];
            }
            else
            {
                return default(V);
            }
        }
        /// <summary>
        /// 将src的所有成员添加到tar中去，略过重复。
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="tar"></param>
        /// <param name="src"></param>
        public List<T> ListMerge<T>(List<T> tar,List<T> src)
        {
            foreach(var li in src)
            {
                if (tar.Contains(li) == false)
                {
                    tar.Add(li);
                }
            }
            return tar;
        }
        public bool isMatch(string checkStr, string Regstr)
        {
            var reg = new Regex(Regstr);
            bool res = false;
            if (checkStr == Regstr || reg.IsMatch(checkStr) || checkStr.IndexOf(Regstr) != -1)
            {
                res = true;
            }
            return res;
        }
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
        public TChild AutoCopy<TParent, TChild>(TParent parent) where TChild : TParent, new()
        {
            TChild child = new TChild();
            var ParentType = typeof(TParent);
            var Properties = ParentType.GetProperties();
            foreach (var Propertie in Properties)
            {
                //循环遍历属性
                if (Propertie.CanRead && Propertie.CanWrite)
                {
                    //进行属性拷贝
                    Propertie.SetValue(child, Propertie.GetValue(parent, null), null);
                }
            }
            var fs = ParentType.GetFields();
            foreach (var fie in fs)
            {
                //循环遍历属性
                fie.SetValue(child, fie.GetValue(parent));
            }
            return child;
        }
        public TChild AutoCopy<TParent, TChild>(TParent parent,TChild child) where TChild : TParent, new()
        {
            var ParentType = typeof(TParent);
            var Properties = ParentType.GetProperties();
            foreach (var Propertie in Properties)
            {
                //循环遍历属性
                if (Propertie.CanRead && Propertie.CanWrite)
                {
                    //进行属性拷贝
                    Propertie.SetValue(child, Propertie.GetValue(parent, null), null);
                }
            }
            return child;
        }
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
                        "yyyyMMdd","yyyy-MM-dd","yyyy-MM",
                        "yyyy/M/d tt hh:mm:ss",
                        "yyyy/MM/dd tt hh:mm:ss",
                        "yyyy/MM/dd HH:mm:ss",
                        "yyyy/M/d HH:mm:ss",
                        "yyyy/M/d",  "yyyy/MM/dd"
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
                                rdate = DateTime.Parse("1900-01-01 00:00:00.000").AddDays(nume - 2);
                                suc = true;
                            }
                        }
                    }
                    if (suc)
                    {
                        resF = rdate;
                        res = "'" + rdate.ToShortDateString() + "'";
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


        public bool compareValue(string colType, string valueLeft, string valueRight)
        {
            bool issame = false;
            try
            {
                if (colType == "System.Boolean")
                {
                    bool aok = true, bok = true;
                    var valA = this.parsemyBool(valueLeft, out aok);
                    var valB = this.parsemyBool(valueRight, out bok);
                    issame = aok == true && bok == true && valA == valB;
                }
                else if (colType == "System.DateTime")
                {
                    issame = DateTime.Compare(DateTime.Parse(valueLeft), DateTime.Parse(valueRight)) == 0;
                }
                else if (colType == "System.Guid")
                {
                    issame = valueLeft.ToLower() == valueRight.ToLower();
                }
                else
                {
                    return valueLeft == valueRight;
                }
            }
            catch (Exception e)
            {
                issame = false;
            }
            return issame;
        }

        /// <summary>
        /// 返回2个值是否一样。
        /// </summary>
        /// <param name="type"></param>
        /// <param name="valueLeft"></param>
        /// <param name="valueRight"></param>
        /// <returns></returns>
        public bool compareValue(Type type, Object valueLeft, Object valueRight)
        {
            bool issame = false;
            if (valueLeft == null && valueRight == null) return true;
            if (valueRight == DBNull.Value && valueLeft == DBNull.Value) return true;
            var leftstr = valueLeft.ToString();
            var rightstr = valueRight.ToString();
            if (leftstr.Trim() == rightstr.Trim()) return true;
            try
            {
                if (type.Name == "Boolean")
                {
                    bool aok = true, bok = true;
                    var valA = this.parsemyBool(leftstr, out aok);
                    var valB = this.parsemyBool(rightstr, out bok);
                    issame = aok == true && bok == true && valA == valB;
                }
                else if (type.Name == "DateTime")
                {
                    if (isEmptyDateTime(leftstr) && isEmptyDateTime(rightstr)) return true;
                    issame = DateTime.Compare(DateTime.Parse(leftstr), DateTime.Parse(rightstr)) == 0;
                }
                else if (type.Name == "Guid")
                {
                    issame = leftstr.ToLower() == rightstr.ToLower();
                }
                else if (type.Name == "Decimal")
                {
                    //特殊处理，因为数据库中 True为1 False为0，所以如果存在布尔值，按此转换。
                    leftstr = SQLBoolToIntString(leftstr);
                    rightstr = SQLBoolToIntString(rightstr);
                    if (leftstr == rightstr) return true;
                    issame = Decimal.Compare(Decimal.Parse(leftstr), Decimal.Parse(rightstr)) == 0;
                }
                else
                {
                    return rightstr == leftstr;
                }
            }
            catch (Exception e)
            {
                issame = false;
            }
            return issame;
        }
        /// <summary>
        /// 是否是空日期，1900年初始日期视为空日期。
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public bool isEmptyDateTime(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return true;
            if (value == "1900/1/1 0:00:00") return true;
            return false;
        }
        public string SQLBoolToIntString(Object val)
        {
            var valstr = val.ToString();
            var trueStrs = new List<string> { "TRUE", "True", "true", "1" };
            var falseStrs = new List<string> { "FALSE", "False", "false", "0", "" };
            if (trueStrs.Contains(valstr))
            {
                return "1";
            }
            if (falseStrs.Contains(valstr))
            {
                return "0";
            }
            return valstr;

        }
        public bool parsemyBool(string val, out bool isOK)
        {
            isOK = true;
            var trueStrs = new List<string> { "TRUE", "True", "true", "1" };
            var falseStrs = new List<string> { "FALSE", "False", "false", "0", "" };
            if (trueStrs.Contains(val))
            {
                return true;
            }
            if (falseStrs.Contains(val))
            {
                return false;
            }
            isOK = false;
            return false;
        }
        public static Object shapeDataType(Object val, Type dataType)
        {
            var res = new Object();
            try
            {
                if (dataType.Name.Contains("Int") || dataType.Name == "Decimal")
                {
                    var valstr = val.ToString().Trim();
                    if (Regex.IsMatch(valstr, @"^[\d.]+$") == false)
                    {
                        return DBNull.Value;
                    }
                }
                switch (dataType.Name)
                {
                    case "String":
                        res = val.ToString();
                        break;
                    case "DateTime":
                        //res = Convert.ToDateTime(val);
                        DateTime dti;
                        if (ParseMyDateTime(val, out dti))
                        {
                            return dti;
                        }
                        res = DBNull.Value;
                        break;
                    case "Guid":
                        res = new Guid(val.ToString());
                        break;
                    case "Boolean":
                        var varStr = val.ToString();
                        if (varStr == "1" || varStr == "true" || varStr == "True")
                        {
                            res = true;
                        }
                        else if (varStr == "0" || varStr == "false" || varStr == "False")
                        {
                            res = false;
                        }
                        else
                        {
                            res = Convert.ToBoolean(val);
                        }
                        break;
                    case "Int32":
                        res = Convert.ToInt32(val);
                        break;
                    case "Decimal":
                        res = Decimal.Parse(val.ToString());
                        break;
                    default:
                        res = Convert.ChangeType(val, dataType);
                        break;
                }
            }
            catch
            {
                res = DBNull.Value;
            }
            if (res == null)
            {
                res = DBNull.Value;
            }
            return res;
        }
        /// <summary>
        /// 简化版日期解析，解析失败时返回初始日期1900-01-01 00:00:00.000
        /// </summary>
        /// <param name="val"></param>
        /// <returns></returns>
        public static DateTime ParseMyDateTime(object val)
        {
            DateTime res;
            if (ParseMyDateTime(val, out res))
            {
                return res;
            }
            res = DateTime.Parse("1900-01-01 00:00:00.000");
            return res;
        }
        /// <summary>
        /// 带是否成功标记的日期解析
        /// </summary>
        /// <param name="val"></param>
        /// <param name="isok"></param>
        /// <returns></returns>
        public static bool ParseMyDateTime(object val, out DateTime res)
        {
            res = new DateTime();
            if (val is DateTime)
            {
                res = (DateTime)val;
                return true;
            }
            try
            {
                res = Convert.ToDateTime(val);
                return true;
            }
            catch (Exception e)
            {
                DateTime rdate = new DateTime();

                var valstr = val.ToString();
                if (DateTime.TryParse(valstr, out rdate))
                {
                    res = rdate;
                    return true;
                }
                string[] dateformats = {
                            "yyyyMMdd","yyyy-MM-dd","yyyy-MM",
                            "yyyy/M/d tt hh:mm:ss",
                            "yyyy/MM/dd tt hh:mm:ss",
                            "yyyy/MM/ddhh:mm:ss",
                            "yyyy/MM/dd HH:mm:ss",
                            "yyyy/M/d HH:mm:ss",
                            "yyyy/M/d",  "yyyy/MM/dd"
                };
                if (DateTime.TryParseExact(valstr, dateformats, null, System.Globalization.DateTimeStyles.AllowWhiteSpaces, out rdate))
                {
                    res = rdate;
                    return true;
                }
                else if (System.Text.RegularExpressions.Regex.IsMatch(valstr, @"\d{1,6}$"))
                {
                    //依然不行时，尝试
                    double nume = 0;
                    if (double.TryParse(valstr, out nume))
                    {
                        rdate = DateTime.Parse("1900-01-01 00:00:00.000").AddDays(nume - 2);
                        res = rdate;
                        return true;
                    }
                }
                //此时转换失败

                res = DateTime.Parse("1900-01-01 00:00:00.000");
                return false;
            }

        }
    }
  
}
