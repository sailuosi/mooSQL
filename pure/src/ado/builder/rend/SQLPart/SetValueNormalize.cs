using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace mooSQL.data
{
    /// <summary>
    /// set 糖方法的值规范化（限范围：Vector → JSON 数组文本）。
    /// </summary>
    internal static class SetValueNormalize
    {
        /// <summary>
        /// <c>float[]</c> / <see cref="IList{T}"/> of float → JSON 数组字符串；已是 string 则原样。
        /// </summary>
        public static object ForVector(object val)
        {
            if (val == null || val == DBNull.Value)
                return val;
            if (val is string)
                return val;
            if (val is float[] floats)
                return FormatFloatArray(floats);
            if (val is IList<float> list)
            {
                var arr = new float[list.Count];
                for (int i = 0; i < list.Count; i++)
                    arr[i] = list[i];
                return FormatFloatArray(arr);
            }
            return val;
        }

        static string FormatFloatArray(float[] floats)
        {
            var sb = new StringBuilder(floats.Length * 8 + 2);
            sb.Append('[');
            for (int i = 0; i < floats.Length; i++)
            {
                if (i > 0) sb.Append(',');
                sb.Append(floats[i].ToString(CultureInfo.InvariantCulture));
            }
            sb.Append(']');
            return sb.ToString();
        }
    }
}
