using FluentAssertions;
using mooSQL.data;
using mooSQL.excel;
using mooSQL.excel.context;
using System;
using System.Collections.Generic;
using System.Data;
using System.Reflection;
using Xunit;

namespace mooSQL.Pure.Tests
{
    /// <summary>
    /// Excel 导入正确性定点测试（不依赖真实 Excel 文件）。
    /// </summary>
    public class ExcelImportCorrectnessTests
    {
        private sealed class Harness : ExcelRead
        {
            private readonly Dictionary<string, string> _cache = new Dictionary<string, string>();
            public int AfterInsertCalls { get; private set; }
            public int AfterUpdateCalls { get; private set; }
            public bool ThrowInAfterInsert { get; set; }

            public Harness() : base("test-token")
            {
                context.option = new ImportOption(this) { checkMode = "database", outInfoCol = "id" };
                readingRow = new rowInfo { rowMark = "第1行" };
            }

            public string CallFormatSqlKey(string freeStr, out int errCount) => formatSqlKey(freeStr, out errCount);

            public string CallPatch(colInfo col, string val, WriteTable tb, string jump)
                => patchValueToWrite(col, val, tb, jump);

            public breakPoint CallApply(WriteTable li, WriteTable tb, string jump)
                => ApplyWriteColumns(li, tb, jump);

            public void CallExecuteInsert(WriteTable tb) => ExecuteInsertRow(tb);

            public void CallExecuteUpdate(WriteTable tb) => ExecuteUpdateRow(tb);

            protected override void OnAfterInsertRow(WriteTable tb, DataRow row)
            {
                AfterInsertCalls++;
                if (ThrowInAfterInsert)
                {
                    throw new InvalidOperationException("log-fail");
                }
            }

            protected override void OnAfterUpdateRow(WriteTable tb, DataRow row)
            {
                AfterUpdateCalls++;
            }

            public override DBInstance GetDBInstance(int position) => null!;
            public override void saveMsgToExcel() { }
            public override Dictionary<string, string> getCodeNameToIdMap(string codetableId)
                => new Dictionary<string, string>();
            public override void removeCache(string key) => _cache.Remove(key);
            public override string getCacheValue(string key)
                => _cache.TryGetValue(key, out var v) ? v : "";
            public override void setCacheValue(string key, string value) => _cache[key] = value;
        }

        private static WriteTable CreateWriteTable(Harness root, checkFailAct failPolicy)
        {
            var opt = new Table(root.context.option)
            {
                name = "T",
                DBName = "T",
                caption = "测试表",
                failPolicy = failPolicy,
                batchUpdate = true
            };
            var tb = new WriteTable(opt) { root = root, canInsert = true, canUpdate = true };
            var dt = new DataTable();
            dt.Columns.Add("Id", typeof(string));
            dt.Columns.Add("Name", typeof(string));
            var old = dt.NewRow();
            old["Id"] = "1";
            old["Name"] = "old";
            dt.Rows.Add(old);
            tb.checkResult = new[] { old };

            var bulk = new BulkBase { bulkTarget = dt.Clone() };
            typeof(WriteTable).GetField("_writor", BindingFlags.Instance | BindingFlags.NonPublic)!
                .SetValue(tb, bulk);
            tb.addingRow = bulk.bulkTarget.NewRow();
            tb.addingRow["Id"] = "2";
            tb.addingRow["Name"] = "new";
            return tb;
        }

        private static colInfo CreateNeedCol(Harness root, string field)
        {
            var opt = new Column(root.context.option) { field = field };
            var col = new colInfo(opt)
            {
                root = root,
                ID = field,
                key = field,
                caption = field,
                isNeed = true,
                canInsert = true,
                canUpdate = true,
                colType = valueType.stringi,
                dataType = typeof(string)
            };
            return col;
        }

        [Fact]
        public void FormatSqlKey_ShouldReplaceDollarBracePlaceholders()
        {
            var h = new Harness();
            var col = new colInfo("inst", h) { writeValue = "OID-1", state = "done" };
            h.context.valueCollection.addCol("inst", col);

            var result = h.CallFormatSqlKey("Inst_FK='${inst}'", out var err);

            err.Should().Be(0);
            result.Should().Be("Inst_FK='OID-1'");
        }

