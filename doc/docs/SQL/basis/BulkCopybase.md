# 利用 BulkCopy批量写入

注意：只有 sql server原生支持完整的高性能 bulkInsert,其它数据库会衰减为普通的批量插入。

````c#
var bt = DBCash.newBulk("C_geneflow",0);
foreach (JObject obj in arr) {
    bt.newRow();
    bt.add("C_geneflowOID", Guid.NewGuid());
    bt.add("f_workId", obj["workId"]);
    bt.add("c_bizflow_FK", obj["c_bizflow_FK"]);
    bt.add("f_bizPK", obj["bizPK"]);
    bt.add("f_bizId", obj["bizId"]);
    bt.add("f_state", obj["state"]);
    bt.add("remark", obj["remark"]);
    bt.add("f_msg", obj["msg"]);
    bt.add("f_nodeId", obj["nodeId"]);
    bt.add("f_taskCode", obj["taskCode"]);
    bt.add("f_assId", obj["assId"]);
    bt.add("f_flowNo", obj["flowNo"]);
    bt.add("f_curUsers", obj["curUsers"]);
    bt.addSysPart(loginUserInfo);
    bt.addRow();
}
var cc = bt.doInsert();
if (cc > 0) {
    return AjaxResult.success("保存成功！");
}
````