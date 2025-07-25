using System;
using System.Collections.Generic;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

using System.Text;
using System.Threading.Tasks;
using mooSQL.data;
using mooSQL.data.call;



namespace mooSQL.linq
{

    internal partial class FastMethodVisitor : MethodVisitor
    {
        /// <summary>
        /// 合作的表达式访问器 搭档。当处理过程中需要临时移交给ExpressionVisitor进行处理时，交给它。
        /// </summary>
        public ExpressionVisitor Buddy { get; set; }



        public FastCompileContext Context { get; set; }
        private ValueExpressionVisitor valueVisitor;
        /// <summary>
        /// 构造函数，初始化表达式访问器。
        /// </summary>
        public FastMethodVisitor() {
            valueVisitor = new ValueExpressionVisitor();
        }

        public override MethodCall VisitExpression(ExpressionCall method)
        {
            //由于本访问器，在表达式访问器的下层，遇到表达式时，直接返回，交给上层处理即可。
            return method;
        }




        #region 具体访问者

        public override MethodCall VisitAlias(AliasCall method)
        {
            var argu = method.Arguments[0];
            var next = Buddy.Visit(argu);
            return method.Expression(next);
        }

        public override MethodCall VisitAll(AllCall method)
        {

            return base.VisitAll(method);
        }

        public override MethodCall VisitCount(CountCall method)
        {
            Context.RunType = LayerRunType.select;
            //访问调用者
            var argu = method.Arguments[0];
            var next = Buddy.Visit(argu);
            //
            checkBeforeRun(LayerRunType.select);

            Context.onRunQuery = (context) =>
            {

                return Context.TopLayer.Root.count();
            };
            return method;
        }

        public override MethodCall VisitGroupBy(GroupByCall method)
        {

            var argu = method.Arguments[0];
            var next = Buddy.Visit(argu);

            if (method.Arguments.Count > 1)
            {
                var p2 = method.Arguments[1];
                var fv = new FieldVisitor(Context, true);
                var fie = fv.FindField(p2);
                if (fie != null)
                {
                    Context.TopLayer.Current.groupBy(fie);
                }
            }
            return base.VisitGroupBy(method);
        
        }
        public override MethodCall VisitInjectSQL(InjectSQLCall method)
        {
            //访问调用者
            var argu = method.Arguments[0];
            var next = Buddy.Visit(argu);
            if (method.Arguments.Count > 1)
            {
                var condi = method.Arguments[1];
                if (condi is ConstantExpression cont) { 
                    var val= cont.Value;
                    if (val is Action<SQLBuilder, FastCompileContext> act) {
                        act(Context.CurrentLayer.Current, Context);
                    }
                }
            }
            return base.VisitInjectSQL(method);
        }

        public override MethodCall VisitWhere(WhereCall method)
        {
            //访问调用者
            var argu = method.Arguments[0];
            var next = Buddy.Visit(argu);

            if (method.Arguments.Count > 1)
            {
                var condi= method.Arguments[1];
                var wok = new WhereExpressionVisitor(Context);
                wok.Visit(condi);
            }
            return base.VisitWhere(method);
        }

        public override MethodCall VisitSingle(SingleCall method)
        {
            //访问调用者
            var argu = method.Arguments[0];
            var next = Buddy.Visit(argu);
            //
            checkBeforeRun(LayerRunType.select);
            //查询唯一的一个结果
            Context.onRunQuery = (context) =>
            {
                var gtype = method.MethodInfo.ReturnType;
                var mothod = this.GetType().GetMethod("ExecuteQuerySingleT", BindingFlags.NonPublic | BindingFlags.Instance);
                var mot = mothod.MakeGenericMethod(gtype);

                var para = new object[2] { Context.TopLayer.Root ,context};
                var res= mot.Invoke(this, para);
                return res;

            };
            return method;
        }

        public override MethodCall VisitSelect(SelectCall method)
        {

            var argu = method.Arguments[0];
            var next = Buddy.Visit(argu);

            if (method.Arguments.Count > 1)
            {
                var p2 = method.Arguments[1];
                var fv = new FieldVisitor(Context, true);
                var fie = fv.FindField(p2);
                if (fie != null)
                {
                    Context.TopLayer.Current.select(fie);
                }
            }
            return base.VisitSelect(method);

        
        }

        public override MethodCall VisitSet(SetCall method)
        {
            //访问调用者
            var argu = method.Arguments[0];
            var next = Buddy.Visit(argu);

            //2个参数的set，是单个字段赋值
            if (method.Arguments.Count == 3) { 
                var fieldVisitor= new FieldVisitor(Context,false);
                var fie = fieldVisitor.Visit(method.Arguments[1]);

                var v = valueVisitor.Visit(method.Arguments[2]);

                if (fie is StringExpression str && v is ConstantExpression c) {
                    Context.CurrentLayer.Current.set(str.Value, c.Value);
                }
                

            }

            return base.VisitSet(method);
        }