        [Fact]
        public void PatchValueToWrite_FailPolicy_ShouldReturnControlCodes()
        {
            var h = new Harness();
            var tbSelf = CreateWriteTable(h, checkFailAct.self);
            tbSelf.checkResult = Array.Empty<DataRow>();
            var col = CreateNeedCol(h, "Name");

            h.CallPatch(col, default!, tbSelf, "").Should().Be("c");

            var tbNext = CreateWriteTable(h, checkFailAct.next);
            tbNext.checkResult = Array.Empty<DataRow>();
            h.CallPatch(col, default!, tbNext, "").Should().Be("b");

            var tbRow = CreateWriteTable(h, checkFailAct.row);
            tbRow.checkResult = Array.Empty<DataRow>();
            h.CallPatch(col, default!, tbRow, "").Should().Be("end");
        }

        [Fact]
        public void ApplyWriteColumns_ShouldMapPatchCodesToBreakPoints()
        {
            var h = new Harness();
            var tb = CreateWriteTable(h, checkFailAct.row);
            tb.checkResult = Array.Empty<DataRow>();
            tb.srcRow = new rowInfo { dataRow = new DataTable().NewRow() };
            var col = CreateNeedCol(h, "Name");
            col.writeValue = null!;
            tb.writeCols["Name"] = col;
            // loadWriteColValue will try load from row; ensure col stays empty need
            col.type = columnType.fix;
            col.state = "done";
            col.writeValue = null!;

            // Directly exercise mapping via patch return by using a stub col that patch sees as invalid need
            var mapEnd = h.CallApply(tb, tb, "");
            // With fix+null need, loadWriteColValue may still leave null → patch returns end for row policy
            mapEnd.Should().Be(breakPoint.excelRowContine);

            var tbBreak = CreateWriteTable(h, checkFailAct.next);
            tbBreak.checkResult = Array.Empty<DataRow>();
            tbBreak.srcRow = tb.srcRow;
            var col2 = CreateNeedCol(h, "Name");
            col2.type = columnType.fix;
            col2.state = "done";
            col2.writeValue = null!;
            tbBreak.writeCols["Name"] = col2;
            h.CallApply(tbBreak, tbBreak, "").Should().Be(breakPoint.tableBreak);

            var tbCont = CreateWriteTable(h, checkFailAct.self);
            tbCont.checkResult = Array.Empty<DataRow>();
            tbCont.srcRow = tb.srcRow;
            var col3 = CreateNeedCol(h, "Name");
            col3.type = columnType.fix;
            col3.state = "done";
            col3.writeValue = null!;
            tbCont.writeCols["Name"] = col3;
            h.CallApply(tbCont, tbCont, "").Should().Be(breakPoint.tableContinue);
        }

        [Fact]
        public void ReplaceReg_ShouldAssignReplacedValue()
        {
            var h = new Harness();
            var opt = new Column(h.context.option)
            {
                replaceReg = @"\s+",
                replaceAs = ""
            };
            var col = new colInfo("name", h)
            {
                option = opt,
                type = columnType.match,
                writeValue = "a b",
                ExcelIndex = 0,
                excelCode = "A"
            };
            h.context.valueCollection.addCol("name", col);
            var dt = new DataTable();
            dt.Columns.Add("A");
            var row = dt.NewRow();
            row["A"] = "a b";
            dt.Rows.Add(row);

            h.context.valueCollection.loadColData(row, col);

            col.writeValue.Should().Be("ab");
        }

        [Fact]
        public void ExecuteInsertRow_ShouldCallOnAfterInsert_AndSwallowLogException()
        {
            var h = new Harness { ThrowInAfterInsert = true };
            var tb = CreateWriteTable(h, checkFailAct.self);
            tb.checkResult = Array.Empty<DataRow>();

            h.Invoking(x => x.CallExecuteInsert(tb)).Should().NotThrow();
            h.AfterInsertCalls.Should().Be(1);
            tb.bulk.bulkTarget.Rows.Count.Should().Be(1);
        }

        [Fact]
        public void ExecuteUpdateRow_ShouldCallOnAfterUpdate()
        {
            var h = new Harness();
            var tb = CreateWriteTable(h, checkFailAct.self);
            tb.canUpdate = true;

            h.CallExecuteUpdate(tb);

            h.AfterUpdateCalls.Should().Be(1);
        }
    }
}
