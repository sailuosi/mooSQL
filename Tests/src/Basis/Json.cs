// 基础功能说明：

using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HHNY.NET.Core;

public class Json
{

    #region 数据处理小工具

    #endregion
    public static JObject getObj()
    {
        return new JObject();
    }
    public static JArray getArray()
    {
        return new JArray();
    }
    public static string JArrayJoin(Newtonsoft.Json.Linq.JArray arr, string sep)
    {
        var res = new System.Text.StringBuilder();
        if (arr == null)
        {
            return "";
        }
        foreach (var a in arr)
        {
            if (res.Length > 0)
            {
                res.Append(sep);
            }
            res.Append(a.ToString());
        }
        return res.ToString();
    }
    public static string toJson(Object dt)
    {
        return JsonConvert.SerializeObject(dt);
    }

    public static JArray ListToJArray(List<string> list)
    {
        var res = new JArray();
        foreach (var li in list)
        {
            res.Add(li);
        }
        return res;
    }
    public class treeOption
    {
        public string children = "children";
        public string childcount = "childcount";
        public string isleaf = "isleaf";
        public string treeDeep = "treeDeep";

        //计算子节点数的字段名
        public string childcountField = "";
        //当节点为空时，是否附加属性。
        public bool canEmpty = true;
        public string keyColName;
        public string parentColName;
        public treeOption() { }
    }
    public class myNode
    {
        public myNode(string id, string fk)
        {
            this.Id = id;
            this.fk = fk;
        }
        public JObject myvalue = new JObject();
        public string Id;
        public string fk;
        public int mylevel = 1;
        public List<myNode> child = new List<myNode>();
        public JObject getValue()
        {
            var val = this.myvalue;
            val["treeDeep"] = mylevel;
            var cds = new JArray();
            foreach (var kv in this.child)
            {
                kv.mylevel = mylevel + 1;
                cds.Add(kv.getValue());
            }
            val["children"] = cds;

            //val["childcount"] = cds.Count;
            //val["isleaf"] = cds.Count == 0;
            return val;
        }
        public JObject getValue(treeOption opt)
        {
            var val = this.myvalue;
            val[opt.treeDeep] = mylevel;
            var cds = new JArray();
            foreach (var kv in this.child)
            {
                kv.mylevel = mylevel + 1;
                cds.Add(kv.getValue(opt));
            }
            if (opt.canEmpty || cds.Count > 0)
            {
                val[opt.children] = cds;
            }
            else
            {
                val.Remove(opt.children);
            }
            if (!string.IsNullOrWhiteSpace(opt.childcountField))
            {
                var tar = val[opt.childcountField];
                if (tar != null)
                {
                    val[opt.childcount] = tar;
                    if (tar.ToString().Trim() == "0")
                    {
                        val[opt.isleaf] = true;
                    }
                    else
                    {
                        val[opt.isleaf] = false;
                    }
                }
            }
            return val;
        }
    }
    public static string getJobjValueOrEmpty(JToken obj, string key)
    {
        var res = string.Empty;
        if (obj[key] != null)
        {
            res = obj[key].ToString();
        }
        return res;
    }


    public static JObject MapToJobect<T>(Dictionary<string, T> map)
    {
        var res = new JObject();
        foreach (var kv in map)
        {
            var val = kv.Value as JToken;
            if (val != null)
            {
                res[kv.Key] = val;
            }
            else
            {
                res[kv.Key] = kv.Value.ToString();
            }

        }
        return res;
    }


    public static JArray decodeJarr(string jsonstr)
    {
        try
        {
            var res = (JArray)JsonConvert.DeserializeObject(jsonstr);
            return res;
        }
        catch (Exception e)
        {
            return null;
        }
    }
    public static JObject decodeJobj(string jsonstr)
    {
        try
        {
            var res = (JObject)JsonConvert.DeserializeObject(jsonstr);
            return res;
        }
        catch (Exception e)
        {
            return null;
        }
    }
    #region 将dataTable数据结构化
    public static JObject RowToJobj(DataRow row, List<string> cols)
    {
        var res = new JObject();
        foreach (var li in cols)
        {
            res[li] = row[li].ToString();
        }
        return res;
    }
    public static JObject RowToJobj(DataRow row)
    {
        var res = new JObject();
        if (row == null) {
            return res;
        }
        var cols = row.Table.Columns;
        foreach (DataColumn li in cols)
        {
            var name = li.ColumnName;
            res[name] = row[name].ToString();
        }
        return res;
    }

