namespace mooSQL.data
{
    /// <summary>
    /// whereFormat 延迟体：模板 {i} → 参数名 / null，Run 时写入 Owner Paras。
    /// </summary>
    public sealed class DelayWhereFormat : DelayParaBase
    {
        private readonly string _template;
        private readonly object[] _values;
        private readonly string _dbstr;
        private readonly string _prefixKey;

        /// <param name="template">含 {0} 的模板。</param>
        /// <param name="values">槽位值。</param>
        /// <param name="dbstr">方言参数前缀（如 @）。</param>
        /// <param name="prefixKey">Apply 时捕获的 getMyPrefixKey()。</param>
        public DelayWhereFormat(string template, object[] values, string dbstr, string prefixKey)
        {
            _template = template;
            _values = values ?? new object[0];
            _dbstr = dbstr ?? "";
            _prefixKey = prefixKey ?? "";
        }

        protected override string RunCore()
        {
            var key = _template ?? "";
            var ps = Owner;
            for (int i = 0; i < _values.Length; i++)
            {
                string reg = "{" + i + "}";
                var v = _values[i];
                if (v == null)
                {
                    key = key.Replace(reg, " null ");
                }
                else
                {
                    int count = ps != null ? ps.Count : 0;
                    string ke = _prefixKey + "wf_" + count + "_" + i;
                    key = key.Replace(reg, _dbstr + ke);
                    if (ps != null)
                        ps.AddByPrefix(ke, v, _dbstr);
                }
            }
            return key;
        }
    }
}
