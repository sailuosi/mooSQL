
using System.Collections.Generic;

using System.Text.RegularExpressions;



namespace mooSQL.excel.context
{
    /// <summary>
    /// 校验规则集合
    /// </summary>
    public class RuleCollection
    {
        /// <summary>
        /// 校验规则
        /// </summary>
        public RuleCollection()
        {

        }
        /// <summary>
        /// 校验规则明细
        /// </summary>
        public List<CheckRule> rules = new List<CheckRule>();
        /// <summary>
        /// 分隔符
        /// </summary>
        public string seprator = "&&";
        /// <summary>
        /// 执行校验
        /// </summary>
        /// <param name="value"></param>
        /// <param name="colType"></param>
        /// <returns></returns>
        public bool check(string value, valueType colType)
        {
            if (value == null) return false;
            bool can = false;
            if (rules.Count == 0) return true;
            foreach (var ru in rules)
            {
                var rucan = ru.check(value, colType);
                //短路原则，一旦全与，或一个为真。
                if (rucan == false && seprator == "&&")
                {
                    return false;
                }
                else if (rucan == true && seprator == "||")
                {
                    return true;
                }
            }
            return true;
        }
        /// <summary>
        /// 读取配置
        /// </summary>
        /// <param name="configStr"></param>
        public void readConfig(string configStr)
        {

            var rule = Regex.Replace(configStr, @"[\s]", "");
            if (string.IsNullOrWhiteSpace(rule) == false)
            {
                //格式  多个条件 并的用 && 或的用 ||
                if (rule.Contains("&&") || rule.Contains("||"))
                {
                    var ruleArr = Regex.Split(rule, @"&&|\|\|");
                    foreach (var ru in ruleArr)
                    {
                        var prule = new CheckRule();
                        var canp = prule.parseFuncStr(ru);
                        if (canp)
                        {
                            rules.Add(prule);
                        }
                    }
                }
                else
                {
                    var prule = new CheckRule();
                    var canp = prule.parseFuncStr(rule);
                    if (canp)
                    {
                        rules.Add(prule);
                    }
                }
            }

        }
    }
}
