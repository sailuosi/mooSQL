using System;
using mooSQL.data;
using mooSQL.Pure.Tests.TestHelpers;
using Xunit;

namespace mooSQL.Pure.Tests.DocDemo
{
    /// <summary>
    /// 对标文档 <c>doc/docs/SQL/basis/sqlBuilderdemo.md</c> 各案例的严格 ExactSql。
    /// CTE withRecurTo 见 <see cref="SQLBuilderWithRecurToBizTests"/>。
    /// </summary>
    public class DocSqlBuilderDemoExactSqlTests : IDisposable
    {
        private readonly SQLBuilder _kit;
        private readonly SQLBuilder _mssql;

        public DocSqlBuilderDemoExactSqlTests()
        {
            _kit = TestDatabaseHelper.CreateSQLBuilder();
            _mssql = TestDatabaseHelper.CreateSQLBuilder(DataBaseType.MSSQL);
        }

        public void Dispose()
        {
            _kit?.Dispose();
            _mssql?.Dispose();
        }

        /// <summary>常规 select + top</summary>
        [Fact]
        public void Demo_TopSelect_ExactSql()
        {
            Api8UsageSqlAssert.AssertExactSql(
                _kit.clear()
                    .select("t.ZH_TroubleOID as oid, t.T_Title, t.T_OrgName")
                    .from("ZH_Trouble t")
                    .where("t.T_Status= 1")
                    .orderBy("t.SYS_Created desc")
                    .top(6)
                    .toSelect(),
                "SELECT t.ZH_TroubleOID as oid, t.T_Title, t.T_OrgName FROM ZH_Trouble t WHERE t.T_Status= 1 ORDER BY t.SYS_Created desc LIMIT 6 OFFSET 0 ");
        }

        /// <summary>翻页 select + rowNumber（文档提示：排序写入 rowNumber；MSSQL 形态）</summary>
        [Fact]
        public void Demo_PageRowNumber_ExactSql()
        {
            Api8UsageSqlAssert.AssertExactSql(
                _mssql.clear()
                    .select("a.ZH_DangerAreaOID,a.SYS_Created")
                    .from("ZH_DangerArea a")
                    .where("(a.SYS_Deleted is null or a.SYS_Deleted=0)")
                    .setPage(10, 2)
                    .rowNumber("DA_Idx", "rowm")
                    .orderBy("rowm asc")
                    .toSelect(),
                "WITH datares AS ( SELECT TOP 10 a.ZH_DangerAreaOID,a.SYS_Created,ROW_NUMBER() OVER (ORDER BY DA_Idx) AS rowm FROM ZH_DangerArea a WHERE (a.SYS_Deleted is null or a.SYS_Deleted=0)  ) SELECT * FROM datares WHERE rowm > 10 ORDER BY rowm asc ");
        }

        /// <summary>group by</summary>
        [Fact]
        public void Demo_GroupBy_ExactSql()
        {
            Api8UsageSqlAssert.AssertExactSql(
                _kit.clear()
                    .select("r.RS_Year,COUNT(*) as cc,SUM(r.RS_Agreementfunds) as funds")
                    .from("ZH_RelocatStatist r")
                    .groupBy("r.RS_Year")
                    .orderBy("r.RS_Year desc")
                    .top(10)
                    .toSelect(),
                "SELECT r.RS_Year,COUNT(*) as cc,SUM(r.RS_Agreementfunds) as funds FROM ZH_RelocatStatist r GROUP BY r.RS_Year ORDER BY r.RS_Year desc LIMIT 10 OFFSET 0 ");
        }

        /// <summary>普通 update</summary>
        [Fact]
        public void Demo_Update_ExactSql()
        {
            var fk = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
            Api8UsageSqlAssert.AssertExactSql(
                _kit.clear()
                    .setTable("ZH_PortCell")
                    .set("PC_X", "1")
                    .set("PC_Y", "2")
                    .set("PC_W", "3")
                    .set("PC_H", "4")
                    .where("ZH_PortCellOID", fk)
                    .toUpdate(),
                "UPDATE ZH_PortCell  SET PC_X='1' ,PC_Y='2' ,PC_W='3' ,PC_H='4'  WHERE ZH_PortCellOID = 'aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee'");
        }

        /// <summary>update from</summary>
        [Fact]
        public void Demo_UpdateFrom_ExactSql()
        {
            Api8UsageSqlAssert.AssertExactSql(
                _kit.clear()
                    .set("C_Year", "2022")
                    .set("C_TrainType", "p.Po_TrainType", false)
                    .set("C_Days", "p.Po_Day", false)
                    .setTable("c")
                    .from("PX_Class as c left join PX_PostClass p on c.PX_PostClass_FK=p.PX_PostClassOID")
                    .where("c.PX_ClassOID in ('oid1','oid2')")
                    .toUpdateFrom(),
                "UPDATE c SET C_Year='2022' ,C_TrainType=p.Po_TrainType ,C_Days=p.Po_Day   FROM PX_Class as c left join PX_PostClass p on c.PX_PostClass_FK=p.PX_PostClassOID  WHERE c.PX_ClassOID in ('oid1','oid2')");
        }

