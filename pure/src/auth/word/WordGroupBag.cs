using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using mooSQL.data;

namespace mooSQL.auth
{
    /// <summary>
    /// 权限组数据包，用于封装一组权限项的逻辑关系
    /// </summary>
    public class WordGroupBag
    {
        /// <summary>
        /// 权限组ID，用于关联权限组和权限项
        /// </summary>
        public string groupId;
        /// <summary>
        /// 是否添加权限，默认是AND逻辑，如果要使用OR逻辑，则需要设置为true。
        /// </summary>
        public bool isAdd= false;
        /// <summary>
        /// 逻辑运算符，默认是AND
        /// </summary>
        protected string seprator
        {
            get {
                return isAdd ? "AND" : "OR";
            }
        }
        /// <summary>
        /// 是否全部数据
        /// </summary>
        public bool isAll = false;
        /// <summary>
        /// 人员范围权限
        /// </summary>
        public ItemRange<AuthUser> manRange = new ItemRange<AuthUser>();
        /// <summary>
        /// 绑定的单位范围
        /// </summary>
        public CodeRange<AuthOrg> orgRange = new CodeRange<AuthOrg>();

        /// <summary>
        /// 绑定的单位范围
        /// </summary>
        public CodeRange<AuthPost> postRange = new CodeRange<AuthPost>();

        /// <summary>
        /// 未解析的语义
        /// </summary>
        public List<AuthWord> lazyWords = new List<AuthWord>();

        /// <summary>
        /// 动态语义翻译器
        /// </summary>
        public WordTranslator wordTranslator { get; set; }
        /// <summary>
        /// 复制动作
        /// </summary>
        /// <param name="src"></param>
        public virtual void CopyFunc(WordGroupBag src)
        {
            manRange.CopyFunc(src.manRange);
            orgRange.CopyFunc(src.orgRange);
            postRange.CopyFunc(src.postRange);
        }

        /// <summary>
        /// 添加所有数据的权限
        /// </summary>
        public void addAll()
        {
            this.isAll = true;
            //this.topClassCodes.Clear();
            //this.keys.Clear();
        }

        /// <summary>
        /// 添加一个用户
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        public int addMan(AuthUser user)
        {
            if (user == null) return 0;
            return manRange.add(user);
        }

        /// <summary>
        /// 添加一组单位到权限集合
        /// </summary>
        /// <param name="orgs"></param>
        /// <param name="haschild"></param>
        /// <returns></returns>
        public int addOrgs(List<AuthOrg> orgs, bool haschild)
        {
            int cc = 0;
            foreach (var org in orgs)
            {
                cc += addOrg(org, haschild);
            }
            return cc;
        }
        /// <summary>
        /// 添加单位权限
        /// </summary>
        /// <param name="org"></param>
        /// <param name="contains"></param>
        /// <returns></returns>
        public int addOrg(AuthOrg org, bool contains)
        {
            if (org == null) return 0;
            if (contains)
            {
                var oked = addContainOrg(org);
                return oked ? 1 : 0;
            }
            else
            {
                var oked = addBindOrg(org);
                return oked ? 1 : 0;
            }
        }

        /// <summary>
        /// /添加指定单位权限
        /// </summary>
        /// <param name="org"></param>
        /// <returns></returns>
        public bool addBindOrg(AuthOrg org)
        {
            if (isAll || org == null) return false;
            return orgRange.addBindValue(org);
        }
        /// <summary>
        /// 添加指定单位及其下级权限
        /// </summary>
        /// <param name="org"></param>
        /// <returns></returns>
        public bool addContainOrg(AuthOrg org)
        {
            if (isAll) return false;

            return orgRange.addContainValue(org);

        }

        /// <summary>
        /// /添加指定岗位权限
        /// </summary>
        /// <param name="post"></param>
        /// <returns></returns>
        public bool addBindPost(AuthPost post)
        {
            if (isAll || post == null) return false;
            return postRange.addBindValue(post);
        }
        /// <summary>
        /// 添加指定岗位及其下级权限
        /// </summary>
        /// <param name="post"></param>
        /// <returns></returns>
        public bool addContainPost(AuthPost post)
        {
            if (isAll) return false;

            return postRange.addContainValue(post);

        }

