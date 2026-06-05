using System;
using System.Collections.Generic;

namespace mooSQL.data
{
    internal interface IApartStep
    {
        void Apply(SQLBuilder kit);
    }

    internal sealed class ApartSelectStep : IApartStep
    {
        private readonly string _columns;
        public ApartSelectStep(string columns) => _columns = columns;
        public void Apply(SQLBuilder kit) => kit.select(_columns);
    }

    internal sealed class ApartFromStep : IApartStep
    {
        private readonly string _from;
        public ApartFromStep(string from) => _from = from;
        public void Apply(SQLBuilder kit) => kit.from(_from);
    }

    internal sealed class ApartWhereReplayStep : IApartStep
    {
        private readonly List<WhereStep> _steps;
        public ApartWhereReplayStep(List<WhereStep> steps) => _steps = steps;
        public void Apply(SQLBuilder kit) => WhereStep.Replay(_steps, kit);
    }

    internal sealed class ApartDistinctStep : IApartStep
    {
        public static readonly ApartDistinctStep Instance = new ApartDistinctStep();
        public void Apply(SQLBuilder kit) => kit.distinct();
    }

    internal sealed class ApartTopStep : IApartStep
    {
        private readonly int _num;
        public ApartTopStep(int num) => _num = num;
        public void Apply(SQLBuilder kit) => kit.top(_num);
    }

    internal sealed class ApartGroupByStep : IApartStep
    {
        private readonly string _field;
        public ApartGroupByStep(string field) => _field = field;
        public void Apply(SQLBuilder kit) => kit.groupBy(_field);
    }

    internal sealed class ApartOrderByStep : IApartStep
    {
        private readonly string _part;
        public ApartOrderByStep(string part) => _part = part;
        public void Apply(SQLBuilder kit) => kit.orderBy(_part);
    }

    internal sealed class ApartHavingStep : IApartStep
    {
        private readonly string _having;
        public ApartHavingStep(string having) => _having = having;
        public void Apply(SQLBuilder kit) => kit.having(_having);
    }

    internal sealed class ApartSetPageStep : IApartStep
    {
        private readonly int _size;
        private readonly int _num;
        public ApartSetPageStep(int size, int num)
        {
            _size = size;
            _num = num;
        }
        public void Apply(SQLBuilder kit) => kit.setPage(_size, _num);
    }

    internal sealed class ApartSetTableStep : IApartStep
    {
        private readonly string _table;
        public ApartSetTableStep(string table) => _table = table;
        public void Apply(SQLBuilder kit) => kit.setTable(_table);
    }

    internal sealed class ApartSetColumnStep : IApartStep
    {
        private readonly string _key;
        private readonly object _val;
        private readonly bool _paramed;
        private readonly bool _updatable;
        private readonly bool _insertable;

        public ApartSetColumnStep(string key, object val, bool paramed, bool updatable, bool insertable)
        {
            _key = key;
            _val = val;
            _paramed = paramed;
            _updatable = updatable;
            _insertable = insertable;
        }

        public void Apply(SQLBuilder kit) =>
            kit.set(_key, _val, _paramed, null, _updatable, _insertable);
    }

    internal sealed class ApartNewRowStep : IApartStep
    {
        public static readonly ApartNewRowStep Instance = new ApartNewRowStep();
        public void Apply(SQLBuilder kit) => kit.newRow();
    }

    internal sealed class ApartUnionBranchStep : IApartStep
    {
        private readonly bool _isUnionAll;
        private readonly bool _wrapSelect;
        private readonly string _wrapName;
        private readonly bool _isFirst;

        public ApartUnionBranchStep(bool isUnionAll, bool wrapSelect, string wrapName, bool isFirst)
        {
            _isUnionAll = isUnionAll;
            _wrapSelect = wrapSelect;
            _wrapName = wrapName;
            _isFirst = isFirst;
        }

        public void Apply(SQLBuilder kit)
        {
            if (_isFirst)
                kit.union(_isUnionAll, _wrapSelect, _wrapName);
            else
                kit.union();
        }
    }

    internal sealed class ApartGroupScriptStep : IApartStep
    {
        private readonly ApartBuildScript _groupScript;
        public ApartGroupScriptStep(ApartBuildScript groupScript) => _groupScript = groupScript;
        public void Apply(SQLBuilder kit) => _groupScript.ApplyTo(kit);
    }

    internal sealed class ApartCteSelectStep : IApartStep
    {
        private readonly string _name;
        private readonly ApartBuildScript _inner;
        public ApartCteSelectStep(string name, ApartBuildScript inner)
        {
            _name = name;
            _inner = inner;
        }
        public void Apply(SQLBuilder kit) =>
            kit.withSelect(_name, b => _inner.ApplyTo(b));
    }

    internal sealed class ApartCteSolidStep : IApartStep
    {
        private readonly string _name;
        private readonly string _sql;
        public ApartCteSolidStep(string name, string sql)
        {
            _name = name;
            _sql = sql;
        }
        public void Apply(SQLBuilder kit) => kit.withSelect(_name, _sql);
    }

    internal sealed class ApartSummaryFieldStep : IApartStep
    {
        private readonly string _field;
        public ApartSummaryFieldStep(string field) => _field = field;
        public void Apply(SQLBuilder kit) => kit.selectSummary(_field);
    }

    /// <summary>
    /// 碎片构建步骤脚本。
    /// </summary>
    internal sealed class ApartBuildScript
    {
        private readonly List<IApartStep> _steps = new List<IApartStep>();

        public int Count => _steps.Count;

        public void Add(IApartStep step)
        {
            if (step != null) _steps.Add(step);
        }

        public void ApplyTo(SQLBuilder kit)
        {
            foreach (var step in _steps)
                step.Apply(kit);
        }

        public void Clear() => _steps.Clear();
    }
}
