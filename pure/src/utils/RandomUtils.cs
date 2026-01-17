using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.utils
{
    public class RandomUtils
    {
        /// <summary>
        /// 生成一个包含字母和数字的随机字符串
        /// </summary>
        /// <param name="length"></param>
        /// <returns></returns>
         public static string NextString(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            var random = new Random();
            var sb = new StringBuilder(length);

            for (int i = 0; i < length; i++)
            {
                sb.Append(chars[random.Next(chars.Length)]);
            }

            return sb.ToString();
        }
    }
}