        /// <summary>
        /// 添加一个动态语义
        /// </summary>
        /// <param name="word"></param>
        /// <returns></returns>
        public void addLiveWord(AuthWord word)
        {
            if (this.wordTranslator == null)
            {
                this.wordTranslator = new WordTranslator();
            }
            wordTranslator.addWord(word);
            return ;
        }
        /// <summary>
        /// 添加一个未解析的语义，在运行时进行解析。
        /// </summary>
        /// <param name="word"></param>
        public void addLazyWord(AuthWord word)
        {
            if (word == null)
            {
                return ;
            }
            lazyWords.Add(word);
            return;
        }
        /// <summary>
        /// 是否为空权限集合。不包含所有权限，也不包含任何用户、单位、岗位权限时，认为是空的。
        /// </summary>
        public virtual bool Empty
        {
            get
            {
                if (isAll)
                {
                    return false;
                }
                if (!manRange.Empty)
                {
                    return false;
                }
                if (!orgRange.Empty)
                {
                    return false;
                }
                if (!postRange.Empty)
                {
                    return false;
                }
                if (lazyWords.Count > 0) { 
                    return false;
                }
                if (CheckEmpty() == false) { 
                    return false;
                }
                return true;
            }
        }
        /// <summary>
        /// 检查扩展的权限是否为空。默认是空的，子类可以重写此方法。
        /// </summary>
        /// <returns></returns>
        protected bool CheckEmpty()
        {
            return true;
        }

        /// <summary>
        /// 为动态语义注册参数
        /// </summary>
        /// <param name="key"></param>
        /// <param name="val"></param>
        /// <returns></returns>
        public void useWordPara(string key, object val)
        {
            if (this.wordTranslator != null)
            {
                wordTranslator.addPara(key, val);
            }
            return ;
        }
        /// <summary>
        /// 语义解析应用时刻。可以
        /// </summary>
        /// <param name="registerBuilder"></param>
        /// <returns></returns>
        public void onBuildLiveWord(Action<ConditionGroup, SQLBuilder> registerBuilder)
        {
            wordTranslator.onLoadBuild(registerBuilder);
            return ;
        }
        /// <summary>
        /// 重置SQL构建器，以便重新构建条件表达式。
        /// </summary>
        public virtual void resetBuilder()
        {
            manRange.resetBuilder();
            orgRange.resetBuilder();
            postRange.resetBuilder();

        }
        /// <summary>
        /// 子类扩展词条范围时，需要重写此方法。默认是织入组织、岗位和人三个范围的条件。
        /// </summary>
        /// <param name="condi"></param>
        /// <param name="kit"></param>
        /// <returns></returns>
        protected virtual List<string> buildRange(List<string> condi, SQLBuilder kit) {
            condi = orgRange.buildWhere(condi);
            condi = postRange.buildWhere(condi);
            condi = manRange.buildWhere(condi);
            return condi;
        }

        /// <summary>
        /// 编织一组条件。可重写
        /// </summary>
        /// <param name="wh"></param>
        /// <returns></returns>
        public virtual List<string> buildWhere(List<string> wh, SQLBuilder? kit)
        {

            if (kit != null)
            {
                if (isAdd)
                {
                    kit.sink();
                }
                else {
                    kit.sinkOR();
                }
                

                if (isAll) return wh;
                var condi = new List<string>();
                condi = buildRange(condi,kit);

                if (condi.Count > 0) {
                    var orwh = string.Join(" "+seprator+" ", condi);
                    orwh= "(" + orwh + ")";
                    wh.Add(orwh);
                }

                kit.rise();
            }
            else {
                var condi = buildWhereSQLString();
                if (condi != null) { 
                    wh.Add(condi);
                }
            }
            return wh;
        }
        /// <summary>
        /// 编织一组条件。可重写
        /// </summary>
        /// <returns></returns>
        protected string buildWhereSQLString() {
            var condi = new List<string>();
            condi = buildRange(condi,null);
            if (condi.Count == 0) {
                return null;
            }
            var orwh = string.Join(" " + seprator + " ", condi);
            return "(" + orwh + ")";
        }

