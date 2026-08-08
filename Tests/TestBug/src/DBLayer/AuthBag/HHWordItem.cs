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

   
}
