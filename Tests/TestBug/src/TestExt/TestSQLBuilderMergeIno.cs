// 基础功能说明：

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace TestMooSQL.src;
public class TestSQLBuilderMergeIno
{
    [Fact]
    public void selectTop1()
    {

        var kit = DBTest.useSQL(0);
        var cmd= kit.mergeInto("HH_SysRole")
            .from("UCML_RESPONSIBILITY as r")
            .on("HH_SysRole.Code=r.R_Code")
            .set("Name", "r.RESP_NAME", false)
            .set("Remark", "r.RESP_DESC_TEXT", false)
            .set("OrderNo", "r.[level]", false)
            .set("DataScope", "r.accessType", false)
            .setI("Code", "r.R_Code", false)
            .setI("TenantId", "1300000000001")
            .setI("IsDelete", "0", false)
            .toMergeInto();

        var sql = cmd.toRawSQL();
        Assert.Equal("merge into HH_SysRole  using UCML_RESPONSIBILITY as r on (HH_SysRole.Code=r.R_Code)   when not matched  then insert(Name,Remark,OrderNo,DataScope,Code,TenantId,IsDelete) values( r.RESP_NAME,r.RESP_DESC_TEXT,r.[level],r.accessType,r.R_Code,'1300000000001',0)   when not matched  then update set Name=r.RESP_NAME ,Remark=r.RESP_DESC_TEXT ,OrderNo=r.[level] ,DataScope=r.accessType  ;", sql);
    }
}
