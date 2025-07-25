using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data
{
    /// <summary>
    /// 固定格式的SQL生成器。主要为兼容老版本，以及部分冷门SQL设置
    /// </summary>
    public class SQLCreator
    {
        /// <summary>
        /// 生成父子关系的SQL语句，使用with as 递归处理。
        /// </summary>
        /// <param name="tableName"></param>
        /// <param name="keyCol"></param>
        /// <param name="parentCol"></param>
        /// <param name="otherCols"></param>
        /// <param name="baseWhere"></param>
        /// <param name="withAsName"></param>
        /// <param name="upToDown"></param>
        /// <returns></returns>
        public static string dealFatherSon(string tableName, string keyCol, string parentCol, string[] otherCols, string baseWhere, string withAsName, bool upToDown)
        {
            return dealFatherSon(tableName, keyCol, parentCol, otherCols, baseWhere, "", withAsName, upToDown);
        }
        /// <summary>
        /// 增加递归深度标记列 tDeepNum
        /// </summary>
        /// <param name="tableName"></param>
        /// <param name="keyCol"></param>
        /// <param name="parentCol"></param>
        /// <param name="otherCols"></param>
        /// <param name="baseWhere"></param>
        /// <param name="nextWhere"></param>
        /// <param name="withAsName"></param>
        /// <param name="upToDown"></param>
        /// <returns></returns>
        public static string dealFatherSon(string tableName, string keyCol, string parentCol, string[] otherCols, string baseWhere, string nextWhere, string withAsName, bool upToDown,string withtail="")
        {
            /* 生成如下格式的查询语句，即递归查询父或子节点数据
			             with tab as( 
			  select o.UCML_OrganizeOID as id,o.ParentOID as pid,o.OrgName from UCML_Organize as roo 
			  where o.UCML_OrganizeOID='0002EF74-0000-0000-0000-0000884F3996'
			  union all 
			  select b.UCML_OrganizeOID as id,b.ParentOID as pid,b.OrgName 
			  from  tab a,  UCML_Organize b  
			  where a.id=b.ParentOID
			)
			select * from tab
             */
            string rooCols = "";
            string pacols = "";
            if (otherCols.Length > 0)
            {
                foreach (var col in otherCols)
                {
                    if (col.Contains("{0}"))
                    {
                        rooCols += string.Format("," + col, "roo");
                        pacols += string.Format("," + col, "pa");
                    }
                    else
                    {
                        rooCols += string.Format(",roo.{0}", col);
                        pacols += string.Format(",pa.{0}", col);
                    }
                }
            }
            var joinPart = "";
            if (upToDown)
            {
                joinPart = string.Format("tark.{0}=pa.{1}", keyCol, parentCol);
            }
            else
            {
                joinPart = string.Format("tark.{0}=pa.{1}", parentCol, keyCol);
            }
            if (string.IsNullOrWhiteSpace(nextWhere) == false)
            {
                joinPart += " and " + nextWhere;
            }
            var sql = string.Format("with "+ withtail + " {7} as (" +
                " select 1 as tDeepNum,roo.{0},roo.{1} {2} from (select * from {3} where {4}) as roo " +
                " union all" +
                " select tark.tDeepNum+1 as tDeepNum,pa.{0},pa.{1} {5} from {7} as tark,{3} as pa where {6}" +
                ")", keyCol, parentCol, rooCols, tableName, baseWhere, pacols, joinPart, withAsName);
            return sql;

        }


        /// <summary>
        /// 生成行列转置的SQL语句。一个合格的pivot源数据表，列必须纯粹。除了统计列、结果列、共性列之外，不能有多余的列。尤其是统计列不能重复，否则会有大量冗余数据。
        /// </summary>
        /// <param name="keyColname"></param>
        /// <param name="valueCols"></param>
        /// <param name="valueSums"></param>
        /// <param name="commonCol"></param>
        /// <param name="fromPart"></param>
        /// <returns></returns>
        public static string dealPivot(string keyColname, string valueCols, string valueSums, string commonCol, string fromPart, DBInstance db)
        {
            var res = "";

            //尝试自行获取列值范围
            var ckSql = string.Format("select distinct {0} from {1} ", keyColname, fromPart);
            var ckDt = db.ExeQuery(ckSql);
            string keycol = "";

            for (int i = 0; i < ckDt.Rows.Count; i++)
            {
                var row = ckDt.Rows[i];
                if (keycol != "") { keycol += ","; }
                var colstr = row[0].ToString();
                if (colstr == "") { continue; }
                keycol += string.Format("[{0}]", row[0].ToString());
            }
            if (keycol == "")
            {
                throw new Exception("当前统计列没有数据或者列名为空！");
            }
            if (commonCol != "") { commonCol = "," + commonCol; }
            res = string.Format("select {0} {2} from ( select {1} as keycol {2},{3} from {5}) as a " +
                " pivot ( {4} for keycol in ({0}) ) as pv", keycol, keyColname, commonCol, valueCols, valueSums, fromPart);
            return res;
        }
        /// <summary>
        /// 生成行列转置的SQL语句。一个合格的pivot源数据表，列必须纯粹。除了统计列、结果列、共性列之外，不能有多余的列。尤其是统计列不能重复，否则会有大量冗余数据。
        /// </summary>
        /// <param name="keyColname"></param>
        /// <param name="keyScope"></param>
        /// <param name="valueCols"></param>
        /// <param name="valueSums"></param>
        /// <param name="commonCol"></param>
        /// <param name="fromPart"></param>
        /// <returns></returns>
        public static string dealPivot(string keyColname, string keyScope, string valueCols, string valueSums, string commonCol, string fromPart)
        {
            var res = "";
            if (commonCol != "") { commonCol = "," + commonCol; }
            res = string.Format("select {0} {2} from ( select {1} as keycol {2},{3} from {5}) as a " +
                " pivot ( {4} for keycol in ({0}) ) as pv", keyScope, keyColname, commonCol, valueCols, valueSums, fromPart);
            return res;
        }
    }
}
