// 基础功能说明：

using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HHNY.NET.Core;
public class AjaxResult
{
    #region JSON的接口结果输出
    public static JObject wrapStatus(string status, Object data)
    {
        var res = new JObject();
        res["status"] = status;
        res["data"] = (JToken)data;
        return res;
    }
    public static JObject success(string msg, JObject dataObj)
    {
        var res = new JObject();

        res["status"] = "success";
        res["info"] = msg;

        if (dataObj == null) { dataObj = new JObject(); }
        res["data"] = dataObj;
        return res;
    }

    public static JObject error(string msg, JObject dataObj)
    {
        var res = new JObject();

        res["status"] = "error";
        res["info"] = msg;

        if (dataObj == null) { dataObj = new JObject(); }
        res["data"] = dataObj;
        return res;
    }
    public static JObject error(string msg)
    {
        var res = new JObject();

        res["status"] = "error";
        res["info"] = msg;

        return res;
    }

    public static JObject success(string msg, DataTable dataObj)
    {
        var res = new JObject();

        res["status"] = "success";
        res["info"] = msg;
        JArray ar = (JArray)JsonConvert.DeserializeObject(JsonConvert.SerializeObject(dataObj));
        res["data"] = ar;
        return res;
    }

    public static JObject success(int total, DataTable dataObj)
    {
        var res = new JObject();

        res["status"] = "success";
        res["total"] = total;
        JArray ar = (JArray)JsonConvert.DeserializeObject(JsonConvert.SerializeObject(dataObj));
        res["data"] = ar;
        return res;
    }
    public static JObject success(int total, DataTable dataObj, Object dict)
    {
        var res = new JObject();

        res["status"] = "success";
        res["total"] = total;
        JArray ar = (JArray)JsonConvert.DeserializeObject(JsonConvert.SerializeObject(dataObj));
        res["data"] = ar;
        res["dict"] = JObject.FromObject(dict);
        return res;
    }
    public static JObject success(int total, JArray arr)
    {
        var res = new JObject();

        res["status"] = "success";
        res["total"] = total;
        res["data"] = arr;
        return res;
    }

    public static JObject success(string msg, string dataObj)
    {
        var res = new JObject();

        res["status"] = "success";
        res["info"] = msg;
        res["data"] = dataObj;
        return res;
    }
    public static JObject success(DataTable dataObj)
    {
        var res = new JObject();

        res["status"] = "success";

        JArray ar = (JArray)JsonConvert.DeserializeObject(JsonConvert.SerializeObject(dataObj));
        res["data"] = ar;
        return res;
    }

    public static JObject success(string dataObj)
    {
        var res = new JObject();

        res["status"] = "success";

        res["data"] = dataObj;
        return res;
    }
    public static JObject success(JArray dataObj)
    {
        var res = new JObject();

        res["status"] = "success";

        res["data"] = dataObj;
        return res;
    }

    public static JObject success(JObject dataObj)
    {
        var res = new JObject();

        res["status"] = "success";

        res["data"] = dataObj;
        return res;
    }

    public static JObject success(DataRow row)
    {
        var res = new JObject();

        res["status"] = "success";

        res["data"] = Json.RowToJobj(row);
        return res;
    }
    public static JObject success<T>(IEnumerable<T> data)
    {
        var res = new JObject();

        res["status"] = "success";
        if (data is JToken)
        {
            res["data"] = (JToken)data;

        }
        else { 
            var da = JsonConvert.DeserializeObject(JsonConvert.SerializeObject(data));
            if (da as JArray != null)
            {
                res["data"] = da as JArray;
            }
            else if (da as JObject != null)
            {
                res["data"] = da as JObject;
            }
            else {
                res["data"] =(JToken) da; 
            }        
        }

        //res["data"] = (JArray)JsonConvert.DeserializeObject(JsonConvert.SerializeObject(data));
        return res;
    }
    public static JObject success(Object data)
    {
        var res = new JObject();

        res["status"] = "success";
        if (data != null) {
            if (data is int d)
            {
                res["data"] = d;
            }
            else if (data is long dlong)
            {
                res["data"] = dlong;
            }
            else if (data is float dflo)
            {
                res["data"] = dflo;
            }
            else if (data is string dstr)
            {
                res["data"] = dstr;
            }
            else if (data is double dou)
            {
                res["data"] = dou;
            }
            else if (data is Guid dguid)
            {
                res["data"] = dguid;
            }
            else {
                res["data"] = JObject.FromObject(data);
            }
            
        }
        
        return res;
    }
    #endregion
}
