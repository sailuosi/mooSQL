using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace mooSQL.excel
{
    /// <summary>
    /// Excel工具类，提供一些Excel操作的辅助方法
    /// </summary>
    public static class ExcelUntil
    {
        /// <summary>
        /// 将Excel列号转换为数字
        /// </summary>
        /// <param name="i"></param>
        /// <returns></returns>
        public static string getExcelColCode(int i)
        {
            var colCode = new List<string> { "A", "B", "C", "D", "E", "F", "G", "H", "I", "J", "K", "L", "M", "N", "O", "P", "Q", "R", "S", "T", "U", "V", "W", "X", "Y", "Z" };
            var res = "";
            if (i < 0) return res;
            var m = colCode.Count;
            while (i >= 0)
            {
                if (i < m)
                {
                    res = colCode[i] + res;
                    i -= m;
                }
                else
                {
                    //
                    var n = i % m;
                    res = colCode[n] + res;
                    i = i / m;
                    i--;
                }
            }
            return res;
        }

        /// <summary>
        /// 检查某个列号如AA，是否在形如A,A-H,H-这样的范围字符串的定义范围中
        /// </summary>
        /// <param name="range"></param>
        /// <param name="code"></param>
        /// <returns></returns>
        public static bool checkInColRange(string range, string code)
        {
            var myi = parCellColCode(code);
            if (string.IsNullOrWhiteSpace(range)) return false;
            if (range.Contains(","))
            {
                var ranges = range.Split(',');
                foreach (var ran in ranges)
                {
                    if (CheckInRangeInner(ran, myi)) return true;
                }
            }
            else
            {
                if (CheckInRangeInner(range, myi)) return true;
            }
            return false;
        }
        private static bool CheckInRangeInner(string range, int colIndex)
        {
            var ranges = range.Split(',');
            foreach (var ran in ranges)
            {
                if (ran.Contains("-"))
                {
                    int min = -1;
                    int max = -1;
                    parseColRange(ran, out min, out max);
                    if (colIndex >= min)
                    {
                        if (max == -1 || colIndex <= max) { return true; }
                    }
                }
                else
                {
                    var c = parCellColCode(ran);
                    if (c != -1 && colIndex == c) return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 当输出值为-1时表示未设置范围。
        /// </summary>
        /// <param name="rangeCode"></param>
        /// <param name="minIndex"></param>
        /// <param name="maxIndex"></param>
        /// <returns></returns>
        public static bool parseColRange(string rangeCode, out int minIndex, out int maxIndex)
        {
            minIndex = -1;
            maxIndex = -1;
            var rs = rangeCode.Split('-');
            if (rs.Length > 0)
            {
                minIndex = parCellColCode(rs[0]);
            }
            if (rs.Length > 1)
            {
                maxIndex = parCellColCode(rs[1]);
            }

            return minIndex != -1 || maxIndex != -1;
        }

        //解析形如A2这样的位置参数 TODO
        /// <summary>
        /// 解析形如A2这样的位置参数
        /// </summary>
        /// <param name="code"></param>
        /// <param name="rowIndex"></param>
        /// <param name="colIndex"></param>
        /// <returns></returns>
        public static bool parseCellCode(string code, out int rowIndex, out int colIndex)
        {
            bool gotr = false;
            bool gotc = false;
            rowIndex = 0;
            colIndex = 0;
            var colReg = new Regex(@"[A-Z]{1,}");
            var rowReg = new Regex(@"\d{1,}");
            var colcode = colReg.Match(code);
            if (colcode.Success)
            {
                colIndex = parCellColCode(colcode.Value);
                if (colIndex > -1) gotc = true;
            }
            var rownum = rowReg.Match(code);
            if (rownum.Success && int.TryParse(rownum.Value, out rowIndex))
            {
                rowIndex = rowIndex - 1;
                if (rowIndex > -1) gotr = true;
            }

            return gotr && gotc;
        }
        /// <summary>
        /// 将类似A这样的列号，解析为1；
        /// </summary>
        /// <param name="code"></param>
        /// <returns></returns>
        public static int parCellColCode(string code)
        {
            var res = -1;
            try
            {
                if (!Regex.IsMatch(code.ToUpper(), @"[A-Z]+")) { return -1; }
                int index = 0;
                char[] chars = code.ToUpper().ToCharArray();
                for (int i = 0; i < chars.Length; i++)
                {
                    index += ((int)chars[i] - (int)'A' + 1) * (int)Math.Pow(26, chars.Length - i - 1);
                }
                res = index - 1;
            }
            catch (Exception e)
            {
                res = -1;
            }
            return res;
        }
    }
}
