using System;
using System.Collections.Generic;
using mooSQL.utils;

namespace mooSQL.data
{
    /// <summary>from (select鈥? as alias 鈥斺€?瀛樺瓙姝ラ锛屼笉瀛?Action銆?/summary>
    public sealed class FromSubqueryStep : StepBase {
        public override int Id { get { return 524349; } }
        public override StepKind Kind { get { return StepKind.From; } }

        private readonly string _asName;
        private readonly IReadOnlyList<IStep> _childSteps;

        public FromSubqueryStep(string asName, IReadOnlyList<IStep> childSteps)
        {
            _asName = asName;
            _childSteps = childSteps ?? ArrayCache.Empty<IStep>();
        }
        public override void ContributeHash(ref ScriptHash hc, string paraRule, ref bool opened)
        {
            hc.Add(Id);
            hc.Add(1);
            hc.Add(_asName);
            ContributeChildSteps(ref hc, _childSteps, paraRule);
        }
                public override void Apply(SQLBuilder builder)
        {
            var bro = builder.Inner.getBrotherBuilder();
            PrepareSQLBuilder.ReplaySteps(bro.Inner, _childSteps);
            var sql = bro.toSelect().sql;
            builder.Inner.current.from(string.Format("({0}) as {1} ", sql, _asName));
        }
    }

    /// <summary>JOIN (select鈥? as alias</summary>
    public sealed class JoinSubqueryStep : StepBase {
        public override int Id { get { return 524350; } }
        public override StepKind Kind { get { return StepKind.Join; } }

        private readonly string _joinKey;
        private readonly string _asName;
        private readonly IReadOnlyList<IStep> _childSteps;

        public JoinSubqueryStep(string joinKey, string asName, IReadOnlyList<IStep> childSteps)
        {
            _joinKey = joinKey;
            _asName = asName;
            _childSteps = childSteps ?? ArrayCache.Empty<IStep>();
        }
        public override void ContributeHash(ref ScriptHash hc, string paraRule, ref bool opened)
        {
            hc.Add(Id);
            hc.Add(1);
            hc.Add(_joinKey);
            hc.Add(_asName);
            ContributeChildSteps(ref hc, _childSteps, paraRule);
        }
                public override void Apply(SQLBuilder builder)
        {
            var bro = builder.Inner.getBrotherBuilder();
            PrepareSQLBuilder.ReplaySteps(bro.Inner, _childSteps);
            var sql = bro.toSelect().sql;
            builder.Inner.current.fromAppend(
                string.Format(" {0} ({1}) as {2} ", _joinKey, sql, _asName));
        }
    }

    /// <summary>select (select鈥? as alias</summary>
    public sealed class SelectSubqueryStep : StepBase {
        public override int Id { get { return 524351; } }
        public override StepKind Kind { get { return StepKind.Select; } }

        private readonly string _asName;
        private readonly IReadOnlyList<IStep> _childSteps;

        public SelectSubqueryStep(string asName, IReadOnlyList<IStep> childSteps)
        {
            _asName = asName;
            _childSteps = childSteps ?? ArrayCache.Empty<IStep>();
        }
        public override void ContributeHash(ref ScriptHash hc, string paraRule, ref bool opened)
        {
            hc.Add(Id);
            hc.Add(1);
            hc.Add(_asName);
            ContributeChildSteps(ref hc, _childSteps, paraRule);
        }
                public override void Apply(SQLBuilder builder)
        {
            var bro = builder.Inner.getBrotherBuilder();
            PrepareSQLBuilder.ReplaySteps(bro.Inner, _childSteps);
            var sql = bro.toSelect().sql;
            builder.Inner.current.select(string.Format("({0}) as {1} ", sql, _asName));
        }
    }

    /// <summary>where key op (select鈥?</summary>
    public sealed class WhereSubqueryStep : StepBase {
        public override int Id { get { return 524352; } }
        public override StepKind Kind { get { return StepKind.Where; } }

        private readonly string _key;
        private readonly string _op;
        private readonly IReadOnlyList<IStep> _childSteps;

