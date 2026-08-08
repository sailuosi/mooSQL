// 基础功能说明：


using HHNY.NET.Core.Author.SysRole;
using mooSQL.auth;
using mooSQL.data;
using mooSQL.Pure.Tests.src.Auth;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TestMooSQL.src;

namespace HHNY.NET.Core.Author;
/// <summary>
/// 权限对象和数据库表的接合类
/// </summary>
public class AuthLoader:AuthDialect
{
    public AuthLoader()
    {
        this.pipeline = new AuthPipeline();
        this.wordBag = new WordBagDialect(this);
    }

    public AuthPipeline pipeline;
    public WordBagDialect wordBag;
    public override PipelineDialect getPipeLine()
    {
        return pipeline;
    }

    public override WordBagDialect getWordBag()
    {
        return wordBag;
    }


    public override AuthOrg getOrgByOID(string oid)
    {
        var kit = DBTest.useSQL(0);
        //select UCML_OrganizeOID,OrgNO,OrgName,Varchar1        from ucml_organize
        var row = kit.select("UCML_OrganizeOID,OrgNO,OrgName,Varchar1").from("UCML_Organize").where("UCML_OrganizeOID", oid).queryRow();
        if (row != null)
        {
            return new AuthOrg()
            {
                Name = row["OrgName"].ToString(),
                HROID = row["UCML_OrganizeOID"].ToString(),
                OrgNo = row["OrgNO"].ToString(),
                HRCode = row["Varchar1"].ToString()
            };
        }
        return null;
    }

    public override List<AuthOrg> getOrgByOIDs(List<string> oid)
    {
        var kit = DBTest.useSQL(0);
        //select UCML_OrganizeOID,OrgNO,OrgName,Varchar1        from ucml_organize
        var res = kit.select("UCML_OrganizeOID,OrgNO,OrgName,Varchar1").from("UCML_Organize")
            .whereIn("UCML_OrganizeOID", oid)
            .query<AuthOrg>((row) => {
                return new AuthOrg()
                {
                    Name = row["OrgName"].ToString(),
                    HROID = row["UCML_OrganizeOID"].ToString(),
                    OrgNo = row["OrgNO"].ToString(),
                    HRCode = row["Varchar1"].ToString()
                };
            });
        return res;
    }
    /// <summary>
    /// 角色别名 r
    /// </summary>
    /// <param name="user"></param>
    /// <param name="onloading"></param>
    /// <returns></returns>
    public List<AuthWord> loadRole( UserManager user,Action<SQLBuilder> onloading) {
        var kit = DBTest.useSQL(0);
        kit.select("Id,`Name`,`Code`,DataScope")
            .from("hh_sysrole r")
            //.pinLeft().or()
            .whereIn("r.Id", (a) =>
            {
                a.select("RoleId").from("hh_sysuserrole ur")
                 .where("ur.Acount", user.Account);
            })
            //.where("r.Code", "sys_default")
            //.pinRight().and()
            ;
        onloading(kit);

        var tar= kit.query<AuthWord>((row) => {
            return new AuthWord()
            {
                id = row["Id"].ToString(),
                scopeCode = row["DataScope"].ToString(),
                code= row["Code"].ToString(),
                name = row["Name"].ToString(),
            };
        
        });

        var defRole = getDefaultRole();
        if (defRole != null) { 
            foreach(var role in defRole)
            {
                tar.Add(role);
            }
            
        }
        return tar;
    }

    public override List<AuthWord> getDefaultRole() {
        var kit = DBTest.useSQL(0);
        return kit.select("Id,`Name`,`Code`,DataScope")
            .from("hh_sysrole r")
            .where("r.Code", "sys_default")
            .query((row) =>
            {
                return new AuthWord()
                {
                    id = row["Id"].ToString(),
                    scopeCode = row["DataScope"].ToString(),
                    code = row["Code"].ToString(),
                    name = row["Name"].ToString(),
                };
            });
    }

    public override AuthOrg loadManOrg(AuthUser man) {
        return getOrg((kit) =>
        {
            kit.where("o.UCML_OrganizeOID", man.orgOID);
        });
    }

    public override AuthOrg loadManDiv(AuthUser man)
    {
        return getOrg((kit) =>
        {
            kit.where("o.UCML_OrganizeOID", man.divisionOID);
        });
    }

    public override AuthPost loadManPost(AuthUser man)
    {
        return getPost((kit) =>
        {
            kit.where("p.Po_ID", man.postNo);
        });
    }

    public override AuthOrg getOrg(Action<SQLBuilder> whereBuilder)
    {
        var kit= DBTest.useSQL(0);
        //select UCML_OrganizeOID,ParentOID,Varchar1,OrgNO,OrgName from ucml_organize
        whereBuilder(kit);
        return kit.select("UCML_OrganizeOID,ParentOID,Varchar1,OrgNO,OrgName")
            .from("ucml_organize o")
            .queryRow<AuthOrg>((row) =>
            {
                return new AuthOrg()
                {
                    HROID = row["UCML_OrganizeOID"].ToString(),
                    HRCode = row["Varchar1"].ToString(),
                    OrgNo = row["OrgNO"].ToString(),
                    Name = row["OrgName"].ToString(),
                };
            });
    }

    public override AuthPost getPost(Action<SQLBuilder> whereBuilder)
    {
        var kit = DBTest.useSQL(0);
        //select UCML_OrganizeOID,ParentOID,Varchar1,OrgNO,OrgName from ucml_organize
        whereBuilder(kit);
        return kit.select("HR_PostOID,Po_ID,PO_Name,Post_ParentOID,Po_Number")
            .from("hr_post p")
            .queryRow<AuthPost>((row) =>
            {
                return new AuthPost()
                {
                    postCode = row["Po_Number"].ToString(),
                    postNo = row["Po_ID"].ToString(),
                    postOID = row["HR_PostOID"].ToString(),
                    //Name = row["OrgName"].ToString(),
                };
            });
    }


    public override AuthUser getUser(string acount) {
        return getUser((kit) =>
        {
            kit.where("c.CON_EMP_NUM", acount);
        });
    }


    public override AuthUser getUser(Action<SQLBuilder> whereBuilder)
    {
        var kit = DBTest.useSQL(0);
        //select UCML_OrganizeOID,ParentOID,Varchar1,OrgNO,OrgName from ucml_organize
        whereBuilder(kit);
        return kit.select("UCML_CONTACTOID,CON_EMP_NUM,PersonName,PostCode,HR_division_FK,HR_org_FK,UCML_UserOID")
            .from("ucml_contact c")
            .queryRow<AuthUser>((row) =>
            {
                return new AuthUser()
                {
                    UserOID = row["UCML_UserOID"].ToString(),
                    postNo = row["PostCode"].ToString(),
                    Acount = row["CON_EMP_NUM"].ToString(),
                    divisionOID = row["HR_division_FK"].ToString(),
                    orgOID = row["HR_org_FK"].ToString(),
                    dialect=this,
                };
            });
    }
}