    public static JObject RowToJobj(DataRow row,JObject obj)
    {

        var cols = row.Table.Columns;
        foreach (DataColumn li in cols)
        {
            var name = li.ColumnName;
            obj[name] = row[name].ToString();
        }
        return obj;
    }




    public static JArray DtToJArray(DataTable dt)
        {
            return (JArray)JsonConvert.DeserializeObject(JsonConvert.SerializeObject(dt));
        }
        #endregion
        /// <summary>
        /// 根据某个字段的值，将结果归类成2级结构。
        /// </summary>
        /// <param name="dt"></param>
        /// <param name="fieldName"></param>
        /// <returns></returns>
        public static JArray makeGroupByField(DataTable dt, string fieldName)
        {
            //共享字段值--行集合
            var map = GroupByFieldToMap(dt, fieldName);
            var resJarr = new JArray();

            //将结果转为数组
            foreach (var kv in map)
            {
                var li = new JObject();
                li["value"] = kv.Key;
                li["children"] = kv.Value;
                resJarr.Add(li);
            }
            return resJarr;
        }
        public static JObject GroupByFieldToObj(DataTable dt, string fieldName)
        {
            //共享字段值--行集合
            var map = GroupByFieldToMap(dt, fieldName);
            var res = new JObject();

            //将结果转为数组
            foreach (var kv in map)
            {
                res[kv.Key] = kv.Value;
            }
            return res;
        }
        public static Dictionary<string, JArray> GroupByFieldToMap(DataTable dt, string fieldName)
        {
            //共享字段值--行集合
            var map = new Dictionary<string, JArray>();
            var dtJarr = (JArray)JsonConvert.DeserializeObject(JsonConvert.SerializeObject(dt));
            var resJarr = new JArray();
            foreach (var o in dtJarr)
            {
                var val = o[fieldName].ToString();
                if (!map.ContainsKey(val))
                {
                    map.Add(val, new JArray());
                }
                map[val].Add(o);
            }
            return map;
        }
        /// <summary>
        /// 根据某个字段的值，将结果归类成2级结构。
        /// </summary>
        /// <param name="dt"></param>
        /// <param name="fieldName"></param>
        /// <returns></returns>
        public static JArray makeGroupByField(DataTable dt, string fieldName, string valueKey, string groupKey)
        {
            //共享字段值--行集合
            var map = GroupByFieldToMap(dt, fieldName);
            //将结果转为数组
            var resJarr = new JArray();
            foreach (var kv in map)
            {
                var li = new JObject();
                li[valueKey] = kv.Key;
                li[groupKey] = kv.Value;
                resJarr.Add(li);
            }
            return resJarr;
        }
        public static JArray makeJsonTreeByCode(DataTable dt, string oidColname, string CodeColname)
        {
            /*给定一个数据库的自外键关联表数据，创建树结构的结果
             * 核心逻辑，使用dictionary存储指针，自根开始对表数据进行搜索。
             */
            //为所有记录创建好map
            var map = new Dictionary<string, JObject>();
            var dtJarr = (JArray)JsonConvert.DeserializeObject(JsonConvert.SerializeObject(dt));
            var resJarr = new JArray();
            var rootoids = new List<string>();
            var nodemap = new Dictionary<string, myNode>();
            foreach (var o in dtJarr)
            {
                var key = o[oidColname].ToString();
                var val = (JObject)o;
                val["children"] = new JArray();
                var code = "";
                if (o[CodeColname] != null)
                {
                    code = o[CodeColname].ToString();
                }
                var node = new myNode(key, code);
                node.myvalue = val;
                if (!nodemap.ContainsKey(key))
                {
                    nodemap.Add(key, node);
                    map.Add(key, val);
                }

            }

            //再一次遍历，执行父节点挂载。
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                var myid = dt.Rows[i][oidColname].ToString();
                bool finded = false;
                if (dt.Rows[i][CodeColname] != DBNull.Value)
                {
                    var code = dt.Rows[i][CodeColname].ToString();
                    //通过当前节点的层次码，去寻找父级节点的层次码，再定位父节点id.
                    //substring 总是报错Substring()参数超出范围。改为循环获取
                    //var filt = string.Format("LEN({0})<{2} and {0}= SUBSTRING('{1}',0,LEN({0}))", CodeColname,code,code.Length);
                    var filt = string.Format("LEN({0})<{1}", CodeColname, code.Length);
                    var filtRows = dt.Select(filt, CodeColname + " desc");
                    if (filtRows.Length > 0)
                    {
                        //循环判断
                        for (int r = 0; r < filtRows.Length; r++)
                        {
                            var rowcode = filtRows[r][CodeColname].ToString();
                            if (code.Substring(0, rowcode.Length) == rowcode)
                            {
                                var poid = filtRows[r][oidColname].ToString();
                                if (map.ContainsKey(poid))
                                {
                                    nodemap[poid].child.Add(nodemap[myid]);
                                    finded = true;
                                    break;
                                }
                            }
                        }


                    }

                }
                if (!finded)
                {
                    rootoids.Add(myid);
                }
            }
            //搜寻根节点，放入结果
            foreach (var li in rootoids)
            {
                //resJarr.Add(map[li]);
                var node = nodemap[li];
                resJarr.Add(node.getValue());
            }
            return resJarr;
        }

        public static JArray makeJsonTree(DataRow[] rows, string keyColname, string parentColname)
        {
            /*给定一个数据库的自外键关联表数据，创建树结构的结果
             * 核心逻辑，使用dictionary存储指针，自根开始对表数据进行搜索。
             */
            //为所有记录创建好map
            //var map = new Dictionary<string, JObject>();
            var resJarr = new JArray();
            if (rows.Length == 0)
            {
                return resJarr;
            }
            var rowarr = RowsToJarray(rows);
            var opt = new treeOption();
            opt.keyColName = keyColname;
            opt.parentColName = parentColname;
            resJarr = makeJsonTreeInner(rowarr, opt);
            return resJarr;
        }

        private static JArray makeJsonTreeInner(JArray dtJarr, treeOption opt)
        {
            var map = new Dictionary<string, JObject>();
            var resJarr = new JArray();
            var rootoids = new List<string>();
            var nodemap = new Dictionary<string, myNode>();
            foreach (var o in dtJarr)
            {
                var key = o[opt.keyColName].ToString();
                var val = (JObject)o;
                val["children"] = new JArray();
                var fk = "";
                if (o[opt.parentColName] != null)
                {
                    fk = o[opt.parentColName].ToString();
                }
                var node = new myNode(key, fk);
                node.myvalue = val;
                nodemap.Add(key, node);
                map.Add(key, val);
            }

            //再一次遍历，执行父节点挂载。
            foreach (var o in dtJarr)
            {
                var myid = o[opt.keyColName].ToString();
                if (o[opt.parentColName] != null)
                {
                    var fk = o[opt.parentColName].ToString();
                    if (map.ContainsKey(fk))
                    {
                        nodemap[fk].child.Add(nodemap[myid]);
                    }
                    else
                    {
                        rootoids.Add(myid);
                    }
                }
                else
                {
                    rootoids.Add(myid);
                }
            }
            //搜寻根节点，放入结果
            foreach (var li in rootoids)
            {
                //resJarr.Add(map[li]);
                var node = nodemap[li];
                node.mylevel = 1;
                resJarr.Add(node.getValue(opt));
            }
            return resJarr;
        }
        public static JArray makeJsonTree(DataTable dt, treeOption opt)
        {
            return makeJsonTreeInner(DtToJArray(dt), opt);
        }
        public static JArray makeJsonTree(DataTable dt, string keyColname, string parentColname)
        {
            /*给定一个数据库的自外键关联表数据，创建树结构的结果
             * 核心逻辑，使用dictionary存储指针，自根开始对表数据进行搜索。
             */
            //为所有记录创建好map
            var resJarr = new JArray();
            if (dt.Rows.Count == 0)
            {
                return resJarr;
            }
            var rowarr = DtToJArray(dt);
            var opt = new treeOption();
            opt.keyColName = keyColname;
            opt.parentColName = parentColname;
            resJarr = makeJsonTreeInner(rowarr, opt);
            return resJarr;
        }
        public class Slave
        {
            public string bossKey;
            public string slaveFK;
            public string childKey;
            public DataTable data;
            public Slave(DataTable slave, string bossKey, string slaveFK, string childKey)
            {
                this.data = slave;
                this.bossKey = bossKey;
                this.slaveFK = slaveFK;
                this.childKey = childKey;
            }
        }
        public static JArray getBossSlave(DataTable boss, List<Slave> slaves)
        {
            var resJarr = new JArray();
            var bossArr = DtToJArray(boss);

            foreach (var li in bossArr)
            {
                foreach (var slave in slaves)
                {
                    var id = li[slave.bossKey];
                    var sl = slave.data.Select(string.Format("{0}='{1}'", slave.slaveFK, id));
                    var slarr = RowsToJarray(sl);
                    li[slave.childKey] = slarr;
                }
            }
            return bossArr;
        }
        /// <summary>
        /// 根据主子表的数据，以及主外键关系，创建JSON格式的数据
        /// </summary>
        /// <param name="boss"></param>
        /// <param name="slave"></param>
        /// <param name="bossKey"></param>
        /// <param name="slaveFK"></param>
        /// <returns></returns>
        public static JArray getBossSlave(DataTable boss, DataTable slave, string bossKey, string slaveFK, string childKey)
        {
            var resJarr = new JArray();
            var bossArr = DtToJArray(boss);
            if (string.IsNullOrWhiteSpace(childKey))
            {
                childKey = "children";
            }
            if (slave == null) return bossArr;
            foreach (var li in bossArr)
            {
                var id = li[bossKey];
                var sl = slave.Select(string.Format("{0}='{1}'", slaveFK, id));
                var slarr = RowsToJarray(sl);
                li[childKey] = slarr;
            }
            return bossArr;
        }
        public static JArray RowsToJarray(DataRow[] rows)
        {
            var resJarr = new JArray();
            if (rows.Length == 0)
            {
                return resJarr;
            }
            foreach (var row in rows)
            {
                resJarr.Add(RowToJobj(row));
            }
            return resJarr;
            //以下方法返回的值是全datable的值，不是筛选出的值。
            //var rowarr = (JArray)JsonConvert.DeserializeObject(JsonConvert.SerializeObject(rows));
            //return rowarr[0]["Table"] as JArray;
        }
        public static JArray RowsToJarray(List<DataRow> rows)
        {
            var resJarr = new JArray();
            if (rows==null|| rows.Count == 0)
            {
                return resJarr;
            }
            foreach (var row in rows)
            {
                resJarr.Add(RowToJobj(row));
            }
            return resJarr;
            //以下方法返回的值是全datable的值，不是筛选出的值。
            //var rowarr = (JArray)JsonConvert.DeserializeObject(JsonConvert.SerializeObject(rows));
            //return rowarr[0]["Table"] as JArray;
        }

    /// <summary>
    /// 把一个json转化一个datatable
    /// </summary>
    /// <param name="json">一个json字符串</param>
    /// <returns>序列化的datatable</returns>
    public static DataTable ToDataTable(string strJson)
        {
            if (strJson.Trim().IndexOf('[') != 0)
            {
                strJson = "[" + strJson + "]";
            }
            strJson = strJson.Replace("\\\\\"", "'");
            DataTable dtt = (DataTable)JsonConvert.DeserializeObject<DataTable>(strJson);
            return dtt;
        }
        /// <summary>
        /// 把一个json转化一个datatable 杨玉慧
        /// </summary>
        /// <param name="json">一个json字符串</param>
        /// <returns>序列化的datatable</returns>
        public static DataTable ToDataTableOneRow(string strJson)
        {
            ////杨玉慧  写  用这个写
            if (strJson.Trim().IndexOf('[') != 0)
            {
                strJson = "[" + strJson + "]";
            }
            DataTable dtt = (DataTable)JsonConvert.DeserializeObject<DataTable>(strJson);
            return dtt;
        }
        /// <summary>
        /// 把dataset转成json 不区分大小写.
        /// </summary>
        /// <param name="dataSet"></param>
        /// <returns></returns>
        public static string DataSetToJson(DataSet dataSet, bool isUpperColumn = true)
        {
            string jsonString = "{";
            foreach (DataTable table in dataSet.Tables)
            {
                if (isUpperColumn == true)
                    jsonString += "\"" + table.TableName + "\":" + DataTableToJson(table, true) + ",";
                else
                    jsonString += "\"" + table.TableName + "\":" + DataTableToJson(table, false) + ",";
            }
            jsonString = jsonString.TrimEnd(',');
            return jsonString + "}";
        }
        /// <summary> 
        /// Datatable转换为Json 
        /// </summary> 
        /// <param name="table">Datatable对象</param> 
        /// <returns>Json字符串</returns> 
        public static string DataTableToJson(DataTable dt, bool isUpperColumn = true)
        {
            StringBuilder jsonString = new StringBuilder();
            if (dt.Rows.Count == 0)
            {
                jsonString.Append("[]");
                return jsonString.ToString();
            }

            jsonString.Append("[");
            DataRowCollection drc = dt.Rows;
            for (int i = 0; i < drc.Count; i++)
            {
                jsonString.Append("{");
                for (int j = 0; j < dt.Columns.Count; j++)
                {
                    string strKey = null;
                    if (isUpperColumn == true)
                    {
                        strKey = dt.Columns[j].ColumnName;
                    }
                    else
                    {
                        strKey = dt.Columns[j].ColumnName;
                    }

                    Type type = dt.Columns[j].DataType;

                    string strValue = "";
                    if (type == typeof(Single))
                    {
                        object v = drc[i][j];

                        if (v == null || v == DBNull.Value)
                        {
                            strValue = "0";
                        }
                        else
                        {
                            //double f =(double)((float)v);
                            strValue = ((float)v).ToString("0." + new string('#', 339));
                        }

                        //strValue = v == null ? "0" : v;
                        //strValue = drc[i][j] == null ? "" : ((float)(drc[i][j])).ToString("0.00");

                    }
                    else
                    {
                        strValue = drc[i][j] == null ? "" : drc[i][j].ToString();
                    }


                    jsonString.Append("\"" + strKey + "\":");
                    strValue = StringFormat(strValue, type);
                    if (j < dt.Columns.Count - 1)
                    {
                        jsonString.Append(strValue + ",");
                    }
                    else
                    {
                        jsonString.Append(strValue);
                    }
                }
                jsonString.Append("},");
            }
            jsonString.Remove(jsonString.Length - 1, 1);
            jsonString.Append("]");
            return jsonString.ToString();
        }
        /// <summary> 
        /// 格式化字符型、日期型、布尔型 
        /// </summary> 
        /// <param name="str"></param> 
        /// <param name="type"></param> 
        /// <returns></returns> 
        private static string StringFormat(string str, Type type)
        {
            if (type == typeof(string))
            {
                str = String2Json(str);
                str = "\"" + str + "\"";
            }
            else if (type == typeof(DateTime))
            {
                str = "\"" + Convert.ToDateTime(str).ToShortDateString() + "\"";
            }
            else if (type == typeof(bool))
            {
                str = str.ToLower();
            }

            if (str.Length == 0)
                str = "\"\"";

            return str;
        }
        /// <summary> 
        /// 过滤特殊字符 
        /// </summary> 
        /// <param name="s"></param> 
        /// <returns></returns> 
        private static string String2Json(String s)
        {
            System.Text.StringBuilder sb = new StringBuilder();
            for (int i = 0; i < s.Length; i++)
            {
                char c = s.ToCharArray()[i];

                switch (c)
                {
                    case '\"':
                        sb.Append("\\\""); break;
                    case '\\':
                        sb.Append("\\\\"); break;
                    case '/':
                        sb.Append("\\/"); break;
                    case '\b':
                        sb.Append("\\b"); break;
                    case '\f':
                        sb.Append("\\f"); break;
                    case '\n':
                        sb.Append("\\n"); break;
                    case '\r':
                        sb.Append("\\r"); break;
                    case '\t':
                        sb.Append("\\t"); break;
                    default:
                        sb.Append(c); break;
                }
            }
            return sb.ToString();
        }
        /// <summary>
        /// 把一个json转化一个datatable
        /// </summary>
        /// <param name="json">一个json字符串</param>
        /// <returns>序列化的datatable</returns>
        public static DataSet ToDataSet(string json)
        {
            ////杨玉慧  写  用这个写
            DataSet ds = JsonConvert.DeserializeObject<DataSet>(json);
            return ds;
        }
        /// <summary>
        /// 对象转换为Json字符串
        /// </summary>
        /// <param name="jsonObject">对象</param>
        /// <returns>Json字符串</returns>
        public static string ToJson(object jsonObject)
        {
            string json = JsonConvert.SerializeObject(jsonObject, Formatting.Indented);
            return json;
        }
        /// <summary>
        /// 对象集合转换Json
        /// </summary>
        /// <param name="array">集合对象</param>
        /// <returns>Json字符串</returns>
        public static string ToJson(IEnumerable array)
        {
            string jsonStr = JsonConvert.SerializeObject(array);
            return jsonStr;
        }
        /// <summary>
        /// 普通集合转换Json
        /// </summary>
        /// <param name="array">集合对象</param>
        /// <returns>Json字符串</returns>
        public static string ToArrayString(IEnumerable array)
        {
            string jsonStr = JsonConvert.SerializeObject(array);
            return jsonStr;
        }
        /// <summary>
        /// 删除结尾字符
        /// </summary>
        /// <param name="str">需要删除的字符</param>
        /// <returns>完成后的字符串</returns>
        private static string DeleteLast(string str)
        {
            if (str.Length > 1)
            {
                return str.Substring(0, str.Length - 1);
            }
            return str;
        }
        /// <summary>
        /// 把Ht转换成Entity模式.
        /// </summary>
        /// <param name="ht"></param>
        /// <returns></returns>
        public static string ToJsonEntityModel(Hashtable ht)
        {
            string strs = "{";
            foreach (string key in ht.Keys)
            {
                var val = ht[key];
                if (val == null)
                {
                    strs += "\"" + key + "\":\"\",";
                    continue;
                }

                var tp = val.GetType();
                if (tp == typeof(int)
                    || tp == typeof(float)
                    || tp == typeof(decimal)
                    || tp == typeof(double)
                    || tp == typeof(Int64))
                {
                    strs += "\"" + key + "\":" + ht[key] + ",";
                }
                else
                {
                    strs += "\"" + key + "\":\"" + ht[key].ToString().Replace("\"", "\\\"") + "\",";
                }
            }
            strs += "\"EndJSON\":\"0\"";
            strs += "}";
            strs = TranJsonStr(strs);
            return strs;
        }
        public static string ToJsonEntitiesNoNameMode(Hashtable ht)
        {
            DataTable dt = new DataTable();
            dt.Columns.Add("No");
            dt.Columns.Add("Name");

            foreach (string key in ht.Keys)
            {
                DataRow dr = dt.NewRow();
                dr["No"] = key;
                dr["Name"] = ht[key];
                dt.Rows.Add(dr);

            }

            return DataTableToJson(dt, false);
        }
        /// <summary>
        /// 转化成Json.
        /// </summary>
        /// <param name="ht">Hashtable</param>
        /// <param name="isNoNameFormat">是否编号名称格式</param>
        /// <returns></returns>
        public static string ToJson(Hashtable ht)
        {
            return ToJsonEntityModel(ht);
        }
        /// <summary>
        /// Datatable转换为Json
        /// </summary>
        /// <param name="table">Datatable对象</param>
        /// <returns>Json字符串</returns>
        public static string ToJson(DataTable table)
        {
            // 旧版本...
            return JsonConvert.SerializeObject(table);
        }
        /// <summary>
        /// DataSet转换为Json
        /// </summary>
        /// <param name="dataSet">DataSet对象</param>
        /// <returns>Json字符串</returns>
        public static string ToJson(DataSet dataSet)
        {
            return JsonConvert.SerializeObject(dataSet);
        }
        /// <summary>
        /// String转换为Json
        /// </summary>
        /// <param name="value">String对象</param>
        /// <returns>Json字符串</returns>
        public static string ToJson(string value)
        {
            if (string.IsNullOrEmpty(value))
                return string.Empty;

            string temstr;
            temstr = value;
            temstr = temstr.Replace("{", "｛").Replace("}", "｝").Replace(":", "：").Replace(",", "，").Replace("[", "【").Replace("]", "】").Replace(";", "；").Replace("\n", "<br/>").Replace("\r", "");
            temstr = temstr.Replace("\t", "   ");
            temstr = temstr.Replace("'", "\'");
            temstr = temstr.Replace(@"\", @"\\");
            temstr = temstr.Replace("\"", "\"\"");
            return temstr;
        }
        /// <summary>
        /// JSON字符串的转义
        /// </summary>
        /// <param name="jsonStr"></param>
        /// <returns></returns>
        private static string TranJsonStr(string jsonStr)
        {
            string strs = jsonStr;
            strs = strs.Replace("\\", "\\\\");
            strs = strs.Replace("\n", "\\n");
            strs = strs.Replace("\b", "\\b");
            strs = strs.Replace("\t", "\\t");
            strs = strs.Replace("\f", "\\f");
            strs = strs.Replace("\r", "\\r");
            strs = strs.Replace("/", "\\/");
            return strs;
        }
    }

public static class JsonExtions {
    public static JArray WriteToJArry(this DataTable table, JArray arr)
    {
        var cols = table.Columns;
        foreach (DataRow row in table.Rows)
        {
            var obj = new JObject();
            foreach (DataColumn li in cols)
            {
                var name = li.ColumnName;
                obj[name] = row[name].ToString();
            }
            arr.Add(obj);
        }
        return arr;
    }
}