        public override MethodCall VisitSetPage(SetPageCall method)
        {
            var argu = method.Arguments[0];
            var next = Buddy.Visit(argu);
            if (method.Arguments.Count == 3) { 
                var size= method.Arguments[1];
                var num = method.Arguments[2];
                this.ApplySetPage(size, num);
            }
            return method;
        }

        private void ApplySetPage(Expression size, Expression num) {
            if (size is ConstantExpression sizeE && num is ConstantExpression numE)
            {
                var sizeV = (int)sizeE.Value;
                var numV = (int)numE.Value;
                Context.CurrentLayer.Current.setPage(sizeV, numV);
            }
        }

        public override MethodCall VisitSink(SinkCall method)
        {
            var argu = method.Arguments[0];
            var next = Buddy.Visit(argu);

            Context.CurrentLayer.Current.sink();
            return method;
        }

        public override MethodCall VisitSinkOR(SinkORCall method)
        {
            var argu = method.Arguments[0];
            var next = Buddy.Visit(argu);

            Context.CurrentLayer.Current.sinkOR();
            return method;
        }

        public override MethodCall VisitRise(RiseCall method)
        {
            var argu = method.Arguments[0];
            var next = Buddy.Visit(argu);

            Context.CurrentLayer.Current.rise();
            return method;
        }


        public override MethodCall VisitDoUpdate(DoUpdateCall method)
        {
            Context.RunType = LayerRunType.update;
            //访问调用者
            var argu = method.Arguments[0];
            var next = Buddy.Visit(argu);

            this.checkBeforeRun(LayerRunType.update);

            Context.onRunQuery = (context) =>
            {
                return Context.TopLayer.Root.doUpdate();
            };

            return base.VisitDoUpdate(method);
        }

        public override MethodCall VisitDoDelete(DoDeleteCall method)
        {
            Context.RunType = LayerRunType.delete;
            //访问调用者
            var argu = method.Arguments[0];
            var next = Buddy.Visit(argu);

            this.checkBeforeRun(LayerRunType.delete);

            Context.onRunQuery = (context) =>
            {
                return Context.TopLayer.Root.doDelete();
            };

            return method;
        }
        #endregion

        private void checkBeforeRun(LayerRunType type) {
            //调试用，打印SQL
            Context.TopLayer.Root.print((sq) =>
            {
                Console.WriteLine(sq);
            });

            if (Context.EntityType != null) { 

                Context.CurrentLayer.suck(Context.EntityType);
                
            }
            Context.TopLayer.PrepareRun(type);

        }
        public virtual Func<QueryContext, TResult> WhenRunQuery<TResult>(QueryContext context)
        {
            checkBeforeRun( LayerRunType.select);
            var resType = typeof(TResult);
            if (resType.IsGenericType && resType.GetGenericTypeDefinition() == typeof(IEnumerable<>))
            {

                var gtype = resType.GenericTypeArguments[0];
                var mothod = this.GetType().GetMethod("ExecuteQueryEnumT", BindingFlags.NonPublic | BindingFlags.Instance);
                var mot = mothod.MakeGenericMethod(gtype);
                return (cont) =>
                {
                    var para= new object[1] { Context.TopLayer.Root };
                    return (TResult)mot.Invoke(this, para);
                };
            }
            return null;
        }

        #region from部分

        public override MethodCall VisitIncludes(IncludesCall method)
        {
            var argu = method.Arguments[0];
            var next = Buddy.Visit(argu);
            //
            if (method.Arguments.Count > 1) { 
                var fv = new FieldVisitor(Context, false);
                var p1=method.Arguments[1];
                var tar= fv.FindFieldCol(p1);
                if (tar != null) {
                    //存入导航记录
                    //如果没有导航类的类型，还需要进行一下解析
                    if (tar.Navigat.ChildType == null) {
                        var lmdFind = new ExpressionFindVisitor<LambdaExpression>();
                        var lambda = lmdFind.Find(p1);
                        if (lambda != null) { 
                            
                        }
                    }
                    Context.AddNavTarget(tar.belongTable.Type,tar);
                }
            }


            return base.VisitIncludes(method);
        }

