// 基础功能说明：

using HHNY.NET.Application.Entity;
using HHNY.NET.Core;
using mooSQL.data;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace TestMooSQL.src;
public class DBInsTest
{
    [Fact]
    public void selectTop1()
    {

        var db = DBTest.GetDBInstance(0);
        //查询一个表
        var cmd = "select a.* from table as a where a.idx>{0}".formatSQL(3);
        DataTable dt = db.ExeQuery(cmd);
        //执行更新
        var cmd2 = "update table set name={0} where id={1}".formatSQL("张三", 1);
        int count=db.ExeNonQuery(cmd2);



        var sql = cmd.toRawSQL();
        Assert.Equal("merge into HH_SysRole  using UCML_RESPONSIBILITY as r on (HH_SysRole.Code=r.R_Code)   when not matched  then insert(Name,Remark,OrderNo,DataScope,Code,TenantId,IsDelete) values( r.RESP_NAME,r.RESP_DESC_TEXT,r.[level],r.accessType,r.R_Code,'1300000000001',0)   when not matched  then update set Name=r.RESP_NAME ,Remark=r.RESP_DESC_TEXT ,OrderNo=r.[level] ,DataScope=r.accessType  ;", sql);
    }


    public void usedbrepo()
    {

        var db = DBTest.GetDBInstance(0);
        //查询一个用户
        var rep = db.useRepo<SysUser>();
        SysUser user = rep.GetById(1);


    }

    public void usedbUOF()
    {

        var db = DBTest.GetDBInstance(0);
        //查询一个用户
        var work = db.useUnitOfWork();
        work.Insert(new SysUser() { Id=1,RealName="张三"});
        work.Update(new SysUser() { Id = 1, RealName = "张三" });
        int count=work.Commit();


    }

    public void useClipi()
    {

        var db = DBTest.GetDBInstance(0);
        //使用实体查询
        var clip = db.useClip();
        //执行查询
        IEnumerable<HHDutyItem> tasklist=clip
            .from<HHDutyItem>(out var t)
            .where(()=>t.Di_Code,"1")
            .select(t)
            .queryList();
        //执行修改
        int count= clip
             .setTable<HHDutyItem>(out t)
             .set(() => t.Di_Code, "1")
             .where(() => t.Di_Name, "")
             .doUpdate();


    }
}