        /// <summary>普通 insert into values</summary>
        [Fact]
        public void Demo_Insert_ExactSql()
        {
            Api8UsageSqlAssert.AssertExactSql(
                _kit.clear()
                    .setTable("HH_SysUser")
                    .set("Id", 1001L)
                    .set("Account", "u1")
                    .set("Password", "pwd")
                    .set("Phone", "138")
                    .set("Sex", 1)
                    .set("Status", "1", false)
                    .set("AccountType", "666")
                    .set("OrgId", 0)
                    .set("OrderNo", 100)
                    .set("IsDelete", "0", false)
                    .toInsert(),
                "INSERT INTO HH_SysUser  (Id,Account,Password,Phone,Sex,Status,AccountType,OrgId,OrderNo,IsDelete)  VALUES ('1001','u1','pwd','138','1',1,'666','0','100',0) ");
        }

        /// <summary>多行 insert（newRow）</summary>
        [Fact]
        public void Demo_InsertMultiRow_ExactSql()
        {
            Api8UsageSqlAssert.AssertExactSql(
                _kit.clear()
                    .setTable("KB_DeptWorkor")
                    .set("KB_DeptWorkorOID", Guid.Parse("11111111-1111-1111-1111-111111111111"))
                    .set("HH_Org_FK", Guid.Parse("22222222-2222-2222-2222-222222222222"))
                    .set("Dw_Task", "1")
                    .set("Dw_Belong", "1", false)
                    .set("SYS_Deleted", "0", false)
                    .newRow()
                    .set("KB_DeptWorkorOID", Guid.Parse("33333333-3333-3333-3333-333333333333"))
                    .set("HH_Org_FK", Guid.Parse("44444444-4444-4444-4444-444444444444"))
                    .set("Dw_Task", "2")
                    .set("Dw_Belong", "1", false)
                    .set("SYS_Deleted", "0", false)
                    .toInsert(),
                "INSERT INTO KB_DeptWorkor  (KB_DeptWorkorOID,HH_Org_FK,Dw_Task,Dw_Belong,SYS_Deleted)  VALUES ('11111111-1111-1111-1111-111111111111','22222222-2222-2222-2222-222222222222','1',1,0),('33333333-3333-3333-3333-333333333333','44444444-4444-4444-4444-444444444444','2',1,0)");
        }

        /// <summary>union + selectUnioned</summary>
        [Fact]
        public void Demo_Union_ExactSql()
        {
            var start = new DateTime(2024, 1, 1);
            var end = new DateTime(2024, 12, 31);
            Api8UsageSqlAssert.AssertExactSql(
                _kit.clear()
                    .select("'办结' as statue,count(*) as co")
                    .from("KB_Task")
                    .where("KB_Statue", "2")
                    .where("KB_Type", "t1")
                    .where("KB_StartDate", start, ">=")
                    .where("KB_EndDate", end, "<=")
                    .union(false, true, "a")
                    .select(" '进行' as statue,count(*) as co")
                    .from("KB_Task")
                    .where("KB_Statue", "1")
                    .where("KB_Type", "t1")
                    .where("KB_StartDate", start, ">=")
                    .where("KB_EndDate", end, "<=")
                    .selectUnioned("a.statue,a.co")
                    .toSelect(),
                "SELECT a.statue,a.co FROM (  SELECT '办结' as statue,count(*) as co FROM KB_Task WHERE  ( KB_Statue = '2' AND KB_Type = 't1' AND KB_StartDate >= '"
                    + start
                    + "' AND KB_EndDate <= '"
                    + end
                    + "' )    UNION  SELECT  '进行' as statue,count(*) as co FROM KB_Task WHERE  ( KB_Statue = '1' AND KB_Type = 't1' AND KB_StartDate >= '"
                    + start
                    + "' AND KB_EndDate <= '"
                    + end
                    + "' )    ) as a ");
        }

        /// <summary>merge into（MSSQL）</summary>
        [Fact]
        public void Demo_MergeInto_ExactSql()
        {
            Api8UsageSqlAssert.AssertExactSql(
                _mssql.clear()
                    .setTable("HH_SysRole")
                    .from("UCML_RESPONSIBILITY as r")
                    .mergeOn("HH_SysRole.Code=r.R_Code")
                    .set("Name", "r.RESP_NAME", false)
                    .set("Remark", "r.RESP_DESC_TEXT", false)
                    .set("OrderNo", "r.[level]", false)
                    .set("DataScope", "r.accessType", false)
                    .setI("Code", "r.R_Code", false)
                    .setI("TenantId", "1300000000001", false)
                    .setI("IsDelete", "0", false)
                    .toMergeInto(),
                "MERGE INTO HH_SysRole  USING UCML_RESPONSIBILITY as r ON (HH_SysRole.Code=r.R_Code)   WHEN MATCHED  THEN INSERT(Name,Remark,OrderNo,DataScope,Code,TenantId,IsDelete) VALUES( r.RESP_NAME,r.RESP_DESC_TEXT,r.[level],r.accessType,r.R_Code,1300000000001,0)   WHEN NOT MATCHED  THEN INSERT(Name,Remark,OrderNo,DataScope,Code,TenantId,IsDelete) VALUES( r.RESP_NAME,r.RESP_DESC_TEXT,r.[level],r.accessType,r.R_Code,1300000000001,0) ;");
        }
    }
}