        public override MethodCall VisitJoin(JoinCall method)
        {
            var argu = method.Arguments[0];
            var next = Buddy.Visit(argu);
            if (method.Arguments.Count == 3) { 
                //3参写法，此时为超自由的写法
                var p2=method.Arguments[1];
                var onPart= method.Arguments[2];
                var findv= new ExpressionFindVisitor<ConstantExpression>();
                var p2Tar= findv.Find(p2);
                var joinString = "JOIN";
                if (p2Tar != null) { 
                    joinString = p2Tar.ToString();
                }
                BuildJoin(onPart, joinString);
            }

            return base.VisitJoin(method);
        }
        public override MethodCall VisitInnerJoin(InnerJoinCall method)
        {

            if (method.Arguments.Count == 1)
            {
                //只有一个参数的为基础
                var exp = method.Arguments[0];
                BuildJoin(exp, "JOIN");

            }

            return method;
        }

        public override MethodCall VisitLeftJoin(LeftJoinCall method)
        {

            if (method.Arguments.Count == 1) { 
                //只有一个参数的为基础
                var exp= method.Arguments[0];
                BuildJoin(exp,"LEFT JOIN");

            }

            return base.VisitLeftJoin(method);
        }

        private void BuildJoin(Expression exp,string joinString) {
            var lmdFind = new ExpressionFindVisitor<LambdaExpression>();
            var lambda = lmdFind.Find(exp);
            if (lambda != null)
            {
                //添加左右参数到表集合
                foreach (var p in lambda.Parameters)
                {
                    this.Context.CurrentLayer.suck(p.Type, p.Name);
                }
                var p1= lambda.Parameters[0];
                //注册表1的SQL
                Context.CurrentLayer.register(p1.Name, p1.Type,Context.RunType);
                var body = lambda.Body;
                var runner = new JoinOnExpressionVisitor(this.Context.DB);
                runner.CopyLayer(Context.CurrentLayer);
                var cmd = runner.Visit(exp);
                if (cmd.para.Count > 0) {
                    Context.CurrentLayer.Current.ps.Copy(cmd.para);
                }
                var onstr= cmd.sql;
                var p2=lambda.Parameters[1];
                Context.CurrentLayer.registerJoin(p2.Name, joinString, onstr, p2.Type, Context.RunType);

            }
        }

        public override MethodCall VisitRightJoin(RightJoinCall method)
        {
            if (method.Arguments.Count == 1)
            {
                //只有一个参数的为基础
                var exp = method.Arguments[0];
                BuildJoin(exp, "RIGHT JOIN");

            }

            return method;
        }

        public override MethodCall VisitTake(TakeCall method)
        {
            var argu = method.Arguments[0];
            var next = Buddy.Visit(argu);

            if (method.Arguments.Count == 2) {
                var p2 = method.Arguments[1];
                if (p2 is ConstantExpression val && p2.Type == typeof(int) && val.Value !=null) {
                    var v = (int)val.Value;
                    Context.CurrentLayer.Current.top(v);
                }
            }
            return base.VisitTake(method);
        }

        public override MethodCall VisitTop(TopCall method)
        {
            var argu = method.Arguments[0];
            var next = Buddy.Visit(argu);

            if (method.Arguments.Count == 2)
            {
                var p2 = method.Arguments[1];
                if (p2 is ConstantExpression val && p2.Type == typeof(int) && val.Value != null)
                {
                    var v = (int)val.Value;
                    Context.CurrentLayer.Current.top(v);
                }
            }
            return method;
        }

        public override MethodCall VisitToPageList(ToPageListCall method)
        {
            var argu = method.Arguments[0];
            var next = Buddy.Visit(argu);

            //分为1参 、3参种
            if (method.Arguments.Count == 3)
            {
                var size = method.Arguments[1];
                var num = method.Arguments[2];
                this.ApplySetPage(size, num);
            }

            ApplyToPage(method);

            return method;
        }

        private void ApplyToPage(MethodCall method) {
            checkBeforeRun(LayerRunType.select);

            Context.onRunQuery = (context) =>
            {
                var gtype = method.MethodInfo.ReturnType;
                if (gtype.IsGenericType && gtype.GenericTypeArguments.Length > 0) { 
                    gtype = gtype.GenericTypeArguments[0];
                }
                var mothod = this.GetType().GetMethod("ExecuteQueryPageT", BindingFlags.NonPublic | BindingFlags.Instance);
                var mot = mothod.MakeGenericMethod(gtype);

                var para = new object[2] { Context.TopLayer.Root, context };
                var res = mot.Invoke(this, para);
                return res;
            };

        }

        public override MethodCall VisitOrderBy(OrderByCall method)
        {
            var argu = method.Arguments[0];
            var next = Buddy.Visit(argu);

            if (method.Arguments.Count > 1) { 
                var p2= method.Arguments[1];
                var fv = new FieldVisitor(Context, true);
                var fie= fv.FindField(p2);
                if (fie != null) {
                    Context.TopLayer.Current.orderBy(fie);
                }
            }
            return base.VisitOrderBy(method);
        }
        #endregion



    }
}
