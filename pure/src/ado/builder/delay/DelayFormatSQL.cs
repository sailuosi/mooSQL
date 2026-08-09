namespace mooSQL.data
{
    /// <summary>
    /// selectFormat / fromFormat / joinFormat 延迟体：对齐 <see cref="Paras.formatSQL"/>（#{psfmt_…} + AddRaw）。
    /// </summary>
    public sealed class DelayFormatSQL : DelayParaBase
    {
        private readonly string _template;
        private readonly object[] _values;

        public DelayFormatSQL(string template, object[] values)
        {
            _template = template;
            _values = values ?? new object[0];
        }

        protected override string RunCore()
        {
            if (Owner == null)
                return _template ?? "";
            return Owner.formatSQL(_template, _values);
        }
    }
}
