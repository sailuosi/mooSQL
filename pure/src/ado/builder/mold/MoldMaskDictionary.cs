using System.Collections.Generic;
using System.Text;

namespace mooSQL.data
{
    /// <summary>
    /// L1 结构掩码字典（有序）；不包含运行时参数值。含 MaskBits=0 的跳过接点。
    /// </summary>
    public sealed class MoldMaskDictionary
    {
        readonly List<string> _entries = new List<string>();

        /// <summary>已登记条目数。</summary>
        public int Count => _entries.Count;

        /// <summary>追加一条掩码项。</summary>
        public void Add(string entry)
        {
            if (!string.IsNullOrEmpty(entry))
                _entries.Add(entry);
        }

        /// <summary>跳过（MaskBits=0）。</summary>
        public void AddSkip(string field, string op)
        {
            Add("M0|" + (field ?? "") + "|" + (op ?? "="));
        }

        /// <summary>标量纳入（MaskBits=1）。</summary>
        public void AddScalar(string field, string op)
        {
            Add("M1|" + (field ?? "") + "|" + (op ?? "="));
        }

        /// <summary>whereIn / whereNotIn（MaskBits=2）。</summary>
        public void AddIn(string field, string op, int arity, int chunks)
        {
            Add("M2|" + (field ?? "") + "|" + (op ?? "in") + "|" + arity + "|" + chunks);
        }

        /// <summary>Format 槽（MaskBits=3）：模板指纹 + Present 串。</summary>
        public void AddFormat(string kind, string templateFingerprint, string presentBits)
        {
            Add("M3|" + (kind ?? "fmt") + "|" + (templateFingerprint ?? "") + "|" + (presentBits ?? ""));
        }

        /// <summary>清空。</summary>
        public void Clear()
        {
            _entries.Clear();
        }

        /// <summary>折叠为稳定指纹字符串。</summary>
        public string Fingerprint()
        {
            if (_entries.Count == 0)
                return "";
            var sb = new StringBuilder(_entries.Count * 16);
            for (var i = 0; i < _entries.Count; i++)
            {
                if (i > 0) sb.Append('\u001f');
                sb.Append(_entries[i]);
            }
            return sb.ToString();
        }
    }
}