        /// <summary>
        /// 执行组织条件的编织
        /// </summary>
        /// <param name="wh"></param>
        /// <param name="doBuild"></param>
        /// <returns></returns>
        public List<string> buildOrgWhere(List<string> wh, Func<AuthOrg, bool, string> doBuild)
        {
            if (isAll) return wh;
            wh = orgRange.buildWhere(wh, doBuild);
            return wh;
        }



        /// <summary>
        /// 获取所有的顶级组织节点值，注意，包含全部不在此判定中。因包含全部实质为无限大。
        /// </summary>
        /// <param name="getVal"></param>
        /// <returns></returns>
        public List<string> selectTopOrg(Func<AuthOrg, string> getVal)
        {
            return orgRange.selectTopOrg(getVal);
        }

        /// <summary>
        /// 检查某个层次码是否子码
        /// </summary>
        /// <param name="org"></param>
        /// <returns></returns>
        public bool checkOrgInScope(AuthOrg org)
        {
            if (isAll)
            {
                return true;
            }
            return orgRange.checkInScope(org);
        }



        /// <summary>
        /// 组织判相同条件
        /// </summary>
        /// <param name="doOrgFilter"></param>
        /// <returns></returns>
        public WordGroupBag whereOrgIs(Func<AuthOrg, string> doOrgFilter)
        {
            orgRange.useIsBuilder(doOrgFilter);
            return this;
        }
        /// <summary>
        /// 组织范围条件
        /// </summary>
        /// <param name="doOrgFilter"></param>
        /// <returns></returns>
        public WordGroupBag whereOrgIn(Func<List<AuthOrg>, string> doOrgFilter)
        {
            orgRange.useInBuilder(doOrgFilter);
            return this;
        }
        /// <summary>
        /// 组织下级条件
        /// </summary>
        /// <param name="doOrgFilter"></param>
        /// <returns></returns>
        public WordGroupBag whereOrgLike(Func<AuthOrg, string> doOrgFilter)
        {
            orgRange.useLikeBuilder(doOrgFilter);
            return this;
        }
        /// <summary>
        /// 组织的过滤条件
        /// </summary>
        /// <param name="doOrgFilter"></param>
        /// <returns></returns>
        public WordGroupBag whereOrgOne(Func<AuthOrg, bool, string> doOrgFilter)
        {
            orgRange.useOneBuilder(doOrgFilter);
            return this;
        }
        public WordGroupBag whereOrgBag(Func<CodeRange<AuthOrg>, string> doOrgFilter)
        {
            orgRange.useAllBuilder(doOrgFilter);
            return this;
        }
        // 岗位过滤动作注册
        /// <summary>
        /// 岗位是条件
        /// </summary>
        /// <param name="doPostFilter"></param>
        /// <returns></returns>
        public WordGroupBag wherePostIs(Func<AuthPost, string> doPostFilter)
        {
            postRange.useIsBuilder(doPostFilter);
            return this;
        }
        /// <summary>
        /// 岗位范围条件
        /// </summary>
        /// <param name="doPostFilter"></param>
        /// <returns></returns>
        public WordGroupBag wherePostIn(Func<List<AuthPost>, string> doPostFilter)
        {
            postRange.useInBuilder(doPostFilter);
            return this;
        }
        /// <summary>
        /// 岗位下级条件
        /// </summary>
        /// <param name="doPostFilter"></param>
        /// <returns></returns>
        public WordGroupBag wherePostLike(Func<AuthPost, string> doPostFilter)
        {
            postRange.useLikeBuilder(doPostFilter);
            return this;
        }

        /// <summary>
        /// 是某个岗位的权限
        /// </summary>
        /// <param name="doPostFilter"></param>
        /// <returns></returns>
        public WordGroupBag wherePost(Func<AuthPost, bool, string> doPostFilter)
        {
            postRange.useOneBuilder(doPostFilter);
            return this;
        }





    }
}
