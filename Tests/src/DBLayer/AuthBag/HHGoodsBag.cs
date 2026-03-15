using HHNY.NET.Core;
using mooSQL.utils;
using SqlSugar;

namespace HHNY.NET.Application.Entity;

/// <summary>
/// 资源集合
/// </summary>
[SugarTable("hh_goodsbag","资源集合")]
public class HHGoodsBag :EntityOID
{
    #region 字段
    /// <summary>
    /// 
    /// </summary>
    [SugarColumn(ColumnName ="HH_GoodsBagOID",IsIdentity = false, ColumnDescription = "", IsPrimaryKey = true, Length = 36)]
    public string HH_GoodsBagOID { get; set; }
    
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
    [SugarColumn(ColumnName ="Gb_Name",ColumnDescription = "名称", Length = 200)]
    public string? Gb_Name { get; set; }
    
    /// <summary>
    /// 编号
    /// </summary>
    [SugarColumn(ColumnName ="Gb_Code",ColumnDescription = "编号", Length = 200)]
    public string? Gb_Code { get; set; }
    
    /// <summary>
    /// 说明
    /// </summary>
    [SugarColumn(ColumnName ="Gb_Note",ColumnDescription = "说明", Length = 500)]
    public string? Gb_Note { get; set; }
    
    /// <summary>
    /// 备注
    /// </summary>
    [SugarColumn(ColumnName ="Gb_Remark",ColumnDescription = "备注", Length = 200)]
    public string? Gb_Remark { get; set; }
    
    /// <summary>
    /// 类型
    /// </summary>
    [SugarColumn(ColumnName ="Gb_Type",ColumnDescription = "类型", Length = 20)]
    public string? Gb_Type { get; set; }
    
    /// <summary>
    /// 序号
    /// </summary>
    [SugarColumn(ColumnName ="Gb_Idx",ColumnDescription = "序号")]
    public int? Gb_Idx { get; set; }
    
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
    


    #endregion

    public int Insert() {
        var kit = DBCash.useSQL(0);

        var cc= kit.setTable("hh_goodsbag")
            .set("HH_GoodsBagOID", HH_GoodsBagOID)
            .set("SYS_Created", SYS_Created)
            .set("SYS_LAST_UPD", SYS_LAST_UPD)
            .set("SYS_Deleted", SYS_Deleted)
            .set("Gb_Name", Gb_Name)
            .set("Gb_Code", Gb_Code)
            .set("Gb_Note", Gb_Note)
            .set("Gb_Remark", Gb_Remark)
            .set("Gb_Type", Gb_Type)
            .set("Gb_Idx", Gb_Idx)
            .set("SYS_CreatedBy", SYS_CreatedBy)
            .set("SYS_REPLACEMENT", SYS_REPLACEMENT)
            .set("SYS_POSTN", SYS_POSTN)
            .set("SYS_DIVISION", SYS_DIVISION)
            .set("SYS_ORG", SYS_ORG)
            .set("SYS_LAST_UPD_BY", SYS_LAST_UPD_BY)
            .doInsert();
        return cc;
    }

    public int Update()
    {
        var kit = DBCash.useSQL(0);

        var cc = kit.setTable("hh_goodsbag")
            .set("SYS_Created", SYS_Created)
            .set("SYS_LAST_UPD", SYS_LAST_UPD)
            .set("SYS_Deleted", SYS_Deleted)
            .set("Gb_Name", Gb_Name)
            .set("Gb_Code", Gb_Code)
            .set("Gb_Note", Gb_Note)
            .set("Gb_Remark", Gb_Remark)
            .set("Gb_Type", Gb_Type)
            .set("Gb_Idx", Gb_Idx)
            .set("SYS_CreatedBy", SYS_CreatedBy)
            .set("SYS_REPLACEMENT", SYS_REPLACEMENT)
            .set("SYS_POSTN", SYS_POSTN)
            .set("SYS_DIVISION", SYS_DIVISION)
            .set("SYS_ORG", SYS_ORG)
            .set("SYS_LAST_UPD_BY", SYS_LAST_UPD_BY)
            .where("HH_GoodsBagOID", HH_GoodsBagOID)
            .doUpdate();
        return cc;
    }

    public int Remove() {
        var kit = DBCash.useSQL(0);

        var cc = kit.setTable("hh_goodsbag")
            .where("HH_GoodsBagOID", HH_GoodsBagOID)
            .doDelete();
        return cc;
    }

    public int RemoveFake()
    {
        var kit = DBCash.useSQL(0);

        var cc = kit.setTable("hh_goodsbag")
            .set("SYS_Deleted", true)
            .where("HH_GoodsBagOID", HH_GoodsBagOID)
            .doUpdate();
        return cc;
    }

    public int Save(UserManager user)
    {
        if (RegxUntils.isGUID(HH_GoodsBagOID) == false) { 
            HH_GoodsBagOID= Guid.NewGuid().ToString();
            //设置插入的系统字段
            setSysField(user,false);
            return Insert();
        }

        var kit = DBCash.useSQL(0);
        var cc=kit.from("hh_goodsbag").where("HH_GoodsBagOID", HH_GoodsBagOID).count();
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
