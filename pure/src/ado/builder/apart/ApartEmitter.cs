using System.Collections.Generic;

namespace mooSQL.data
{
    internal static class ApartEmitter
    {
        public static ApartBuildScript Emit(SQLBuilder source)
        {
            var script = new ApartBuildScript();
            EmitCte(source, script);
            EmitMainQuery(source, script);
            EmitMakeUps(source, script);
            return script;
        }

        private static void EmitCte(SQLBuilder source, ApartBuildScript script)
        {
            var cte = source.ApartGetCte();
            if (cte == null || cte.Empty) return;
            foreach (var item in cte.cteList)
            {
                if (item.type == SqlCTEType.Select && item.builder != null)
                {
                    var inner = Emit(item.builder);
                    script.Add(new ApartCteSelectStep(item.asName, inner));
                }
                else if (item.type == SqlCTEType.SolidSQL && !string.IsNullOrWhiteSpace(item.solidSQL))
                {
                    script.Add(new ApartCteSolidStep(item.asName, item.solidSQL));
                }
            }
        }

        private static void EmitMainQuery(SQLBuilder source, ApartBuildScript script)
        {
            if (source.unionHolder != null && source.unionHolder.Count > 0)
            {
                var uh = source.unionHolder;
                var united = uh.united;
                for (int i = 0; i < united.Count; i++)
                {
                    if (i > 0)
                    {
                        script.Add(new ApartUnionBranchStep(
                            uh.ApartUnionAll,
                            uh.ApartUnionWrap,
                            uh.ApartUnionName,
                            i == 1));
                    }
                    var branch = new ApartBuildScript();
                    EmitGroup(united[i], branch);
                    script.Add(new ApartGroupScriptStep(branch));
                }
            }
            else
            {
                EmitGroup(source.current, script);
            }
        }

        private static void EmitMakeUps(SQLBuilder source, ApartBuildScript script)
        {
            var makeUps = source._MakeUps;
            if (makeUps?.summaryField == null) return;
            foreach (var field in makeUps.summaryField)
            {
                if (!string.IsNullOrWhiteSpace(field))
                    script.Add(new ApartSummaryFieldStep(field));
            }
        }

        private static void EmitGroup(SqlGoup group, ApartBuildScript script)
        {
            if (group == null) return;

            foreach (var col in group.selectPart)
                script.Add(new ApartSelectStep(col));

            if (group.ApartHasDistinct)
                script.Add(ApartDistinctStep.Instance);

            foreach (var from in group.fromPart)
                script.Add(new ApartFromStep(from));

            if (group.wherePart != null && group.wherePart.steps.steps.Count > 0)
            {
                script.Add(new ApartWhereReplayStep(
                    WhereStep.CloneList(group.wherePart.steps.steps)));
            }

            foreach (var g in group.groupbyPart)
                script.Add(new ApartGroupByStep(g));

            if (!string.IsNullOrWhiteSpace(group.havingPart))
                script.Add(new ApartHavingStep(group.havingPart));

            foreach (var o in group.orderPart)
                script.Add(new ApartOrderByStep(o));

            if (group.toped > 0)
                script.Add(new ApartTopStep(group.toped));

            if (group.pageNum >= 0)
                script.Add(new ApartSetPageStep(group.pageSize, group.pageNum));

            if (!string.IsNullOrWhiteSpace(group.tableName))
                script.Add(new ApartSetTableStep(group.tableName));

            EmitSetColumns(group, script);
        }

        private static void EmitSetColumns(SqlGoup group, ApartBuildScript script)
        {
            if (group.columns == null || group.columns.Count == 0) return;
            int maxRow = group.RowIndex;
            for (int row = 0; row <= maxRow; row++)
            {
                if (row > 0)
                    script.Add(ApartNewRowStep.Instance);
                foreach (var col in group.columns)
                {
                    if (!col.values.ContainsKey(row)) continue;
                    var pair = col.values[row];
                    script.Add(new ApartSetColumnStep(
                        col.key, pair.value, pair.paramed, pair.updatable, pair.insetable));
                }
            }
        }
    }
}
