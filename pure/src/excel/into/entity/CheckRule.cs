using System;
using System.Collections.Generic;

using System.Text.RegularExpressions;


namespace mooSQL.excel.context
{
    /// <summary>
    /// 对导入值进行格式校验
    /// </summary>
    public class CheckRule
    {
        /// <summary>
        /// 规则类型：op,func,val,reg
        /// </summary>
        public string type;
        public string oprate;
        public List<string> paras;
        /// <summary>
        /// 解析前的规则字符串
        /// </summary>
        public string srcStr;

        public bool check(string value, valueType colType)
        {
            bool ok = true;
            //使用内置函数的模式，由外界传入函数名，调用函数进行核查。
            try
            {
                if (type == "regx")
                {
                    return Regex.IsMatch(value, oprate);
                }
                else if (type == "op" && paras.Count > 0)
                {

                    switch (oprate)
                    {
                        case ">":
                            if (colType == valueType.number)
                            {
                                return double.Parse(value) > double.Parse(paras[0]);
                            }
                            else
                            {
                                return string.Compare(value, paras[0]) > 0;
                            }
                        case ">=":
                            if (colType == valueType.number)
                            {
                                return double.Parse(value) >= double.Parse(paras[0]);
                            }
                            else
                            {
                                return string.Compare(value, paras[0]) >= 0;
                            }
                        case "<":
                            if (colType == valueType.number)
                            {
                                return double.Parse(value) < double.Parse(paras[0]);
                            }
                            else
                            {
                                return string.Compare(value, paras[0]) < 0;
                            }
                        case "<=":
                            if (colType == valueType.number)
                            {
                                return double.Parse(value) <= double.Parse(paras[0]);
                            }
                            else
                            {
                                return string.Compare(value, paras[0]) <= 0;
                            }
                        case "!=":
                            if (colType == valueType.number)
                            {
                                return double.Parse(value) != double.Parse(paras[0]);
                            }
                            else
                            {
                                return string.Compare(value, paras[0]) != 0;
                            }
                        case "==":
                        case "=":
                            if (colType == valueType.number)
                            {
                                return double.Parse(value) == double.Parse(paras[0]);
                            }
                            else
                            {
                                return string.Compare(value, paras[0]) == 0;
                            }
                        default: return ok;

                    }
                }
                else if (type == "func")
                {
                    switch (oprate)
                    {
                        case "valid":
                            return string.IsNullOrWhiteSpace(value);
                        case "isDate":
                            DateTime tim;
                            return DateTime.TryParse(value, out tim);
                        case "isNumber":
                            double num;
                            return double.TryParse(value, out num);
                        default:
                            break;
                    }
                }
                else if (oprate == "notnull")
                {
                    //不为空
                    return string.IsNullOrWhiteSpace(value) == false;
                }
            }
            catch (Exception e)
            {
                ok = false;
            }
            return ok;
        }
        public bool parseFuncStr(string val)
        {
            bool canParse = true;
            if (string.IsNullOrWhiteSpace(val)) return false;
            oprate = "";
            paras = new List<string>();
            type = "none";
            //去除所有空格字符
            val = Regex.Replace(val, @"[\s]*", "");
            var simOpReg = new Regex(@"[><=]+");
            if (val.StartsWith("regx=", true, null))
            {
                type = "regx";
                oprate = val.Substring(5);
            }
            else if (simOpReg.IsMatch(val))
            {
                var op = simOpReg.Matches(val);
                foreach (Match m in op)
                {
                    oprate += m.Value;
                }
                var emptVal = simOpReg.Replace(val, "");

                paras.Add(emptVal);
                type = "op";
            }
            else if (val.Contains("(") && val.Contains(")"))
            {
                //尝试提取函数名模式
                var kuohao = new Regex(@"[(][^)]*[)]*");
                oprate = kuohao.Replace(val, "");
                //提取括号内的参数
                var kuohaoStr = kuohao.Match(val).Value;
                kuohaoStr = kuohaoStr.Replace("(", "");
                kuohaoStr = kuohaoStr.Replace(")", "");
                if (string.IsNullOrWhiteSpace(kuohaoStr) == false)
                {
                    if (kuohaoStr.Contains(","))
                    {
                        var pms = kuohaoStr.Split(',');
                        foreach (var p in pms)
                        {
                            paras.Add(p);
                        }
                    }
                    else
                    {
                        paras.Add(kuohaoStr);
                    }
                }
                type = "func";
            }
            else
            {
                oprate = val;
            }

            //\w*[(][^)]*[)]
            return canParse;
        }
    }
}
