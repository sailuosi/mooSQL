using System;
using System.Collections.Generic;
using System.Text;
using mooSQL.data.mapping;
using mooSQL.utils;

namespace mooSQL.data
{
    /// <summary>
    /// whereInGuid 延迟体：Guid 安全内联（走方言 <see cref="MappingPanel.valueToSQLGuid(Guid)"/>）。
    /// 空/无有效元素 → 1=2（P5）。
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
        private readonly MappingPanel _mapping;

        public DelayWhereInGuid(string key, IEnumerable<Guid> oids, MappingPanel mapping)
        {
            _key = key;
            _mode = Mode.Guid;
            _guids = oids;
            _mapping = mapping;
        }

        public DelayWhereInGuid(string key, IEnumerable<Guid?> oids, MappingPanel mapping)
        {
            _key = key;
            _mode = Mode.GuidNullable;
            _guidNullables = oids;
            _mapping = mapping;
        }

        public DelayWhereInGuid(string key, IEnumerable<string> oids, MappingPanel mapping)
        {
            _key = key;
            _mode = Mode.StringGuid;
            _strings = oids;
            _mapping = mapping;
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
                        AppendLiteral(res, ref cc, _mapping.valueToSQLGuid(oid));
                }
            }
            else if (_mode == Mode.GuidNullable)
            {
                if (_guidNullables != null)
                {
                    foreach (var oid in _guidNullables)
                    {
                        if (oid != null)
                            AppendLiteral(res, ref cc, _mapping.valueToSQLGuid(oid.Value));
                    }
                }
            }
            else if (_strings != null)
            {
                foreach (var oid in _strings)
                {
                    if (RegxUntils.isGUID(oid))
                        AppendLiteral(res, ref cc, _mapping.valueToSQLGuid(oid));
                }
            }

            res.Append(")");
            if (cc == 0)
                return "1=2";
            return _key + " IN " + res.ToString();
        }

        private static void AppendLiteral(StringBuilder res, ref int cc, string literal)
        {
            if (cc > 0)
                res.Append(",");
            res.Append(literal);
            cc++;
        }
    }
}
