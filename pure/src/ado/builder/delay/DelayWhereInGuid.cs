using System;
using System.Collections.Generic;
using System.Text;
using mooSQL.utils;

namespace mooSQL.data
{
    /// <summary>
    /// whereInGuid 延迟体：Guid 安全内联，不做参数化（P4）；空/无有效元素 → 1=2（P5）。
    /// </summary>
    public sealed class DelayWhereInGuid : DelayParaBase
    {
        private enum Mode
        {
            Guid,
            GuidNullable,
            StringGuid
        }

        private readonly string _key;
        private readonly Mode _mode;
        private readonly IEnumerable<Guid> _guids;
        private readonly IEnumerable<Guid?> _guidNullables;
        private readonly IEnumerable<string> _strings;

        public DelayWhereInGuid(string key, IEnumerable<Guid> oids)
        {
            _key = key;
            _mode = Mode.Guid;
            _guids = oids;
        }

        public DelayWhereInGuid(string key, IEnumerable<Guid?> oids)
        {
            _key = key;
            _mode = Mode.GuidNullable;
            _guidNullables = oids;
        }

        public DelayWhereInGuid(string key, IEnumerable<string> oids)
        {
            _key = key;
            _mode = Mode.StringGuid;
            _strings = oids;
        }

        protected override string RunCore()
        {
            var res = new StringBuilder();
            int cc = 0;
            res.Append("(");

            if (_mode == Mode.Guid)
            {
                if (_guids != null)
                {
                    foreach (var oid in _guids)
                    {
                        AppendQuoted(res, ref cc, oid.ToString());
                    }
                }
            }
            else if (_mode == Mode.GuidNullable)
            {
                if (_guidNullables != null)
                {
                    foreach (var oid in _guidNullables)
                    {
                        if (oid != null)
                            AppendQuoted(res, ref cc, oid.ToString());
                    }
                }
            }
            else if (_strings != null)
            {
                foreach (var oid in _strings)
                {
                    if (RegxUntils.isGUID(oid))
                        AppendQuoted(res, ref cc, oid);
                }
            }

            res.Append(")");
            if (cc == 0)
                return "1=2";
            return _key + " IN " + res.ToString();
        }

        private static void AppendQuoted(StringBuilder res, ref int cc, string text)
        {
            if (cc > 0)
                res.Append(",");
            res.Append("'");
            res.Append(text);
            res.Append("'");
            cc++;
        }
    }
}
