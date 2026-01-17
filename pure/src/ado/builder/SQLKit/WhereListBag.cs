// 基础功能说明：


using mooSQL.utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace mooSQL.data
{
    /// <summary>
    /// SQL语句中的 whereIn条件集合
    /// </summary>
    public class WhereListBag
    {
        /// <summary>
        /// 当数值集合时，数字在拼入SQL时不会作为字符串处理
        /// </summary>
        public bool isNumberBag=false;

        /// <summary>
        /// 字段名
        /// </summary>
        public string field;
        /// <summary>
        /// 操作符
        /// </summary>
        public string op;
        /// <summary>
        /// 未处理的值集合
        /// </summary>
        public HashSet<string> values;
        /// <summary>
        /// 数值型的。
        /// </summary>
        public HashSet<string> numValues;
        /// <summary>
        /// 无风险的值列表
        /// </summary>
        public HashSet<string> safedStrValues;
        /// <summary>
        /// 有风险的值列表
        /// </summary>
        public HashSet<string> unSafeValues;

        public WhereListBag() { 
            this.values = new HashSet<string>();
            this.numValues = new HashSet<string>();
            this.safedStrValues = new HashSet<string>();
            this.unSafeValues = new HashSet<string>();
        }
        /// <summary>
        /// 字段类型
        /// </summary>
        public string dataType;

        public static WhereListBag newBag<T>(IEnumerable<T> list) { 
            var bag = new WhereListBag();
            var t = typeof(T);
            t = t.UnwrapNullable();
            if (t == typeof(int) || t == typeof(float) || t == typeof(double) || t == typeof(long) || t == typeof(decimal) || t == typeof(short) || t == typeof(byte) || t == typeof(sbyte) || t == typeof(ushort) || t == typeof(uint) || t == typeof(ulong)) {
                bag.isNumberBag = true;
            }
            bag.addValues(list);
            return bag;
        }

        public static WhereListBag newBag(IEnumerable list)
        {
            var bag = new WhereListBag();
            bool isAllNum=true;
            foreach (var v in list) {
                var t = v.GetType();
                if (isAllNum && !(t == typeof(int) || t == typeof(float) || t == typeof(double)))
                {
                    //有一个不是数值型，则都不按数值型处理
                    isAllNum = false;
                }
                bag.addValue(v);
            }
            if (isAllNum) { 
                bag.isNumberBag= true;
            }
            return bag;
        }

        /// <summary>
        /// 添加一组参数
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        /// <returns></returns>

        public int addValues<T>(IEnumerable<T> list) {
            int cc = 0;
            if (typeof(T) == typeof(string)) {
                foreach (var v in list) { 
                    var str=v as string;
                    cc+=addStrValue(str);
                }
                return cc;
            }

            
            var realType = typeof(T);
            var isNum = realType == typeof(Double) || realType == typeof(float) || realType == typeof(int);

            var isSafeStr= realType == typeof(Guid) || realType == typeof(Boolean);

            foreach (var item in list)
            {
                if (item == null) {
                    continue;
                }
                if (isNum) {
                    if (numValues.Add(item.ToString())) {
                        cc++;
                    }
                }
                var str = item.ToString();
                if (isSafeStr) { 
                    
                    if (string.IsNullOrWhiteSpace(str)) continue;
                    if (safedStrValues.Add(str)) { 
                        cc++;
                    }
                    continue;
                }
                //普通文本字符串
                cc += addStrValue(str);
                
            }
            return cc;
        }
        public int addValue<T>(T item)
        {

            var cc = 0;
            var realType = typeof(T);
            var isNum = realType == typeof(Double) || realType == typeof(float) || realType == typeof(int);

            var isSafeStr = realType == typeof(Guid) || realType == typeof(Boolean);

            
            if (item == null)
            {
                return cc; 
            }
            if (isNum)
            {
                if (numValues.Add(item.ToString()))
                {
                    cc++;
                }
            }
            var str = item.ToString();
            if (isSafeStr)
            {

                if (string.IsNullOrWhiteSpace(str)) return cc; 
                if (safedStrValues.Add(str))
                {
                    cc++;
                }
                return cc;
            }
            //普通文本字符串
            cc += addStrValue(str);

            
            return cc;
        }
        public int addStrValues(IEnumerable<string> list)
        {
            int cc = 0;
            foreach (var item in list)
            {
                cc += addStrValue(item);
            }
            return cc;
        }
        //存在BUG，停用此写法，改泛型写法
        //public int addValue(Object item) {
        //    if (item == null)
        //    {
        //        return 0;
        //    }

        //    var isNum = item is Double || item is float || item is  int;



        //    if (isNum)
        //    {
        //        if (numValues.Add(item.ToString()))
        //        {
        //            return 1;
        //        }
        //        return 0;
        //    }
        //    var isSafeStr = item is Guid || item is Boolean ||item is DateTime;
        //    var str = item.ToString();
            
        //    if (isSafeStr)
        //    {

        //        addStrValue(str);
        //        return 1;
        //    }
        //    if (item is string v) {
        //        addStrValue(v);
        //    }
        //    return 0;
        //}

        public int addStrValue(string str) {
            if (string.IsNullOrWhiteSpace(str)) return 0;
            if (isNum(str))
            {
                return numValues.Add(str) ? 1 : 0;
            }

            if (isSafeStr(str)||RegxUntils.isGUID(str))
            {
                return safedStrValues.Add(str) ? 1 : 0;
            }
            return unSafeValues.Add(str)?1:0;
            
        }


        private bool isSafeStr(string str) {
            //核验是否安全
            //简单纯粹的字符串，由数字、字母的构成的无特殊符号字符串
            var simpleReg = new Regex("^[0-9a-zA-Z._]+$");
            if(simpleReg.IsMatch(str)) { return true; }
            return false;
        }

        private bool isNum(string val) {
            return Regex.IsMatch(val, @"^[+-]?[0-9]*[.]?[0-9]*$");
        }

        /// <summary>
        /// 返回逗号拼接的结果，不含括号
        /// </summary>
        /// <param name="unSafeParaedNames"></param>
        /// <returns></returns>
        public string toWhereIn(IEnumerable<string> unSafeParaedNames) { 
            
            var vals= new List<string>();
            if (isNumberBag) {
            vals.AddRange(numValues);
            }
            else
            {
                foreach (var v in numValues)
                {
                    vals.Add("'" + v + "'");
                }
            }

            foreach (var v in safedStrValues) { 
                vals.Add("'"+v+"'");
            }
            foreach (var v in unSafeParaedNames)
            {
                vals.Add( v );
            }
            return string.Join(",", vals);
        }
    }
}