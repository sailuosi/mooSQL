using System;
using System.Collections.Generic;

namespace mooSQL.data
{
    /// <summary>from (select…) as alias —— 存子步骤，不存 Action。</summary>
    public sealed class FromSubqueryStep : IStep
    {
        private readonly string _asName;
        private readonly IReadOnlyList<IStep> _childSteps;

        public FromSubqueryStep(string asName, IReadOnlyList<IStep> childSteps)
        {
            _asName = asName;
            _childSteps = childSteps ?? Array.Empty<IStep>();
        }

        public void Apply(SQLBuilder builder)
        {
            var bro = builder.Inner.getBrotherBuilder();
            SQLBuilder.ReplaySteps(bro, _childSteps);
            var sql = bro.toSelect().sql;
            builder.Inner.current.from(string.Format("({0}) as {1} ", sql, _asName));
        }
    }

    /// <summary>JOIN (select…) as alias</summary>
    public sealed class JoinSubqueryStep : IStep
    {
        private readonly string _joinKey;
        private readonly string _asName;
        private readonly IReadOnlyList<IStep> _childSteps;

        public JoinSubqueryStep(string joinKey, string asName, IReadOnlyList<IStep> childSteps)
        {
            _joinKey = joinKey;
            _asName = asName;
            _childSteps = childSteps ?? Array.Empty<IStep>();
        }

        public void Apply(SQLBuilder builder)
        {
            var bro = builder.Inner.getBrotherBuilder();
            SQLBuilder.ReplaySteps(bro, _childSteps);
            var sql = bro.toSelect().sql;
            builder.Inner.current.fromAppend(
                string.Format(" {0} ({1}) as {2} ", _joinKey, sql, _asName));
        }
    }

    /// <summary>select (select…) as alias</summary>
    public sealed class SelectSubqueryStep : IStep
    {
        private readonly string _asName;
        private readonly IReadOnlyList<IStep> _childSteps;

        public SelectSubqueryStep(string asName, IReadOnlyList<IStep> childSteps)
        {
            _asName = asName;
            _childSteps = childSteps ?? Array.Empty<IStep>();
        }

        public void Apply(SQLBuilder builder)
        {
            var bro = builder.Inner.getBrotherBuilder();
            SQLBuilder.ReplaySteps(bro, _childSteps);
            var sql = bro.toSelect().sql;
            builder.Inner.current.select(string.Format("({0}) as {1} ", sql, _asName));
        }
    }

    /// <summary>where key op (select…)</summary>
    public sealed class WhereSubqueryStep : IStep
    {
        private readonly string _key;
        private readonly string _op;
        private readonly IReadOnlyList<IStep> _childSteps;

        public WhereSubqueryStep(string key, string op, IReadOnlyList<IStep> childSteps)
        {
            _key = key;
            _op = op;
            _childSteps = childSteps ?? Array.Empty<IStep>();
        }

        public void Apply(SQLBuilder builder)
        {
            var bro = builder.Inner.getBrotherBuilder();
            SQLBuilder.ReplaySteps(bro, _childSteps);
            var sql = bro.toSelect().sql;
            builder.Inner.current.where(_key, " (" + sql + ") ", _op, false);
        }
    }

    /// <summary>where ( where-fragment )</summary>
    public sealed class WhereFragmentStep : IStep
    {
        private readonly IReadOnlyList<IStep> _childSteps;

        public WhereFragmentStep(IReadOnlyList<IStep> childSteps)
        {
            _childSteps = childSteps ?? Array.Empty<IStep>();
        }

        public void Apply(SQLBuilder builder)
        {
            var bro = builder.Inner.getBrotherBuilder();
            SQLBuilder.ReplaySteps(bro, _childSteps);
            var content = bro.buildWhereContent();
            builder.Inner.current.where("", " (" + content + ") ", "", false);
        }
    }

    /// <summary>whereOR：兄弟 or 模式搓条件再并入</summary>
    public sealed class WhereORSubqueryStep : IStep
    {
        private readonly IReadOnlyList<IStep> _childSteps;

        public WhereORSubqueryStep(IReadOnlyList<IStep> childSteps)
        {
            _childSteps = childSteps ?? Array.Empty<IStep>();
        }

        public void Apply(SQLBuilder builder)
        {
            var bro = builder.Inner.getBrotherBuilder();
            bro.or();
            SQLBuilder.ReplaySteps(bro, _childSteps);
            var t = bro.buildWhereContent();
            if (!string.IsNullOrWhiteSpace(t))
            {
                builder.Inner.orLeft().where(t).orRight();
            }
        }
    }

    /// <summary>withSelect / withAs：兄弟上重放后挂入 CTE</summary>
    public sealed class WithSelectSubqueryStep : IStep
    {
        private readonly string _name;
        private readonly IReadOnlyList<IStep> _childSteps;

        public WithSelectSubqueryStep(string name, IReadOnlyList<IStep> childSteps)
        {
            _name = name;
            _childSteps = childSteps ?? Array.Empty<IStep>();
        }

        public void Apply(SQLBuilder builder)
        {
            var kit = builder.Inner.getBrotherBuilder();
            SQLBuilder.ReplaySteps(kit, _childSteps);
            var item = new SqlCTEItem();
            item.builder = kit;
            item.type = SqlCTEType.Select;
            item.asName = _name;
            builder.Inner.ApartGetCte().add(item);
        }
    }
}