        public WhereSubqueryStep(string key, string op, IReadOnlyList<IStep> childSteps)
        {
            _key = key;
            _op = op;
            _childSteps = childSteps ?? ArrayCache.Empty<IStep>();
        }
        public override void ContributeHash(ref ScriptHash hc, string paraRule, ref bool opened)
        {
            if (!ConsumeOpened(ref opened))
            {
                hc.Add(Id);
                hc.Add(0);
                hc.Add(_key);
                hc.Add(_op);
                ContributeChildSteps(ref hc, _childSteps, paraRule);
                return;
            }
            var emit = true;
            hc.Add(Id);
            hc.Add(emit ? 1 : 0);
            hc.Add(_key);
            hc.Add(_op);
            ContributeChildSteps(ref hc, _childSteps, paraRule);
        }
                public override void Apply(SQLBuilder builder)
        {
            var bro = builder.Inner.getBrotherBuilder();
            PrepareSQLBuilder.ReplaySteps(bro.Inner, _childSteps);
            var sql = bro.toSelect().sql;
            builder.Inner.current.where(_key, " (" + sql + ") ", _op, false);
        }
    }

    /// <summary>where ( where-fragment )</summary>
    public sealed class WhereFragmentStep : StepBase {
        public override int Id { get { return 524353; } }
        public override StepKind Kind { get { return StepKind.Where; } }

        private readonly IReadOnlyList<IStep> _childSteps;

        public WhereFragmentStep(IReadOnlyList<IStep> childSteps)
        {
            _childSteps = childSteps ?? ArrayCache.Empty<IStep>();
        }
        public override void ContributeHash(ref ScriptHash hc, string paraRule, ref bool opened)
        {
            if (!ConsumeOpened(ref opened))
            {
                hc.Add(Id);
                hc.Add(0);
                ContributeChildSteps(ref hc, _childSteps, paraRule);
                return;
            }
            var emit = true;
            hc.Add(Id);
            hc.Add(emit ? 1 : 0);
            ContributeChildSteps(ref hc, _childSteps, paraRule);
        }

                public override void Apply(SQLBuilder builder)
        {
            var bro = builder.Inner.getBrotherBuilder();
            PrepareSQLBuilder.ReplaySteps(bro.Inner, _childSteps);
            var content = bro.buildWhereContent();
            builder.Inner.current.where("", " (" + content + ") ", "", false);
        }
    }

    /// <summary>whereOR锛氬厔寮?or 妯″紡鎼撴潯浠跺啀骞跺叆</summary>
    public sealed class WhereORSubqueryStep : StepBase {
        public override int Id { get { return 524354; } }
        public override StepKind Kind { get { return StepKind.Where; } }

        private readonly IReadOnlyList<IStep> _childSteps;

        public WhereORSubqueryStep(IReadOnlyList<IStep> childSteps)
        {
            _childSteps = childSteps ?? ArrayCache.Empty<IStep>();
        }
        public override void ContributeHash(ref ScriptHash hc, string paraRule, ref bool opened)
        {
            if (!ConsumeOpened(ref opened))
            {
                hc.Add(Id);
                hc.Add(0);
                ContributeChildSteps(ref hc, _childSteps, paraRule);
                return;
            }
            var emit = true;
            hc.Add(Id);
            hc.Add(emit ? 1 : 0);
            ContributeChildSteps(ref hc, _childSteps, paraRule);
        }

                public override void Apply(SQLBuilder builder)
        {
            var bro = builder.Inner.getBrotherBuilder();
            bro.or();
            PrepareSQLBuilder.ReplaySteps(bro.Inner, _childSteps);
            var t = bro.buildWhereContent();
            if (!string.IsNullOrWhiteSpace(t))
            {
                builder.Inner.orLeft().where(t).orRight();
            }
        }
    }

    /// <summary>withSelect / withAs锛氬厔寮熶笂閲嶆斁鍚庢寕鍏?CTE</summary>
    public sealed class WithSelectSubqueryStep : StepBase {
        public override int Id { get { return 524355; } }
        public override StepKind Kind { get { return StepKind.Cte; } }

        private readonly string _name;
        private readonly IReadOnlyList<IStep> _childSteps;

        public WithSelectSubqueryStep(string name, IReadOnlyList<IStep> childSteps)
        {
            _name = name;
            _childSteps = childSteps ?? ArrayCache.Empty<IStep>();
        }
        public override void ContributeHash(ref ScriptHash hc, string paraRule, ref bool opened)
        {
            hc.Add(Id);
            hc.Add(1);
            hc.Add(_name);
            ContributeChildSteps(ref hc, _childSteps, paraRule);
        }
                public override void Apply(SQLBuilder builder)
        {
            var kit = builder.Inner.getBrotherBuilder();
            PrepareSQLBuilder.ReplaySteps(kit.Inner, _childSteps);
            var item = new SqlCTEItem();
            item.builder = kit.Inner;
            item.type = SqlCTEType.Select;
            item.asName = _name;
            builder.Inner.ApartGetCte().add(item);
        }
    }
}
