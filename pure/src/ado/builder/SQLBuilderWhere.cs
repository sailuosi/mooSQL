// 基础功能说明：

using mooSQL.utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data {
    public partial class SQLBuilder
    {
        /// <summary>
        /// 当前where条件的个数
        /// </summary>
        public int ConditionCount
        {
            get
            {
                int count = 0;
                foreach (var item in groups)
                {
                    count += item.Value.wherePart.Count;
                }
                return count;
            }
        }
        /// <summary>
        /// where条件的连接符
        /// </summary>
        public string ConditionSeprator
        {
            get
            {
                return current.wherePart.CurrentConnector;
            }
        }
        /// <summary>
        /// 当前的where条件是否为and
        /// </summary>
        public bool ConditionIsAnd
        {
            get {
                return ConditionSeprator.ToUpper().Trim() == "AND";
            }
        }
        /// <summary>
        /// 当前的where条件
        /// </summary>
        public WhereCollection CurrentCondition
        {
            get { 
                return current.wherePart;
            }
        }

        /// <summary>
        /// 丢弃上一个where条件
        /// </summary>
        /// <returns></returns>
        public SQLBuilder popPreWhere()
        {
            if (ConditionCount > 0)
            {
                current.wherePart.pop();
            }
            return this;
        }



        /// <summary>
        /// 开始一个括号，并切换到or模式
        /// </summary>
        /// <returns></returns>
        public SQLBuilder orLeft()
        {
            current.wherePart.sink("OR");
            return this;
        }
        /// <summary>
        /// 结束一个括号，并返回到之前的模式
        /// </summary>
        /// <returns></returns>
        public SQLBuilder orRight()
        {
            closeBraket();
            return this;
        }


        /// <summary>
        /// 开始一个括号，并切换到or模式
        /// </summary>
        /// <returns></returns>
        public SQLBuilder andLeft()
        {
            //this._currentBracket++;
            //_currentSepator[_currentBracket] = current.whereSeprator;
            //pinLeft().and();

            //_preWhereCount[_currentBracket] = current.conditions.Count;
            current.wherePart.sink("AND");
            return this;
        }

        private void closeBraket() {
            current.wherePart.rise();
            //var nowCount = current.conditions.Count;

            //var preCount = _preWhereCount[_currentBracket];
            //if (nowCount == preCount && preCount != -1)
            //{
            //    //什么都没有干，于是撤销拼接，
            //    popPreWhere();
            //}
            //else
            //{
            //    //拼接右括号
            //    pinRight();
            //}
            ////恢复关联符。
            //if (_currentSepator != null)
            //{
            //    current.whereSeprator = _currentSepator[_currentBracket];
            //}
            //else
            //{
            //    and();
            //}
            ////重置状态

            //_preWhereCount[_currentBracket] = -1;
            //_currentSepator[_currentBracket] = "";
            //_currentBracket--;
        }

        /// <summary>
        /// 结束一个括号，并返回到之前的模式
        /// </summary>
        /// <returns></returns>
        public SQLBuilder andRight()
        {
            closeBraket();
            return this;
        }
        #region where条件构造器
        /// <summary>
        /// 添加一个 where条件字符串
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public SQLBuilder where(string key)
        {
            if (!opened)
            {
                opened = true;
                return this;
            }
            //空值不拼接
            if (string.IsNullOrWhiteSpace(key)) return this;
            current.where(key, null, "", false);
            return this;
        }
        /// <summary>
        /// 添加一个 where is null
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public SQLBuilder whereIsNull(string key) { 
            return where(key + " IS NULL");
        }
        /// <summary>
        ///     添加一个 where is not null
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public SQLBuilder whereIsNotNull(string key) { 
            return where(key + " IS NOT NULL");
        }

        /// <summary>
        /// 添加一个 where条件字符串
        /// </summary>
        /// <param name="frag"></param>
        /// <returns></returns>
        public SQLBuilder where(WhereFrag frag)
        {
            if (!opened)
            {
                opened = true;
                return this;
            }
            //空值不拼接
            if (frag==null) return this;
            current.where(frag);
            return this;
        }
        /// <summary>
        /// 添加一个自由拼接的where字符串，一般是左右括号 ( )  ;
        /// </summary>
        /// <param name="SQL"></param>
        /// <returns></returns>
        public SQLBuilder pin(string SQL)
        {
            var tar = new WhereFrag();
            tar.pined = true;
            tar.key = SQL;
            tar.op = "";
            tar.value = null;
            current.where(tar);
            return this;
        }
        /// <summary>
        /// 构建一组where ( ... or ... )的条件，为空时自动忽略本次构建。
        /// </summary>
        /// <param name="whereBuilder"></param>
        /// <returns></returns>
        public SQLBuilder whereOR(Action<SQLBuilder> whereBuilder)
        {
            var bro = this.getBrotherBuilder();
            bro.or();
            whereBuilder(bro);
            var t = bro.buildWhereContent();
            if (!string.IsNullOrWhiteSpace(t))
            {
                this.orLeft()
                    .where(t)
                    .orRight();
                return this;
            }
            return this;
        }

        /// <summary>
        /// 拼接一个左括号( 到where条件中
        /// </summary>
        /// <returns></returns>
        public SQLBuilder pinLeft()
        {
            current.wherePart.sink(this.ConditionSeprator);
            return this;
        }
        /// <summary>
        /// 拼接一个右括号) 到where条件中
        /// </summary>
        /// <returns></returns>
        public SQLBuilder pinRight()
        {
            //var tar = new WhereFrag();
            //tar.pined = true;//左侧不需要连接
            //tar.nextPined = false;//右侧需要连接
            //tar.key = " ) ";
            //tar.op = "";
            //tar.value = null;
            //current.where(tar);
            current.wherePart.rise();
            return this;
        }
        /// <summary>
        /// 调用本方法后，where 条件构建状态为 and 模式，此后所有条件都使用and 进行连接
        /// </summary>
        /// <returns></returns>
        public SQLBuilder and()
        {
            current.and();
            return this;
        }
        /// <summary>
        /// 调用本方法后，where 条件构建状态为 or 模式，此后所有条件都使用 or 进行连接
        /// </summary>
        /// <returns></returns>
        public SQLBuilder or()
        {
            current.or();
            return this;
        }
        /// <summary>
        /// 执行一组 and/or ( ... or  ... ) 的where条件的构建，构造的条件不能为空，否则形成 and () 的空结构。。
        /// </summary>
        /// <param name="doSomeWhere"></param>
        /// <returns></returns>
        public SQLBuilder or(Action<SQLBuilder> doSomeWhere)
        {
            orLeft();
            doSomeWhere(this);
            orRight();
            return this;
        }
        /// <summary>
        /// 执行一组and条件。
        /// </summary>
        /// <param name="doSomeWhere"></param>
        /// <returns></returns>
        public SQLBuilder and(Action<SQLBuilder> doSomeWhere)
        {
            andLeft();
            doSomeWhere(this);
            andRight();
            return this;
        }
        /// <summary>
        /// 开启一个新的条件分组，默认是开启AND分组，注意：不调用rise将保持在分组中
        /// </summary>
        /// <param name="connector"></param>
        /// <returns></returns>
        public SQLBuilder sink(string connector = "AND")
        {
            current.wherePart.sink(connector);
            return this;
        }
        /// <summary>
        /// 开启一个新的条件分组，默认是开启OR分组 注意：不调用rise将保持在分组中
        /// </summary>
        /// <returns></returns>
        public SQLBuilder sinkOR()
        {
            current.wherePart.sink("OR");
            return this;
        }
        /// <summary>
        /// 脱离当前的一组条件分组，回退到上一组条件。
        /// </summary>
        /// <returns></returns>
        public SQLBuilder rise() { 
            current.wherePart.rise();
            return this;
        }
        /// <summary>
        /// 当前括号条件组为否定模式
        /// </summary>
        /// <returns></returns>
        public SQLBuilder not()
        {
            current.wherePart.not();
            return this;
        }

        /// <summary>
        /// 拼接一个 like concat(concat('%', "+paraed+"), '%')形式的参数SQL
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>

        public string getLikeSQL(string key, Object value)
        {
            string paraed = this.addPara(key, value);
            string res = " like concat(concat('%', " + paraed + "), '%')";
            return res;
        }
        /// <summary>
        /// 左右全模糊的like查询，值为null将忽略
        /// </summary>
        /// <param name="key"></param>
        /// <param name="val"></param>
        /// <returns></returns>
        public SQLBuilder whereLike(string key, Object val)
        {
            if (!opened)
            {
                opened = true;
                return this;
            }
            if (paraRule == ("notEmpty"))
            {
                if (val == null || string.IsNullOrEmpty(val.ToString().Trim()))
                {
                    return this;
                }
            }
            if (paraRule == ("notNull"))
            {
                if (val == null)
                {
                    return this;
                }
            }
            if (val !=null && RegxUntils.isPureSimpleStr(val.ToString())) {
                return where(string.Format("{0} like '%{1}%'", key, val.ToString()));
            }
            //更改实现方式，将内容部分直接作为参数；
            if (val == null) {
                return this;
            }
            var Val = string.Format("%{0}%", val.ToString());
            return where(key, Val, "like");
            //
            //var content = Dialect.expression.stringConcat("'%'", "{0}", "'%'");
            //return whereFormat(key + " like "+ content, val);
        }
        /// <summary>
        /// 在多个字段中模糊匹配一个字符串，形如 (key1 like '%abc%' or key2 like '%abc%') 形式，
        /// </summary>
        /// <param name="keys"></param>
        /// <param name="val"></param>
        /// <returns></returns>
        public SQLBuilder whereLikes(IEnumerable<string> keys, string val)
        {
            sinkOR();
            foreach (var key in keys)
            {
                whereLike(key, val);
            }
            rise();
            return this;
        }
        /// <summary>
        /// 模糊匹配一组字符串，默认使用 or 连接，形如 (key like '%abc%' or key like '%bcd%') 形式，
        /// </summary>
        /// <param name="key"></param>
        /// <param name="vals"></param>
        /// <param name="isOr"></param>
        /// <returns></returns>
        public SQLBuilder whereLikes(string key, IEnumerable<string> vals, bool isOr = true) {
            //判定有效性
            if (vals == null) return this;
            if (vals.Count() == 0)
            {
                return this;
            }
            if (isOr)
            {
                sinkOR();
            }
            else
            {
                sink();
            }
            foreach (var val in vals)
            {
                whereLike(key, val);
            }
            rise();
            return this;
        }
        /// <summary>
        /// 左侧开始的模糊 形成 like 'abc%' 格式语句
        /// </summary>
        /// <param name="key"></param>
        /// <param name="val"></param>
        /// <returns></returns>
        public SQLBuilder whereLikeLeft(string key, Object val)
        {
            if (!opened)
            {
                opened = true;
                return this;
            }
            if (paraRule == ("notEmpty"))
            {
                if (val == null || string.IsNullOrEmpty(val.ToString().Trim()))
                {
                    return this;
                }
            }
            if (paraRule == ("notNull"))
            {
                if (val == null)
                {
                    return this;
                }
            }
            if (val != null && RegxUntils.isPureSimpleStr(val.ToString()))
            {
                return where(string.Format("{0} like '{1}%'", key, val.ToString()));
            }
            //更改实现方式，将内容部分直接作为参数；
            if (val == null)
            {
                return this;
            }
            var Val = string.Format("{0}%", val.ToString());
            return where(key, Val, "like");
            //var content = Dialect.expression.stringConcat("{0}", "'%'");
            //return whereFormat(key + " like "+ content, val);
        }
        /// <summary>
        /// 层次码一组条件，形成(a.code like '100%' or a.code like '200%' ...)形式
        /// </summary>
        /// <param name="key"></param>
        /// <param name="vals"></param>
        /// <param name="isOr"></param>
        /// <returns></returns>
        public SQLBuilder whereLikeLefts(string key, IEnumerable<string> vals, bool isOr = true) { 
            //判定有效性
            if(vals==null) return this;
            if(vals.Count()==0) { 
                return this;
            }
            if (isOr)
            {
                sinkOR();
            }
            else { 
                sink();
            }
            foreach (var val in vals) { 
                whereLikeLeft(key, val);
            }
            rise();
            return this;
        }
        /// <summary>
        /// 多个左模糊条件
        /// </summary>
        /// <param name="key"></param>
        /// <param name="likeCodes"></param>
        /// <returns></returns>
        public SQLBuilder whereLikeLefts(string key, params string[] likeCodes) { 
            return whereLikeLefts(key, likeCodes,true);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <param name="val"></param>
        /// <returns></returns>
        public SQLBuilder whereNotLike(string key, Object val)
        {
            if (!opened)
            {
                opened = true;
                return this;
            }
            if (paraRule == ("notEmpty"))
            {
                if (val == null || string.IsNullOrEmpty(val.ToString().Trim()))
                {
                    return this;
                }
            }
            if (paraRule == ("notNull"))
            {
                if (val == null)
                {
                    return this;
                }
            }
            return whereFormat(key + " not like concat(concat('%', {0}), '%')", val);
        }
        /// <summary>
        /// 检查参数是否正常，参数量为空时，自动转为 1=2的不可能条件，为null时忽略。
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="val"></param>
        /// <returns></returns>
        private bool checkWhereIn<T>(IEnumerable<T> val)
        {
            if (val == null)
            {
                return false;
            }
            if (val.Count() == 0)
            {
                where("1=2");
                return false;
            }
            return true;
        }

        /// <summary>
        /// 构建where in + (固定范围值) 条件。注意：数值型集合直接转为数值范围SQL，简单字符集合转为字符SQL，复杂字符串为参数化。 受SQL参数上限影响，请不要传入过大的list。参数量为空时，自动转为 1=2的不可能条件，为null时忽略。
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <param name="values"></param>
        /// <returns></returns>
        public SQLBuilder whereIn<T>(string key, IEnumerable<T> values)
        {
            if (!opened)
            {
                opened = true;
                return this;
            }
            if (!checkWhereIn(values))
            {
                return this;
            }
            whereListInner(key, " IN ", values);
            return this;
        }
        /// <summary>
        /// 构建where in + (固定范围值) 条件。注意：数值型集合直接转为数值范围SQL，简单字符集合转为字符SQL，复杂字符串为参数化。 受SQL参数上限影响，请不要传入过大的list。参数量为空时，自动转为 1=2
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <param name="values"></param>
        /// <returns></returns>
        public SQLBuilder whereIn<T>(string key,params T[] values)
        {
            if (!opened)
            {
                opened = true;
                return this;
            }
            if (!checkWhereIn(values))
            {
                return this;
            }
            whereListInner(key, " IN ", values);
            return this;
        }
        /// <summary>
        /// 形成 ( key =val1 or key =val2 or ... 形式，等同于 whereIn(key,values.ToArray()
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <param name="values"></param>
        /// <returns></returns>
        public SQLBuilder whereOR<T>(string key, params T[] values)
        {
            if (!opened)
            {
                opened = true;
                return this;
            }
            if (!checkWhereIn(values))
            {
                return this;
            }
            sinkOR();
            foreach (var val in values) {
                where(key, val);
            }
            rise();
            return this;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <param name="values"></param>
        /// <returns></returns>
        public SQLBuilder whereIn(string key, IEnumerable values)
        {
            if (!opened)
            {
                opened = true;
                return this;
            }

            int i = 0;
            foreach (var t in values) {
                i++;
            }
            if (i == 0) {
                return where("1=2");
            }
            whereListInner(key, " IN ", values);
            return this;
        }

        /// <summary>
        /// 构建where in 范围值，所有值均参数化。注意：受SQL参数上限影响，请不要传入过大的list。参数量为空时，自动转为 1=2的不可能条件，为null时忽略。
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <param name="val">参数量为空时，自动转为 1=2的不可能条件，为null时忽略。</param>
        /// <returns></returns>
        public SQLBuilder whereIn<T>(string key, List<T> val)
        {
            if (!opened)
            {
                opened = true;
                return this;
            }
            if (!checkWhereIn(val))
            {
                return this;
            }
            whereListInner(key, " IN ", val);
            return this;
        }
        /// <summary>
        /// 构建where in 范围值，所有值均参数化。注意：受SQL参数上限影响，请不要传入过大的list。参数量为空时，自动转为 1=2的不可能条件，为null时忽略。
        /// </summary>
        /// <param name="key"></param>
        /// <param name="val">参数量为空时，自动转为 1=2的不可能条件，为null时忽略。</param>
        /// <returns></returns>
        public SQLBuilder whereIn(string key, List<Object> val)
        {
            if (!opened)
            {
                opened = true;
                return this;
            }
            if (!checkWhereIn(val))
            {
                return this;
            }
            whereListInner(key, " IN ", val);
            return this;
        }
        /// <summary>
        /// 创建一个 自定义嵌套 where in 的 select
        /// </summary>
        /// <param name="key"></param>
        /// <param name="doselect"></param>
        /// <returns></returns>
        public SQLBuilder whereIn(string key, Action<SQLBuilder> doselect)
        {
            if (!opened)
            {
                opened = true;
                return this;
            }
            current.whereIn(key, doselect);
            return this;
        }
        /// <summary>
        /// 必须是有效的GUID,否则条件将转为 永远不成立的"1=2";
        /// </summary>
        /// <param name="key"></param>
        /// <param name="OIDs"></param>
        /// <returns></returns>
        public SQLBuilder whereInGuid(string key, IEnumerable<string> OIDs)
        {
            var res = new StringBuilder();
            int cc = 0;
            res.Append("(");
            foreach (var oid in OIDs)
            {
                if (RegxUntils.isGUID(oid))
                {
                    if (cc > 0)
                    {
                        res.Append(",");
                    }
                    res.Append("'");
                    res.Append(oid);
                    res.Append("'");
                    cc++;
                }

            }
            res.Append(")");
            if (cc == 0)
            {
                return where("1=2");
            }
            return where(key + " IN " + res.ToString());
        }
        /// <summary>
        /// 构建where not in 范围值，所有值均参数化。注意：受SQL参数上限影响，请不要传入过大的list。
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <param name="values"></param>
        /// <returns></returns>
        public SQLBuilder whereNotIn<T>(string key, IEnumerable<T> values)
        {
            if (!opened)
            {
                opened = true;
                return this;
            }
            whereListInner(key, " NOT IN ", values);
            return this;
        }
        public SQLBuilder whereNotIn<T>(string key,params T[] values)
        {
            if (!opened)
            {
                opened = true;
                return this;
            }
            whereListInner(key, " NOT IN ", values);
            return this;
        }
        /// <summary>
        /// 构建where not in 范围值，所有值均参数化。注意：受SQL参数上限影响，请不要传入过大的list。
        /// </summary>
        /// <param name="key"></param>
        /// <param name="values"></param>
        /// <returns></returns>
        public SQLBuilder whereNotIn(string key, IEnumerable values)
        {
            if (!opened)
            {
                opened = true;
                return this;
            }
            whereListInner(key, " NOT IN ", values);
            return this;
        }
        /// <summary>
        /// 构建多个字段为某个值的条件，默认无包裹，使用外界的范围
        /// </summary>
        /// <param name="fields"></param>
        /// <param name="value"></param>
        /// <param name="op"></param>
        /// <param name="SinkMode">1为OR，2为AND，0为关闭</param>
        /// <returns></returns>
        public SQLBuilder whereFields(IEnumerable<string> fields,object value,int SinkMode=0, string op = "=")
        {
            if (!opened)
            {
                opened = true;
                return this;
            }
            if (SinkMode == 1)
            {
                sinkOR();
            }
            else if (SinkMode == 2) {
                sink();
            }
            foreach (var field in fields) { 
                where(field, value, op);
            }
            if (SinkMode == 1 || SinkMode == 2) { 
                rise();
            }
            return this;
        }
        /// <summary>
        /// 任意一个字段满足条件，即形成（field1 = val or field2 = val or ...）。等同于 whereAnyFieids 方法。
        /// </summary>
        /// <param name="fields"></param>
        /// <param name="value"></param>
        /// <param name="op"></param>
        /// <returns></returns>
        public SQLBuilder whereAnyFieid(IEnumerable<string> fields, object value,string op = "=")
        {
            return whereFields(fields, value, 1,op);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="value"></param>
        /// <param name="fields"></param>
        /// <returns></returns>
        public SQLBuilder whereAnyFieldIs(object value,params string[] fields)
        {
            return whereFields(fields, value, 1);
        }
        /// <summary>
        /// 所有字段都满足条件,即形成（field1 = val and field2 = val and ...）。等同于 whereAllFieids 方法。
        /// </summary>
        /// <param name="fields"></param>
        /// <param name="value"></param>
        /// <param name="op"></param>
        /// <returns></returns>
        public SQLBuilder whereAllFieid(IEnumerable<string> fields, object value, string op = "=")
        {
            return whereFields(fields, value, 2, op);
        }
        /// <summary>
        /// 创建一个 where key op (list)的SQL条件
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <param name="op"></param>
        /// <param name="values"></param>
        /// <returns></returns>
        public SQLBuilder whereList<T>(string key, string op, IEnumerable<T> values)
        {
            if (!opened)
            {
                opened = true;
                return this;
            }

            whereListInner(key, op, values);
            return this;
        }
        /// <summary>
        /// 增加条件包支持
        /// </summary>
        /// <param name="bag"></param>
        /// <returns></returns>
        public SQLBuilder where(WhereListBag bag) {
            if (!opened)
            {
                opened = true;
                return this;
            }
            if(bag == null) { return this; }
            return whereListInner(bag);
        }

        private SQLBuilder whereListInner(WhereListBag bag)
        {

            WhereFrag field = new WhereFrag();
            field.setConnect(current.whereSeprator);
            field.key = bag.field;
            //                frag.paramKey = string.Format("{0}wh_{1}_{2}_{3}", root.paraSeed,this.key,conditions.Count,_whfragIndex) ;
            var paraKey = paraSeed + "whin_" + current.key + "_" + current.wherePart.Count + "_";


            List<string> names = addListPara(bag.unSafeValues, paraKey);
            var innerSQL = bag.toWhereIn(names);
            if (!string.IsNullOrWhiteSpace(innerSQL))
            {
                field.value = "(" + innerSQL + ")";
                field.paramed = false;
                field.op = bag.op;

                current.where(field);
            }
            else {
                where("1=2");
            }

            return this;
        }

        private SQLBuilder whereListInner<T>(string key, string op, IEnumerable<T> val)
        {
            var bag = WhereListBag.newBag(val);
            bag.field = key;
            bag.op = op;
            return whereListInner(bag);
        }

        private SQLBuilder whereListInner(string key, string op, IEnumerable val)
        {
            var bag = WhereListBag.newBag(val);
            bag.field = key;
            bag.op = op;
            return whereListInner(bag);
        }

        /// <summary>
        /// 创建一个 自定义嵌套 where not in 的 select
        /// </summary>
        /// <param name="key"></param>
        /// <param name="doselect"></param>
        /// <returns></returns>
        public SQLBuilder whereNotIn(string key, Action<SQLBuilder> doselect)
        {
            if (!opened)
            {
                opened = true;
                return this;
            }
            current.where(key, " NOT IN ", doselect);
            return this;
        }


        /// <summary>
        /// where exist 条件
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public SQLBuilder whereExist(string value)
        {
            if (!opened)
            {
                opened = true;
                return this;
            }
            current.whereExist(value);
            return this;
        }


        /// <summary>
        /// 创建 where exits的子查询条件。
        /// </summary>
        /// <param name="doselect"></param>
        /// <returns></returns>
        public SQLBuilder whereExist(Action<SQLBuilder> doselect)
        {
            if (!opened)
            {
                opened = true;
                return this;
            }
            current.whereExist(doselect);
            return this;
        }
        /// <summary>
        /// 创建固定的 where not exists ( YourSQL ) 条件
        /// </summary>
        /// <param name="selectSQL"></param>
        /// <returns></returns>
        public SQLBuilder whereNotExist(string selectSQL)
        {
            this.where(string.Format(" not exists ({0})", selectSQL));
            return this;
        }

        /// <summary>
        /// 创建 where not exists 子查询条件
        /// </summary>
        /// <param name="doselect"></param>
        /// <returns></returns>
        public SQLBuilder whereNotExist(Action<SQLBuilder> doselect)
        {
            if (!opened)
            {
                opened = true;
                return this;
            }
            current.where("", " NOT EXISTS ", doselect);
            return this;
        }
        /// <summary>
        /// 使用一个子查询来构建条件项
        /// </summary>
        /// <param name="key"></param>
        /// <param name="op"></param>
        /// <param name="doselect"></param>
        /// <returns></returns>
        public SQLBuilder where(string key, string op, Action<SQLBuilder> doselect)
        {
            if (!opened)
            {
                opened = true;
                return this;
            }
            current.where(key, op, doselect);
            return this;
        }
        /// <summary>
        /// 创建 where 后面一个 key=#{val}形式的条件。
        /// </summary>
        /// <param name="key"></param>
        /// <param name="doselect"></param>
        /// <returns></returns>
        public SQLBuilder where(string key, Action<SQLBuilder> doselect)
        {
            if (!opened)
            {
                opened = true;
                return this;
            }
            current.where(key, "=", doselect);
            return this;
        }
        /// <summary>
        /// 创建 where 后面一个 key=#{val}形式的条件。
        /// </summary>
        /// <param name="key">where条件的字段部</param>
        /// <param name="val">where条件的字段值</param>
        /// <returns></returns>

        public SQLBuilder where(string key, Object val)
        {
            return where(key, val, "=", true);
        }
        /// <summary>
        /// 判断一个GUID的 值相等条件，如果不是正确的GUID,条件衰减为永不成立的 1=2 
        /// </summary>
        /// <param name="key"></param>
        /// <param name="val"></param>
        /// <returns></returns>
        public SQLBuilder whereGuid(string key, object val)
        {
            if (val == null || val == DBNull.Value)
            {
                return where("1=2");
            }
            if (val is Guid)
            {
                return where(key, val, "=", true);
            }
            else if (RegxUntils.isGUID(val.ToString()))
            {
                return where(key, val, "=", true);
            }
            else
            {
                return where("1=2");
            }
            //return this;
        }

        /// <summary>
        /// 创建 where 后面一个 key op #{val}形式的条件。
        /// </summary>
        /// <param name="key"></param>
        /// <param name="val"></param>
        /// <param name="op"></param>
        /// <returns></returns>
        public SQLBuilder where(string key, Object val, string op)
        {
            return where(key, val, op, true);
        }
        /// <summary>
        /// 字段、值、值类型
        /// </summary>
        /// <param name="key"></param>
        /// <param name="val"></param>
        /// <param name="t"></param>
        /// <returns></returns>
        public SQLBuilder where(string key, Object val, Type t)
        {
            return where(key, val, "=", true, t);
        }
        /// <summary>
        /// 字段、值、比较符、值类型。
        /// </summary>
        /// <param name="key"></param>
        /// <param name="val"></param>
        /// <param name="op"></param>
        /// <param name="t"></param>
        /// <returns></returns>
        public SQLBuilder where(string key, Object val, string op, Type t)
        {
            return where(key, val, op, true, t);
        }
        /// <summary>
        /// 字段、值、比较符、是否参数化。
        /// </summary>
        /// <param name="key"></param>
        /// <param name="val"></param>
        /// <param name="op"></param>
        /// <param name="paramed"></param>
        /// <returns></returns>
        public SQLBuilder where(string key, Object val, string op, bool paramed)
        {
            return where(key, val, op, paramed, null);
        }
        /// <summary>
        /// 添加 where 条件项
        /// </summary>
        /// <param name="key"></param>
        /// <param name="val"></param>
        /// <param name="op"></param>
        /// <param name="paramed"></param>
        /// <returns></returns>
        public SQLBuilder where(string key, Object val, string op, bool paramed, Type t)
        {
            if (!opened)
            {
                opened = true;
                return this;
            }


            if (val == null) return this;

            if (paraRule == ("notEmpty"))
            {
                if (val == null || string.IsNullOrEmpty(val.ToString().Trim()))
                {
                    return this;
                }
            }
            if (paraRule == ("notNull"))
            {
                if (val == null)
                {
                    return this;
                }
            }



            this.current.where(key, val, op, paramed, t);
            return this;
        }
        /// <summary>
        /// 创建 between and 的条件，当任一参数为null时，自动衰减大于、小于 ，都为null,则不执行
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <param name="minValue"></param>
        /// <param name="maxValue"></param>
        /// <returns></returns>
        public SQLBuilder whereBetween<T>(string key, T minValue, T maxValue)
        {
            if (!opened)
            {
                opened = true;
                return this;
            }

            if (maxValue == null && maxValue == null)
            {
                return this;
            }

            if (minValue == null)
            {
                return where(key, maxValue, "<=");
            }
            else if (maxValue == null)
            {
                return where(key, minValue, ">=");
            }


            current.whereFormat(key + " between {0} and {1}", minValue, maxValue);

            return this;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <param name="minValue"></param>
        /// <param name="maxValue"></param>
        /// <returns></returns>
        public SQLBuilder whereNotBetween<T>(string key, T minValue, T maxValue)
        {
            if (!opened)
            {
                opened = true;
                return this;
            }

            if (maxValue == null && maxValue == null)
            {
                return this;
            }

            if (minValue == null)
            {
                return where(key, maxValue, ">");
            }
            else if (maxValue == null)
            {
                return where(key, minValue, "<");
            }


            current.whereFormat(key + " not between {0} and {1}", minValue, maxValue);

            return this;
        }
        /// <summary>
        /// 使用字符串模板进行格式化。参数放入到SQL参数中。格式为{0} {1} {2} 等标准化的c# String.format语法。
        /// </summary>
        /// <param name="template"></param>
        /// <param name="values"></param>
        /// <returns></returns>

        public SQLBuilder whereFormat(string template, params Object[] values)
        {
            if (!opened)
            {
                opened = true;
                return this;
            }
            current.whereFormat(template, values);
            return this;
        }


        /// <summary>
        /// 开始构造复制的where条件，调用end结束
        /// </summary>
        /// <returns></returns>

        public WhereItem start()
        {
            return start(true);
        }
        /// <summary>
        /// 开始一个where or部分
        /// </summary>
        /// <param name="addBracket"></param>
        /// <returns></returns>
        public WhereItem start(bool addBracket)
        {
            WhereItem wh = new WhereItem(this);
            wh.autoKuohao = addBracket;
            return wh;
        }
        /// <summary>
        /// 每创建一次，该指针增加，防止兄弟构造器的参数重复。
        /// </summary>
        private int _brotherIndex = 0;
        /// <summary>
        /// 获取一个共用参数体的独立构造器。
        /// </summary>
        /// <returns></returns>
        public SQLBuilder getBrotherBuilder()
        {
            _brotherIndex++;
            var builder = new SQLBuilder();
            builder.ps = this.ps;
            builder.current.ps = this.ps;
            builder.current.position = this.position;
            builder.setDBInstance(DBLive);
            builder.level = level + _brotherIndex;
            var oldseed = seed.Replace("lv" + level + "_", "");
            builder.setSeed( oldseed + "lv" + builder.level + "_");
            builder.init();
            return builder;
        }
        /// <summary>
        /// 复制一个拥有相同数据库连接位的实例；不复制任何其它配置参数。
        /// </summary>
        /// <returns></returns>
        public SQLBuilder copy()
        {
            var builder = new SQLBuilder();
            builder.setDBInstance(DBLive);
            return builder;
        }




        /// <summary>
        /// 使用一个子项 SQLBuilder来创建一个 where条件，构建作为条件项，自动括号包裹，该子项仅where方法生效。
        /// </summary>
        /// <param name="whereBuilder"></param>
        /// <returns></returns>
        public SQLBuilder where(Action<SQLBuilder> whereBuilder)
        {
            current.where(whereBuilder);
            _whereCount++;
            return this;
        }

        #endregion
    }
}

