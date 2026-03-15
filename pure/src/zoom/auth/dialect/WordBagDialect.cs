// 基础功能说明：


using mooSQL.data;
using mooSQL.utils;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.auth
{
    /// <summary>
    /// 单词分组包
    /// </summary>
    public class WordBagDialect
    {
        /// <summary>
        /// 根方言
        /// </summary>
        public AuthDialect rootDialect;
        WordGroupBag defaultBag;
        public WordBagDialect(AuthDialect root) { 
            this.rootDialect = root;
            this.defaultBag= this.getGroup(EmptyKey);
        }




        public Dictionary<string, WordGroupBag> groups = new Dictionary<string, WordGroupBag>();

        private AuthWord bindingWord;

        private Func<WordBagDialect, string> _onIsAll;
        /// <summary>
        /// 检查空集合时刻
        /// </summary>
        /// <returns></returns>
        protected virtual bool onCheckEmpty()
        {
            return true;
        }
        /// <summary>
        /// 绑定单词，
        /// </summary>
        /// <param name="bindingWord"></param>
        public void BindWord(AuthWord bindingWord) { 
            this.bindingWord = bindingWord;
        }
        /// <summary>
        /// 
        /// </summary>
        public bool Empty
        {
            get
            {
                foreach (var group in groups.Values) { 
                    if(!group.Empty) return false;
                }


                if (onCheckEmpty()==false)
                {
                    return false;
                }
                return true;
            }
        }

        private static string EmptyKey = "_empty";
        /// <summary>
        /// 获取分组，
        /// </summary>
        /// <param name="groupId"></param>
        /// <returns></returns>
        protected WordGroupBag getGroup(string groupId) {
            if (bindingWord !=null &&  groupId == EmptyKey) {
                if (!string.IsNullOrWhiteSpace(bindingWord.groupId)) { 
                    groupId = bindingWord.groupId;
                }
            }

            if (groups.ContainsKey(groupId)) { 
                return groups[groupId];
            }
            var tar= rootDialect.newWordGroupBag();
            if (string.IsNullOrEmpty(groupId)) {
                groupId = EmptyKey;
            }

            if (groupId == EmptyKey)
            {
                tar.isAdd = false;
            }
            else { 
                tar.isAdd = true;
            }

            this.groups.Add(groupId, tar);
            return tar;
        }

        /// <summary>
        /// 注册全部数据时的事件,将替换掉默认实现 1=1条件
        /// </summary>
        /// <param name="onIsAll"></param>
        public void onIsAll(Func<WordBagDialect, string> onIsAll)
        {
            this._onIsAll = onIsAll;
        }
        /// <summary>
        /// 添加所有数据的权限
        /// </summary>
        public void addAll(string group= "_empty")
        {
            var grou= this.getGroup(group);
            grou.addAll();

        }
        /// <summary>
        /// 添加一个用户
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        public int addMan(AuthUser user, string group = "_empty")
        {
            if (user == null) return 0;
            var grou = this.getGroup(group);
            return grou.addMan(user);
        }


        /// <summary>
        /// 添加一组单位到权限集合
        /// </summary>
        /// <param name="orgs"></param>
        /// <param name="haschild"></param>
        /// <returns></returns>
        public int addOrgs(List<AuthOrg> orgs, bool haschild, string group = "_empty")
        {
            var grou = this.getGroup(group);
            return grou.addOrgs(orgs,haschild);
        }
        /// <summary>
        /// 添加单位权限
        /// </summary>
        /// <param name="org"></param>
        /// <param name="contains"></param>
        /// <returns></returns>
        public int addOrg(AuthOrg org, bool contains, string group = "_empty")
        {
            var grou = this.getGroup(group);
            return grou.addOrg(org, contains);
        }
        /// <summary>
        /// /添加指定单位权限
        /// </summary>
        /// <param name="org"></param>
        /// <returns></returns>
        public bool addBindOrg(AuthOrg org, string group = "_empty")
        {
            var grou = this.getGroup(group);
            return grou.addBindOrg(org);
        }
        /// <summary>
        /// 添加指定单位及其下级权限
        /// </summary>
        /// <param name="org"></param>
        /// <returns></returns>
        public bool addContainOrg(AuthOrg org, string group = "_empty")
        {
            var grou = this.getGroup(group);
            return grou.addContainOrg(org);
        }

        /// <summary>
        /// 添加一个动态语义
        /// </summary>
        /// <param name="word"></param>
        /// <returns></returns>
        public WordBagDialect addLiveWord(AuthWord word, string group = "_empty")
        {
            var grou = this.getGroup(group);
            grou.addLiveWord(word);
            return this;
        }

        /// <summary>
        /// /添加指定岗位权限
        /// </summary>
        /// <param name="post"></param>
        /// <returns></returns>
        public bool addBindPost(AuthPost post, string group = "_empty")
        {
            var grou = this.getGroup(group);
            return  grou.addBindPost(post);
        }
        /// <summary>
        /// 添加指定岗位及其下级权限
        /// </summary>
        /// <param name="post"></param>
        /// <returns></returns>
        public bool addContainPost(AuthPost post, string group = "_empty")
        {
            var grou = this.getGroup(group);
            return grou.addContainPost(post);

        }

        public void addLazyWord(AuthWord word, string group = "_empty")
        {
            var grou = this.getGroup(group);
            grou.addLazyWord(word);
            
        }

        public void parseWord(AuthDialect dialect) {
            foreach (var group in this.groups)
            {
                if (group.Value.wordTranslator != null) {
                    group.Value.wordTranslator.parse(dialect);
                }
                
            }
        }
        #region 清空
        /// <summary>
        /// 清空注册的条件编织器
        /// </summary>
        /// <returns></returns>
        public virtual WordBagDialect resetBuilder()
        {
            foreach (var group in this.groups) { 
                group.Value.resetBuilder();
            }
            onResetBuilder();
            return this;
        }
        /// <summary>
        /// 清空时刻
        /// </summary>
        protected virtual void onResetBuilder()
        {

        }

        /// <summary>
        /// 为动态语义注册参数
        /// </summary>
        /// <param name="key"></param>
        /// <param name="val"></param>
        /// <returns></returns>
        public void useWordPara(string key, object val)
        {
            foreach (var group in this.groups) {
                group.Value.useWordPara(key, val);
            }
        }
        /// <summary>
        /// 语义解析应用时刻。可以
        /// </summary>
        /// <param name="registerBuilder"></param>
        /// <returns></returns>
        public void onBuildLiveWord(Action<ConditionGroup, SQLBuilder> registerBuilder)
        {
            foreach (var group in this.groups)
            {
                group.Value.onBuildLiveWord(registerBuilder);
            }
        }
        #endregion

        #region 执行器部分
        /// <summary>
        /// 人员的where条件
        /// </summary>
        /// <param name="doManFilter"></param>
        /// <returns></returns>
        public WordBagDialect whereMan(Func<AuthUser, string> doManFilter)
        {
            foreach (var kv in this.groups) { 
                kv.Value.manRange.useOneBuilder(doManFilter);
            }
            
            return this;
        }
        /// <summary>
        /// 一组人员的条件
        /// </summary>
        /// <param name="doManFilter"></param>
        /// <returns></returns>
        public WordBagDialect whereMan(Func<List<AuthUser>, string> doManFilter)
        {
            foreach (var kv in this.groups)
            {
                kv.Value.manRange.useInBuilder(doManFilter);
            }            
            return this;
        }


        /// <summary>
        /// 组织判相同条件
        /// </summary>
        /// <param name="doOrgFilter"></param>
        /// <returns></returns>
        public WordBagDialect whereOrgIs(Func<AuthOrg, string> doOrgFilter)
        {
            foreach (var kv in this.groups) {
                kv.Value.whereOrgIs(doOrgFilter);
            }
            
            return this;
        }
        /// <summary>
        /// 组织范围条件
        /// </summary>
        /// <param name="doOrgFilter"></param>
        /// <returns></returns>
        public WordBagDialect whereOrgIn(Func<List<AuthOrg>, string> doOrgFilter)
        {
            foreach (var kv in this.groups)
            {
                kv.Value.whereOrgIn(doOrgFilter);
            }

            return this;
        }
        /// <summary>
        /// 组织下级条件
        /// </summary>
        /// <param name="doOrgFilter"></param>
        /// <returns></returns>
        public WordBagDialect whereOrgLike(Func<AuthOrg, string> doOrgFilter)
        {
            foreach (var kv in this.groups)
            {
                kv.Value.whereOrgLike(doOrgFilter);
            }

            return this;
        }
        /// <summary>
        /// 组织的过滤条件
        /// </summary>
        /// <param name="doOrgFilter"></param>
        /// <returns></returns>
        public WordBagDialect whereOrgOne(Func<AuthOrg, bool, string> doOrgFilter)
        {
            foreach (var kv in this.groups)
            {
                kv.Value.whereOrgOne(doOrgFilter);
            }

            return this;
        }
        public WordBagDialect whereOrgBag(Func<CodeRange<AuthOrg>, string> doOrgFilter)
        {
            foreach (var kv in this.groups)
            {
                kv.Value.whereOrgBag(doOrgFilter);
            }

            return this;
        }
        // 岗位过滤动作注册
        /// <summary>
        /// 岗位是条件
        /// </summary>
        /// <param name="doPostFilter"></param>
        /// <returns></returns>
        public WordBagDialect wherePostIs(Func<AuthPost, string> doPostFilter)
        {
            foreach (var kv in this.groups)
            {
                kv.Value.wherePostIs(doPostFilter);
            }

            return this;
        }
        /// <summary>
        /// 岗位范围条件
        /// </summary>
        /// <param name="doPostFilter"></param>
        /// <returns></returns>
        public WordBagDialect wherePostIn(Func<List<AuthPost>, string> doPostFilter)
        {
            foreach (var kv in this.groups)
            {
                kv.Value.wherePostIn(doPostFilter);
            }

            return this;
        }
        /// <summary>
        /// 岗位下级条件
        /// </summary>
        /// <param name="doPostFilter"></param>
        /// <returns></returns>
        public WordBagDialect wherePostLike(Func<AuthPost, string> doPostFilter)
        {
            foreach (var kv in this.groups)
            {
                kv.Value.wherePostLike(doPostFilter);
            }

            return this;
        }

        /// <summary>
        /// 是某个岗位的权限
        /// </summary>
        /// <param name="doPostFilter"></param>
        /// <returns></returns>
        public WordBagDialect wherePost(Func<AuthPost, bool, string> doPostFilter)
        {
            foreach (var kv in this.groups)
            {
                kv.Value.wherePost(doPostFilter);
            }
            return this;
        }

        #endregion

        #region 生命周期
        /// <summary>
        /// 编织一组条件。可重写
        /// </summary>
        /// <param name="wh"></param>
        /// <returns></returns>
        public virtual List<string> buildWhere(List<string> wh, SQLBuilder kit)
        {
            checkAction();
            foreach (var kv in this.groups) { 
                kv.Value.buildWhere(wh, kit);
            }

            wh = onBuildWhere(wh);

            return wh;
        }

        private void checkAction() {
            foreach (var kv in this.groups)
            {
                if (kv.Value.groupId != EmptyKey) {
                    kv.Value.CopyFunc(defaultBag);
                }
            }
        }

        protected virtual List<string> onBuildWhere(List<string> wh)
        {
            return wh;
        }

        /// <summary>
        /// 执行组织条件的编织
        /// </summary>
        /// <param name="wh"></param>
        /// <param name="doBuild"></param>
        /// <returns></returns>
        public List<string> buildOrgWhere(List<string> wh, Func<AuthOrg, bool, string> doBuild)
        {
            foreach (var kv in this.groups)
            {
                kv.Value.buildOrgWhere(wh, doBuild);
            }
            return wh;
        }



        /// <summary>
        /// 获取所有的顶级组织节点值，注意，包含全部不在此判定中。因包含全部实质为无限大。
        /// </summary>
        /// <param name="getVal"></param>
        /// <returns></returns>
        public List<string> selectTopOrg(Func<AuthOrg, string> getVal)
        {
            var res= new List<string>();
            foreach (var kv in this.groups)
            {
                var t= kv.Value.selectTopOrg(getVal);
                res.AddNotRepeat(t);
            }
            return res;
            
        }

        /// <summary>
        /// 检查某个层次码是否子码
        /// </summary>
        /// <param name="org"></param>
        /// <returns></returns>
        public bool checkOrgInScope(AuthOrg org, string group = "empty")
        {
            var grou = this.getGroup(group);
            var t= grou.checkOrgInScope(org);
            return t;
        }



        public bool isAll {
            get { 
                //在所有的或分组中，如果有任意一个是所有数据，则就是所有数据
                foreach (var kv in this.groups)
                {
                    if (kv.Value.isAll && kv.Value.isAdd==false) { 
                        return true;
                    }
                }
                return false;            
            }

        }

        /// <summary>
        /// 编织为一个SQL语句
        /// </summary>
        /// <returns></returns>
        public string build(SQLBuilder kit, Func<List<string>, List<string>> afterBuild)
        {


            var res = new List<string>();
            if (kit != null)
            {
                //调用执行器的or包裹

                kit.orLeft();
                if (isAll)
                {
                    if (this._onIsAll != null)
                    {
                        res.AddNotRepeat(this._onIsAll(this));
                    }
                    else
                    {
                        kit.where("1=1");
                    }
                }
                else
                {
                    res = buildWhere(res, kit);
                    res = afterBuild(res);
                }

                kit.orRight();

            }
            else
            {
                if (isAll)
                {
                    if (this._onIsAll != null)
                    {
                        return this._onIsAll(this);
                    }
                    else
                    {
                        return "1=1";
                    }
                }
                res = buildWhere(res, kit);
                //res = builder.invokeLazyReador(res, kit);
            }

            if (res.Count == 0)
            {
                return "1=2";
            }
            var orwh = string.Join(" OR ", res);
            return "(" + orwh + ")";
        }
        #endregion
    }
}