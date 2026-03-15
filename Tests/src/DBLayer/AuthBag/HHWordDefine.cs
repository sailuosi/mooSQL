using HHNY.NET.Core;
using mooSQL.utils;
using SqlSugar;

namespace HHNY.NET.Application.Entity;

/// <summary>
/// 词条定义
/// </summary>
[SugarTable("hh_worddefine","词条定义")]
public class HHWordDefine :EntityOID
{
    #region 字段
    /// <summary>
    /// 
    /// </summary>
    [SugarColumn(ColumnName ="HH_WordDefineOID",IsIdentity = false, ColumnDescription = "", IsPrimaryKey = true, Length = 36)]
    public string HH_WordDefineOID { get; set; }
    
    /// <summary>
    /// 创建日期
    /// </summary>
    [SugarColumn(ColumnName ="SYS_Created",ColumnDescription = "创建日期")]
    public DateTime? SYS_Created { get; set; }
    
    /// <summary>
    /// 最后修改日期
    /// </summary>
    [SugarColumn(ColumnName ="SYS_LAST_UPD",ColumnDescription = "最后修改日期")]
    public DateTime? SYS_LAST_UPD { get; set; }
    
    /// <summary>
    /// 记录删除标记
    /// </summary>
    [SugarColumn(ColumnName ="SYS_Deleted",ColumnDescription = "记录删除标记")]
    public bool? SYS_Deleted { get; set; }
    
    /// <summary>
    /// 名称
    /// </summary>
    [SugarColumn(ColumnName ="Wd_Name",ColumnDescription = "名称", Length = 200)]
    public string? Wd_Name { get; set; }
    
    /// <summary>
    /// 编号
    /// </summary>
    [SugarColumn(ColumnName ="Wd_Code",ColumnDescription = "编号", Length = 50)]
    public string? Wd_Code { get; set; }
    
    /// <summary>
    /// 类型
    /// </summary>
    [SugarColumn(ColumnName ="Wd_Type",ColumnDescription = "类型", Length = 20)]
    public string? Wd_Type { get; set; }
    
    /// <summary>
    /// 解析参数
    /// </summary>
    [SugarColumn(ColumnName ="Wd_Para",ColumnDescription = "解析参数", Length = 500)]
    public string? Wd_Para { get; set; }
    
    /// <summary>
    /// 解析器
    /// </summary>
    [SugarColumn(ColumnName ="Wd_Parser",ColumnDescription = "解析器", Length = 200)]
    public string? Wd_Parser { get; set; }
    
    /// <summary>
    /// 序号
    /// </summary>
    [SugarColumn(ColumnName ="Wd_Idx",ColumnDescription = "序号")]
    public int? Wd_Idx { get; set; }
    
    /// <summary>
    /// 创建用户
    /// </summary>
    [SugarColumn(ColumnName ="SYS_CreatedBy",ColumnDescription = "创建用户", Length = 36)]
    public string? SYS_CreatedBy { get; set; }
    
    /// <summary>
    /// 授权用户
    /// </summary>
    [SugarColumn(ColumnName ="SYS_REPLACEMENT",ColumnDescription = "授权用户", Length = 36)]
    public string? SYS_REPLACEMENT { get; set; }
    
    /// <summary>
    /// 所属岗位
    /// </summary>
    [SugarColumn(ColumnName ="SYS_POSTN",ColumnDescription = "所属岗位", Length = 36)]
    public string? SYS_POSTN { get; set; }
    
    /// <summary>
    /// 所属部门
    /// </summary>
    [SugarColumn(ColumnName ="SYS_DIVISION",ColumnDescription = "所属部门", Length = 36)]
    public string? SYS_DIVISION { get; set; }
    
    /// <summary>
    /// 所属组织
    /// </summary>
    [SugarColumn(ColumnName ="SYS_ORG",ColumnDescription = "所属组织", Length = 36)]
    public string? SYS_ORG { get; set; }
    
