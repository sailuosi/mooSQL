using HHNY.NET.Core;
using mooSQL.utils;
using SqlSugar;

namespace HHNY.NET.Application.Entity;

/// <summary>
/// 访客项
/// </summary>
[SugarTable("hh_visitoritem","访客项")]
public class HHVisitorItem :EntityOID
{
    #region 字段
    /// <summary>
    /// 
    /// </summary>
    [SugarColumn(ColumnName ="HH_VisitorItemOID",IsIdentity = false, ColumnDescription = "", IsPrimaryKey = true, Length = 36)]
    public string HH_VisitorItemOID { get; set; }
    
    /// <summary>
    /// 启用
    /// </summary>
    [SugarColumn(ColumnName ="Vi_IsOn",ColumnDescription = "启用")]
    public bool? Vi_IsOn { get; set; }
    
    /// <summary>
    /// 类型
    /// </summary>
    [SugarColumn(ColumnName ="Vi_Type",ColumnDescription = "类型", Length = 20)]
    public string? Vi_Type { get; set; }
    
    /// <summary>
    /// 名称
    /// </summary>
    [SugarColumn(ColumnName ="Vi_Name",ColumnDescription = "名称", Length = 500)]
    public string? Vi_Name { get; set; }
    
    /// <summary>
    /// 编号
    /// </summary>
    [SugarColumn(ColumnName ="Vi_Code",ColumnDescription = "编号", Length = 200)]
    public string? Vi_Code { get; set; }
    
    /// <summary>
    /// OID
    /// </summary>
    [SugarColumn(ColumnName ="Vi_OID",ColumnDescription = "OID", Length = 50)]
    public string? Vi_OID { get; set; }
    
    /// <summary>
    /// 来源
    /// </summary>
    [SugarColumn(ColumnName ="Vi_Src",ColumnDescription = "来源", Length = 20)]
    public string? Vi_Src { get; set; }
    
    /// <summary>
    /// 方式
    /// </summary>
    [SugarColumn(ColumnName ="Vi_Mode",ColumnDescription = "方式", Length = 50)]
    public string? Vi_Mode { get; set; }
    
    /// <summary>
    /// 序号
    /// </summary>
    [SugarColumn(ColumnName ="Vi_Idx",ColumnDescription = "序号")]
    public int? Vi_Idx { get; set; }
    
    /// <summary>
    /// 访客集合
    /// </summary>
    [SugarColumn(ColumnName ="HH_VisitorBag_FK",ColumnDescription = "访客集合", Length = 36)]
    public string? HH_VisitorBag_FK { get; set; }
    


    #endregion

 
}
