using System;
using System.Collections.Generic;
using System.Text;

namespace mooSQL.data
{
    /// <summary>
    /// whereIn / whereNotIn / whereList 延迟体：复用 <see cref="WhereListBag"/> 分类与分批；
    /// 空 IN → <c>1=2</c>；空 NOT IN → <c>1=1</c>（P5）；unsafe 值在 Run 时写入 Owner。
    /// </summary>
    public sealed class DelayWhereIn : DelayParaBase
    {
        private readonly string _key;
        private readonly string _op;
        private readonly Func<WhereListBag> _makeBag;
        private readonly string _dbstr;
        private readonly string _paraKey;
        private readonly int? _limit;

        public DelayWhereIn(
            string key,
            string op,
            Func<WhereListBag> makeBag,
            string dbstr,
            string paraKey,
            int? limit)
        {
            _key = key;
            _op = op ?? " IN ";
            _makeBag = makeBag;
            _dbstr = dbstr ?? "";
            _paraKey = paraKey ?? "";
            _limit = limit;
        }

        private bool IsNotIn
        {
            get
            {
                var o = (_op ?? "").Trim();
                return o.Equals("NOT IN", StringComparison.OrdinalIgnoreCase);
            }
        }

        protected override string RunCore()
        {
            if (_makeBag == null)
                return EmptyLiteral();

            var bag = _makeBag();
            if (bag == null)
                return EmptyLiteral();

            bag.field = _key;
            bag.op = _op;

            // 显式空袋：避免异常路径把「无元素」误拼成 NOT IN (@p0)
            int bagCount = (bag.numValues != null ? bag.numValues.Count : 0)
                + (bag.safedStrValues != null ? bag.safedStrValues.Count : 0)
                + (bag.unSafeValues != null ? bag.unSafeValues.Count : 0);
            if (bagCount == 0)
                return EmptyLiteral();

            var names = AddUnsafeParas(bag);
            var parts = bag.toWhereIn(names, _limit);
            if (parts == null || parts.Count == 0)
                return EmptyLiteral();

            if (parts.Count == 1)
                return _key + _op + "(" + parts[0] + ")";

            // 分批：折叠为单一条件，等价于 sinkOR + 多段 whereIn
            var sb = new StringBuilder();
            sb.Append("(");
            for (int i = 0; i < parts.Count; i++)
            {
                if (i > 0)
                    sb.Append(" OR ");
                sb.Append(_key);
                sb.Append(_op);
                sb.Append("(");
                sb.Append(parts[i]);
                sb.Append(")");
            }
            sb.Append(")");
            return sb.ToString();
        }

        private List<string> AddUnsafeParas(WhereListBag bag)
        {
            var names = new List<string>();
            if (bag.unSafeValues == null || bag.unSafeValues.Count == 0)
                return names;

            var ps = Owner;
            int i = 0;
            foreach (var item in bag.unSafeValues)
            {
                string name = _paraKey + i;
                names.Add(_dbstr + name);
                if (ps != null)
                    ps.AddByPrefix(name, item, _dbstr);
                i++;
            }
            return names;
        }

        private string EmptyLiteral()
        {
            return IsNotIn ? "1=1" : "1=2";
        }
    }
}
