



编制生命期


输入物  Expression 


中间产物 SqlStatement


最终产物 字符串SQL




第一编译阶段
    环境  IBuildContext
    编译器 
        ExpressionBuilder
        ISequenceBuilder
    动作
        → ExpressionBuilder.BuildSequence() //
            → ExpressionBuilder.TryBuildSequence()
                → ExpressionBuilder.ExpandToRoot()
                → ExpressionBuilder.TryFindBuilder() //此处会根据节点类型，调用不同的Builder
                → ISequenceBuilder.BuildSequence()
                    →TakeSkipBuilder.BuildMethodCall() --分支众多
                        →ExpressionBuilder.BuildTake()

第二编译阶段

    动作
        → ExpressionBuilder.BuildQuery()
            → ExpressionBuilder.MakeExpression()
                → IBuildContext.MakeExpression()  //此处产出 SqlPlaceholderExpression 类
                    → Builder.ConvertToSqlExpr()
                    → ExpressionBuilder.CollectPlaceholders()
                    → ExpressionBuilder.CreatePlaceholder()
            → ExpressionBuilder.FinalizeProjection()
