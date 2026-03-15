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

 
}
