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

        /// <summary>岗位编号（业务侧层次码或序号）。</summary>
        public string postNo;
        /// <summary>岗位主键 OID。</summary>
        public string postOID;

        /// <summary>岗位层次编码（用于上下级前缀判断）。</summary>
        public string postCode;

        /// <summary>
        /// 当前岗位编码是否以另一岗位的岗位编码为前缀（下级）。
        /// </summary>
        /// <param name="a">另一岗位（通常为父级）。</param>
        public bool isChildOf(Childable a)
        {
            var t = a as AuthPost;
            if (postCode.StartsWith(t.postCode))
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// 是否与另一岗位在编码、编号或 OID 上相同。
        /// </summary>
        /// <param name="a">另一岗位。</param>
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