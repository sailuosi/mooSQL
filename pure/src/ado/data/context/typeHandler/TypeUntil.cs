// 基础功能说明：


using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data
{
    internal class TypeUntil
    {


         /// <summary>
         /// 判断是否为基本数据类型，或基本数据类型的包装类，或泛型为基本数据类型
         /// </summary>
         /// <param name="val"></param>
         /// <returns></returns>

          
        public static bool isSQLParaType(Object val)
        {
            if (val.GetType().IsValueType || val.GetType().IsArray)
            {
                return true;
            }
            if (val is string || val is DateTime || val is Guid)
            {
                return true;
            }

            return false;
        }
    }
}

