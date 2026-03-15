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

    public int Insert() {
        var kit = DBCash.useSQL(0);

        var cc= kit.setTable("hh_goodsitem")
            .set("HH_GoodsItemOID", HH_GoodsItemOID)
            .set("Gi_Name", Gi_Name)
            .set("Gi_Code", Gi_Code)
            .set("Gi_Type", Gi_Type)
            .set("Gi_OID", Gi_OID)
            .set("HH_GoodsBag_FK", HH_GoodsBag_FK)
            .doInsert();
        return cc;
    }

    public int Update()
    {
        var kit = DBCash.useSQL(0);

        var cc = kit.setTable("hh_goodsitem")
            .set("Gi_Name", Gi_Name)
            .set("Gi_Code", Gi_Code)
            .set("Gi_Type", Gi_Type)
            .set("Gi_OID", Gi_OID)
            .set("HH_GoodsBag_FK", HH_GoodsBag_FK)
            .where("HH_GoodsItemOID", HH_GoodsItemOID)
            .doUpdate();
        return cc;
    }

    public int Remove() {
        var kit = DBCash.useSQL(0);

        var cc = kit.setTable("hh_goodsitem")
            .where("HH_GoodsItemOID", HH_GoodsItemOID)
            .doDelete();
        return cc;
    }

    public int RemoveFake()
    {
        var kit = DBCash.useSQL(0);

        var cc = kit.setTable("hh_goodsitem")
            .set("SYS_Deleted", true)
            .where("HH_GoodsItemOID", HH_GoodsItemOID)
            .doUpdate();
        return cc;
    }

    public int Save(UserManager user)
    {
        if (RegxUntils.isGUID(HH_GoodsItemOID) == false) { 
            HH_GoodsItemOID= Guid.NewGuid().ToString();
            return Insert();
        }

        var kit = DBCash.useSQL(0);
        var cc=kit.from("hh_goodsitem").where("HH_GoodsItemOID", HH_GoodsItemOID).count();
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
