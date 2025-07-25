// 基础功能说明：顶级方言类，对于子级方言，约定方法，而不约定成员。访问子级方言时，通过方法中转

using mooSQL.data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.auth
{
    /// <summary>
    /// 权限对象和数据库表的接合类
    /// </summary>
    public abstract class AuthDialect
    {

        public abstract PipelineDialect getPipeLine();

        public abstract WordBagDialect getWordBag();

        public virtual WordTranslator getWordTranslator() { 
            var tar= new WordTranslator();
            tar.dialect = this;
            return tar;
        }
        private WordTranslator _wordTranslator;

        internal WordTranslator wordTranslator
        {
            get {
                if (_wordTranslator == null) { 
                    _wordTranslator = getWordTranslator();
                }
                return _wordTranslator;
            }
        }
        /// <summary>
        /// 过滤器方言
        /// </summary>
        internal PipelineDialect pipeline
        {
            get {
                return getPipeLine();
            }
        }
        /// <summary>
        /// 词条集合
        /// </summary>
        internal WordBagDialect wordBag
        {
            get { 
                return getWordBag();
            }
        }
        /// <summary>
        /// 根据主键获取单位
        /// </summary>
        /// <param name="oid"></param>
        /// <returns></returns>
        public abstract AuthOrg getOrgByOID(string oid);

        /// <summary>
        /// 获取一组单位
        /// </summary>
        /// <param name="oid"></param>
        /// <returns></returns>
        public abstract List<AuthOrg> getOrgByOIDs(List<string> oid);

        /// <summary>
        /// 获取默认角色
        /// </summary>
        /// <returns></returns>
        public abstract List<AuthWord> getDefaultRole();
        /// <summary>
        /// 获取人员的单位
        /// </summary>
        /// <param name="man"></param>
        /// <returns></returns>
        public abstract AuthOrg loadManOrg(AuthUser man);
        /// <summary>
        /// 获取人员的部门
        /// </summary>
        /// <param name="man"></param>
        /// <returns></returns>
        public abstract AuthOrg loadManDiv(AuthUser man);
        /// <summary>
        /// 获取人员的岗位
        /// </summary>
        /// <param name="man"></param>
        /// <returns></returns>
        public abstract AuthPost loadManPost(AuthUser man);

        /// <summary>
        /// 获取用户
        /// </summary>
        /// <param name="acount"></param>
        /// <returns></returns>
        public abstract AuthUser getUser(string acount);
        /// <summary>
        /// 获取单位
        /// </summary>
        /// <param name="whereBuilder"></param>
        /// <returns></returns>
        public abstract AuthOrg getOrg(Action<SQLBuilder> whereBuilder);
        /// <summary>
        /// 获取岗位
        /// </summary>
        /// <param name="whereBuilder"></param>
        /// <returns></returns>
        public abstract AuthPost getPost(Action<SQLBuilder> whereBuilder);
        /// <summary>
        /// 获取用户
        /// </summary>
        /// <param name="whereBuilder"></param>
        /// <returns></returns>
        public abstract AuthUser getUser(Action<SQLBuilder> whereBuilder);

        /// <summary>
        /// 获取权限下的岗位
        /// </summary>
        /// <returns></returns>
        public virtual WordGroupBag newWordGroupBag()
        {

            var t = new WordGroupBag();

            return t;
        }
    }
}