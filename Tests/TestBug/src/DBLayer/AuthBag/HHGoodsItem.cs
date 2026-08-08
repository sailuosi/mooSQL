using HHNY.NET.Core;
using mooSQL.utils;
using SqlSugar;

namespace HHNY.NET.Application.Entity;

/// <summary>
/// 资源项
/// </summary>
[SugarTable("hh_goodsitem","资源项")]
public class HHGoodsItem :EntityOID
{
    #region 字段
    /// <summary>
    /// 
    /// </summary>
    [SugarColumn(ColumnName ="HH_GoodsItemOID",IsIdentity = false, ColumnDescription = "", IsPrimaryKey = true, Length = 36)]
    public string HH_GoodsItemOID { get; set; }
    
    /// <summary>
    /// 名称
    /// </summary>
    [SugarColumn(ColumnName ="Gi_Name",ColumnDescription = "名称", Length = 50)]
    public string? Gi_Name { get; set; }
    
    /// <summary>
    /// 编号
    /// </summary>
    [SugarColumn(ColumnName ="Gi_Code",ColumnDescription = "编号", Length = 50)]
    public string? Gi_Code { get; set; }
    
    /// <summary>
    /// 类型
    /// </summary>
    [SugarColumn(ColumnName ="Gi_Type",ColumnDescription = "类型", Length = 50)]
    public string? Gi_Type { get; set; }
    
    /// <summary>
    /// OID
    /// </summary>
    [SugarColumn(ColumnName ="Gi_OID",ColumnDescription = "OID", Length = 36)]
    public string? Gi_OID { get; set; }
    
    /// <summary>
    /// 资源集合
    /// </summary>
    [SugarColumn(ColumnName ="HH_GoodsBag_FK",ColumnDescription = "资源集合", Length = 36)]
    public string? HH_GoodsBag_FK { get; set; }
    


    #endregion

   
}
