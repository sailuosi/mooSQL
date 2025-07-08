
using System.Collections.Generic;

using System.Text.RegularExpressions;


namespace mooSQL.utils
{
    /// <summary>
    /// 正则工具
    /// </summary>
    public class RegxUntils
    {
        /// <summary>
        /// 对字符串执行正则校验。
        /// </summary>
        /// <param name="value"></param>
        /// <param name="regx"></param>
        /// <returns></returns>
        public static bool test(string value, string regx)
        {
            if (string.IsNullOrWhiteSpace(value) || string.IsNullOrWhiteSpace(regx)) return false;
            return Regex.IsMatch(value, regx, RegexOptions.IgnoreCase);
        }
        /// <summary>
        /// 验证
        /// </summary>
        /// <param name="value"></param>
        /// <param name="regx"></param>
        /// <returns></returns>
        public static bool test(string value, Regex regx)
        {
            if (string.IsNullOrWhiteSpace(value) || regx == null) return false;
            return regx.IsMatch(value);
        }
        /// <summary>
        /// 严格的身份证正则，包含起止符号。
        /// </summary>
        public static string idcardReg = @"(^[1-9]\d{5}(18|19|([23]\d))\d{2}((0[1-9])|(10|11|12))(([0-2][1-9])|10|20|30|31)\d{3}[0-9Xx]$)|(^[1-9]\d{5}\d{2}((0[1-9])|(10|11|12))(([0-2][1-9])|10|20|30|31)\d{2}[0-9Xx]$)";
        /// <summary>
        /// 手机号正则，限制为11位数字
        /// </summary>
        public static string phoneReg = @"^\d{11}$";
        /// <summary>
        /// 严格的GUID正则
        /// </summary>
        public static string guidReg = @"^[0-9a-fA-F]{8}(-[0-9a-fA-F]{4}){3}-[0-9a-fA-F]{12}$";
        /// <summary>
        /// 严格的身份证校验，非包含。
        /// </summary>
        /// <param name="val"></param>
        /// <returns></returns>
        public static bool isIdcard(string val)
        {
            return test(val, idcardReg);
        }
        /// <summary>
        /// 手机号校验，默认为11位数字
        /// </summary>
        /// <param name="val"></param>
        /// <returns></returns>
        public static bool isPhone(string val)
        {
            return test(val, phoneReg);
        }

        /// <summary>
        /// 核查是否是严格的guid。
        /// </summary>
        /// <param name="val"></param>
        /// <returns></returns>
        public static bool isGUID(string val)
        {
            return test(val, guidReg);
        }

        /// <summary>
        /// 检查是否是以“,”连接起来的一组guid。
        /// </summary>
        /// <param name="val"></param>
        /// <returns></returns>
        public static bool isGUIDs(string val)
        {
            if (string.IsNullOrWhiteSpace(val)) return false;
            return Regex.IsMatch(val, @"^([0-9a-fA-F]{8}(-[0-9a-fA-F]{4}){3}-[0-9a-fA-F]{12}[,]?)+$", RegexOptions.IgnoreCase);
        }
        /// <summary>
        /// 将字符串拼接的GUID，转换为SQL语句where条件的 where in ({0})中的内容。转换失败时，返回null
        /// </summary>
        /// <param name="val"></param>
        /// <returns></returns>
        public static string GUIDStrToWhere(string val)
        {
            if (isGUID(val)) return "'" + val + "'";
            if (isGUIDs(val))
            {

                var oids = new List<string>();
                var valarr = val.Split(',');
                foreach (var v in valarr)
                {
                    if (isGUID(v) && !oids.Contains(v)) oids.Add(v);
                }
                if (oids.Count > 0)
                {
                    return string.Format("'{0}'", string.Join("','", oids));
                }
            }
            return null;
        }
        /// <summary>
        /// 核验是否是带符号或小数点的数字^[+-]?/d*[.]?/d*$
        /// </summary>
        /// <param name="val"></param>
        /// <returns></returns>
        public static bool IsNumeric(string val)
        {
            if (string.IsNullOrWhiteSpace(val)) return false;
            return Regex.IsMatch(val, @"^[+-]?[0-9]*[.]?[0-9]*$");
        }
        /// <summary>
        /// 核验是否是整数
        /// </summary>
        /// <param name="val"></param>
        /// <returns></returns>
        public static bool IsInt(string val)
        {
            if (string.IsNullOrWhiteSpace(val)) return false;
            return Regex.IsMatch(val, @"^[+-]?\d*$");
        }
        /// <summary>
        /// 核验是是否是数字^/d*[.]?/d*$
        /// </summary>
        /// <param name="val"></param>
        /// <returns></returns>
        public static bool IsUnsign(string val)
        {
            if (string.IsNullOrWhiteSpace(val)) return false;
            return Regex.IsMatch(val, @"^\d*[.]?\d*$");
        }
        /// <summary>
        /// 由0-9A-Za-z以及下划线组成的简单字符串
        /// </summary>
        /// <param name="val"></param>
        /// <returns></returns>
        public static bool IsSimpleString(string val)
        {
            if (string.IsNullOrWhiteSpace(val)) return false;
            return Regex.IsMatch(val, @"^[0-9a-zA-Z_.-]+$");
        }

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
        /// <summary>
        /// 是否为数值字母下划线组成的字符串。
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static bool isPureSimpleStr(string str)
        {
            //核验是否安全
            //简单纯粹的字符串，由数字、字母的构成的无特殊符号字符串
            var simpleReg = new Regex("^[0-9a-zA-Z_]+$");
            if (simpleReg.IsMatch(str)) { return true; }
            return false;
        }
    }
}
