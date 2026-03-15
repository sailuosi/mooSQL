// 基础功能说明：

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.auth
{

    /// <summary>
    /// 权限的各类对象构造工厂
    /// </summary>
    public abstract class AuthFactory<RealDialect> where RealDialect : AuthDialect
    {
        /// <summary>
        /// 获取方言体，必须重写本方法
        /// </summary>
        /// <returns></returns>
        public abstract RealDialect GetDialect();

        /// <summary>
        /// 获取权限下的单位对象
        /// </summary>
        /// <returns></returns>
        public virtual AuthOrg getAuthOrg()
        {

            var t = new AuthOrg();

            return t;
        }
        /// <summary>
        /// 获取权限下的人员
        /// </summary>
        /// <returns></returns>
        public virtual AuthUser getAuthUser()
        {

            var t = new AuthUser();
            t.dialect = GetDialect();
            return t;
        }
        /// <summary>
        /// 获取权限下的岗位
        /// </summary>
        /// <returns></returns>
        public virtual AuthPost getAuthPost()
        {

            var t = new AuthPost();

            return t;
        }

        /// <summary>
        /// 获取权限下的岗位
        /// </summary>
        /// <returns></returns>
        public virtual WordGroupBag getWordGroupBag()
        {

            var t = new WordGroupBag();

            return t;
        }
    }
}