    /// <summary>
    /// 最后修改用户
    /// </summary>
    [SugarColumn(ColumnName ="SYS_LAST_UPD_BY",ColumnDescription = "最后修改用户", Length = 36)]
    public string? SYS_LAST_UPD_BY { get; set; }

    
    [SugarColumn(ColumnName = "Wd_IsOn", ColumnDescription = "启用")]
    public bool? Wd_IsOn { get; set; }

    [SugarColumn(ColumnName = "Wd_Note", ColumnDescription = "说明")]
    public string? Wd_Note { get; set; }
    
    #endregion

    public int Insert() {
        var kit = DBCash.useSQL(0);

        var cc= kit.setTable("hh_worddefine")
            .set("HH_WordDefineOID", HH_WordDefineOID)
            .set("SYS_Created", SYS_Created)
            .set("SYS_LAST_UPD", SYS_LAST_UPD)
            .set("SYS_Deleted", SYS_Deleted)
            .set("Wd_Name", Wd_Name)
            .set("Wd_Code", Wd_Code)
            .set("Wd_Type", Wd_Type)
            .set("Wd_Para", Wd_Para)
            .set("Wd_Parser", Wd_Parser)
            .set("Wd_Idx", Wd_Idx)
            .set("SYS_CreatedBy", SYS_CreatedBy)
            .set("SYS_REPLACEMENT", SYS_REPLACEMENT)
            .set("SYS_POSTN", SYS_POSTN)
            .set("SYS_DIVISION", SYS_DIVISION)
            .set("SYS_ORG", SYS_ORG)
            .set("SYS_LAST_UPD_BY", SYS_LAST_UPD_BY)
            .set("Wd_IsOn", Wd_IsOn)
            .set("Wd_Note", Wd_Note)
            .doInsert();
        return cc;
    }

    public int Update()
    {
        var kit = DBCash.useSQL(0);

        var cc = kit.setTable("hh_worddefine")
            .set("SYS_LAST_UPD", SYS_LAST_UPD)
            .set("SYS_Deleted", SYS_Deleted)
            .set("Wd_Name", Wd_Name)
            .set("Wd_Code", Wd_Code)
            .set("Wd_Type", Wd_Type)
            .set("Wd_Para", Wd_Para)
            .set("Wd_Parser", Wd_Parser)
            .set("Wd_Idx", Wd_Idx)
            .set("SYS_LAST_UPD_BY", SYS_LAST_UPD_BY)
            .set("Wd_IsOn", Wd_IsOn)
            .set("Wd_Note", Wd_Note)
            .where("HH_WordDefineOID", HH_WordDefineOID)
            .doUpdate();
        return cc;
    }

    public int Remove() {
        var kit = DBCash.useSQL(0);

        var cc = kit.setTable("hh_worddefine")
            .where("HH_WordDefineOID", HH_WordDefineOID)
            .doDelete();
        return cc;
    }

    public int RemoveFake()
    {
        var kit = DBCash.useSQL(0);

        var cc = kit.setTable("hh_worddefine")
            .set("SYS_Deleted", true)
            .where("HH_WordDefineOID", HH_WordDefineOID)
            .doUpdate();
        return cc;
    }

    public int Save(UserManager user)
    {
        if (RegxUntils.isGUID(HH_WordDefineOID) == false) { 
            HH_WordDefineOID= Guid.NewGuid().ToString();
            //设置插入的系统字段
            setSysField(user,false);
            return Insert();
        }

        var kit = DBCash.useSQL(0);
        var cc=kit.from("hh_worddefine").where("HH_WordDefineOID", HH_WordDefineOID).count();
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
            SYS_Created = DateTime.Now;
            SYS_CreatedBy = user.UId;
            SYS_Deleted = false;
            SYS_DIVISION = user.DivOID;
            SYS_ORG = user.OrgOID;
            SYS_POSTN= user.PostOID;
            SYS_REPLACEMENT = user.UId;
        }
        SYS_LAST_UPD = DateTime.Now;
        SYS_LAST_UPD_BY = user.UId;
    }
}
