// 基础功能说明：

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.auth
{
    /// <summary>
    /// 岗位权限功能对象
    /// </summary>
    public class AuthPost : Childable
    {

        public string postNo;
        public string postOID;

        public string postCode;

        public bool isChildOf(Childable a)
        {
            var t = a as AuthPost;
            if (postCode.StartsWith(t.postCode))
            {
                return true;
            }
            return false;
        }

        public bool isSame(Childable a)
        {
            var t = a as AuthPost;
            if (t.postCode == postCode || postNo == t.postNo || postOID == t.postOID)
            {
                return true;
            }
            return false;
        }
    }
}