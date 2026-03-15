using HHNY.NET.Core;
using mooSQL.utils;
using SqlSugar;

namespace HHNY.NET.Application.Entity;

/// <summary>
/// 访客集合
/// </summary>
[SugarTable("hh_visitorbag","访客集合")]
public class HHVisitorBag :EntityOID
{
    #region 字段
    /// <summary>
    /// 
    /// </summary>
    [SugarColumn(ColumnName ="HH_VisitorBagOID",IsIdentity = false, ColumnDescription = "", IsPrimaryKey = true, Length = 36)]
    public string HH_VisitorBagOID { get; set; }
    
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
    [SugarColumn(ColumnName ="Vb_Name",ColumnDescription = "名称", Length = 200)]
    public string? Vb_Name { get; set; }
    
    /// <summary>
    /// 描述
    /// </summary>
    [SugarColumn(ColumnName ="Vb_Note",ColumnDescription = "描述", Length = 500)]
    public string? Vb_Note { get; set; }
    
    /// <summary>
    /// 编号
    /// </summary>
    [SugarColumn(ColumnName ="Vb_Code",ColumnDescription = "编号", Length = 200)]
    public string? Vb_Code { get; set; }
    
    /// <summary>
    /// 备注
    /// </summary>
    [SugarColumn(ColumnName ="Vb_Remark",ColumnDescription = "备注", Length = 200)]
    public string? Vb_Remark { get; set; }
    
    /// <summary>
    /// 类型
    /// </summary>
    [SugarColumn(ColumnName ="Vb_Type",ColumnDescription = "类型", Length = 20)]
    public string? Vb_Type { get; set; }
    
    /// <summary>
    /// 关键成员
    /// </summary>
    [SugarColumn(ColumnName ="Vb_KeyWord",ColumnDescription = "关键成员", Length = 500)]
    public string? Vb_KeyWord { get; set; }
    
    /// <summary>
    /// 序号
    /// </summary>
    [SugarColumn(ColumnName ="Vb_Idx",ColumnDescription = "序号")]
    public int? Vb_Idx { get; set; }
    
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
    
    /// <summary>
    /// 父级访客集合
    /// </summary>
    [SugarColumn(ColumnName ="HH_Parent_FK",ColumnDescription = "父级访客集合", Length = 36)]
    public string? HH_Parent_FK { get; set; }
    


    #endregion

    public int Insert() {
        var kit = DBCash.useSQL(0);

        var cc= kit.setTable("hh_visitorbag")
            .set("HH_VisitorBagOID", HH_VisitorBagOID)
            .set("SYS_Created", SYS_Created)
            .set("SYS_LAST_UPD", SYS_LAST_UPD)
            .set("SYS_Deleted", SYS_Deleted)
            .set("Vb_Name", Vb_Name)
            .set("Vb_Note", Vb_Note)
            .set("Vb_Code", Vb_Code)
            .set("Vb_Remark", Vb_Remark)
            .set("Vb_Type", Vb_Type)
            .set("Vb_KeyWord", Vb_KeyWord)
            .set("Vb_Idx", Vb_Idx)
            .set("SYS_CreatedBy", SYS_CreatedBy)
            .set("SYS_REPLACEMENT", SYS_REPLACEMENT)
            .set("SYS_POSTN", SYS_POSTN)
            .set("SYS_DIVISION", SYS_DIVISION)
            .set("SYS_ORG", SYS_ORG)
            .set("SYS_LAST_UPD_BY", SYS_LAST_UPD_BY)
            .set("HH_Parent_FK", HH_Parent_FK)
            .doInsert();
        return cc;
    }

    public int Update()
    {
        var kit = DBCash.useSQL(0);

        var cc = kit.setTable("hh_visitorbag")
            .set("SYS_Created", SYS_Created)
            .set("SYS_LAST_UPD", SYS_LAST_UPD)
            .set("SYS_Deleted", SYS_Deleted)
            .set("Vb_Name", Vb_Name)
            .set("Vb_Note", Vb_Note)
            .set("Vb_Code", Vb_Code)
            .set("Vb_Remark", Vb_Remark)
            .set("Vb_Type", Vb_Type)
            .set("Vb_KeyWord", Vb_KeyWord)
            .set("Vb_Idx", Vb_Idx)
            .set("SYS_CreatedBy", SYS_CreatedBy)
            .set("SYS_REPLACEMENT", SYS_REPLACEMENT)
            .set("SYS_POSTN", SYS_POSTN)
            .set("SYS_DIVISION", SYS_DIVISION)
            .set("SYS_ORG", SYS_ORG)
            .set("SYS_LAST_UPD_BY", SYS_LAST_UPD_BY)
            .set("HH_Parent_FK", HH_Parent_FK)
            .where("HH_VisitorBagOID", HH_VisitorBagOID)
            .doUpdate();
        return cc;
    }

    public int Remove() {
        var kit = DBCash.useSQL(0);

        var cc = kit.setTable("hh_visitorbag")
            .where("HH_VisitorBagOID", HH_VisitorBagOID)
            .doDelete();
        return cc;
    }

    public int RemoveFake()
    {
        var kit = DBCash.useSQL(0);

        var cc = kit.setTable("hh_visitorbag")
            .set("SYS_Deleted", true)
            .where("HH_VisitorBagOID", HH_VisitorBagOID)
            .doUpdate();
        return cc;
    }

    public int Save(UserManager user)
    {
        if (RegxUntils.isGUID(HH_VisitorBagOID) == false) { 
            HH_VisitorBagOID= Guid.NewGuid().ToString();
            //设置插入的系统字段
            setSysField(user,false);
            return Insert();
        }

        var kit = DBCash.useSQL(0);
        var cc=kit.from("hh_visitorbag").where("HH_VisitorBagOID", HH_VisitorBagOID).count();
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
