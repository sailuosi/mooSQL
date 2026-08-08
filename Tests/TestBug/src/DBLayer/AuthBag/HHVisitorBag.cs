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

 
}
