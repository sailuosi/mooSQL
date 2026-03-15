using HHNY.NET.Core;
using mooSQL.utils;
using SqlSugar;

namespace HHNY.NET.Application.Entity;

/// <summary>
/// 词条项
/// </summary>
[SugarTable("hh_worditem","词条项")]
public class HHWordItem :EntityOID
{
    #region 字段
    /// <summary>
    /// 
    /// </summary>
    [SugarColumn(ColumnName ="HH_WordItemOID",IsIdentity = false, ColumnDescription = "", IsPrimaryKey = true, Length = 36)]
    public string HH_WordItemOID { get; set; }
    
    /// <summary>
    /// 名称
    /// </summary>
    [SugarColumn(ColumnName ="Wi_Name",ColumnDescription = "名称", Length = 200)]
    public string? Wi_Name { get; set; }
    
    /// <summary>
    /// 编号
    /// </summary>
    [SugarColumn(ColumnName ="Wi_Code",ColumnDescription = "编号", Length = 50)]
    public string? Wi_Code { get; set; }
    
    /// <summary>
    /// 类型
    /// </summary>
    [SugarColumn(ColumnName ="Wi_Type",ColumnDescription = "类型", Length = 50)]
    public string? Wi_Type { get; set; }
    
    /// <summary>
    /// 说明
    /// </summary>
    [SugarColumn(ColumnName ="Wi_Note",ColumnDescription = "说明", Length = 50)]
    public string? Wi_Note { get; set; }
    
    /// <summary>
    /// 词条集合
    /// </summary>
    [SugarColumn(ColumnName ="HH_WordBag_FK",ColumnDescription = "词条集合", Length = 36)]
    public string? HH_WordBag_FK { get; set; }
    
    /// <summary>
    /// 词条定义
    /// </summary>
    [SugarColumn(ColumnName ="HH_WordDefine_FK",ColumnDescription = "词条定义", Length = 36)]
    public string? HH_WordDefine_FK { get; set; }
    


    #endregion

    public int Insert() {
        var kit = DBCash.useSQL(0);

        var cc= kit.setTable("hh_worditem")
            .set("HH_WordItemOID", HH_WordItemOID)
            .set("Wi_Name", Wi_Name)
            .set("Wi_Code", Wi_Code)
            .set("Wi_Type", Wi_Type)
            .set("Wi_Note", Wi_Note)
            .set("HH_WordBag_FK", HH_WordBag_FK)
            .set("HH_WordDefine_FK", HH_WordDefine_FK)
            .doInsert();
        return cc;
    }

    public int Update()
    {
        var kit = DBCash.useSQL(0);

        var cc = kit.setTable("hh_worditem")
            .set("Wi_Name", Wi_Name)
            .set("Wi_Code", Wi_Code)
            .set("Wi_Type", Wi_Type)
            .set("Wi_Note", Wi_Note)
            .set("HH_WordBag_FK", HH_WordBag_FK)
            .set("HH_WordDefine_FK", HH_WordDefine_FK)
            .where("HH_WordItemOID", HH_WordItemOID)
            .doUpdate();
        return cc;
    }

    public int Remove() {
        var kit = DBCash.useSQL(0);

        var cc = kit.setTable("hh_worditem")
            .where("HH_WordItemOID", HH_WordItemOID)
            .doDelete();
        return cc;
    }

    public int RemoveFake()
    {
        var kit = DBCash.useSQL(0);

        var cc = kit.setTable("hh_worditem")
            .set("SYS_Deleted", true)
            .where("HH_WordItemOID", HH_WordItemOID)
            .doUpdate();
        return cc;
    }

    public int Save(UserManager user)
    {
        if (RegxUntils.isGUID(HH_WordItemOID) == false) { 
            HH_WordItemOID= Guid.NewGuid().ToString();
            return Insert();
        }

        var kit = DBCash.useSQL(0);
        var cc=kit.from("hh_worditem").where("HH_WordItemOID", HH_WordItemOID).count();
        if(cc > 0)
        {
            setSysField(user,true);
            return Update();
        }
        setSysField(user,false);
        return Insert();
    }

    private void setSysField(UserManager user, bool isUpdate) { 
        if(!isUpdate)
        {
            //SYS_Created = DateTime.Now;
            //SYS_CreatedBy = user.UId;
            //SYS_Deleted = false;
            //SYS_DIVISION = user.DivOID;
            //SYS_ORG = user.OrgOID;
            //SYS_POSTN= user.PostOID;
            //SYS_REPLACEMENT = user.UId;
        }
        //SYS_LAST_UPD = DateTime.Now;
        //SYS_LAST_UPD_BY = user.UId;
    }
}
