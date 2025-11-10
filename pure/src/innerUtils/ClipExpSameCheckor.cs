// 基础功能说明：

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using mooSQL.data.linq;
using mooSQL.linq;

namespace mooSQL.linq;
/// <summary>
/// 用于比较两个表达式是否相同。对于常量表达式，仅按类型进行比较。
/// </summary>
public sealed class ClipExpSameCheckor : IEqualityComparer<Expression?>
{

    public List<ConstantExpression> constantVals {  get; private set; }
    /// <summary>
    /// 私有构造函数，防止外部实例化。
    /// </summary>
    public ClipExpSameCheckor()
    {
        constantVals = new List<ConstantExpression>();
    }


    public int GetHashCode(Expression obj)
    {

        if (obj == null)
        {
            return 0;
        }

        unchecked
        {
            var hash = new HashCode();
            hash.Add(obj.NodeType);
            hash.Add(obj.Type);

            switch (obj)
            {
                case BinaryExpression binaryExpression:
                    hash.Add(binaryExpression.Left, this);
                    hash.Add(binaryExpression.Right, this);
                    AddExpressionToHashIfNotNull(binaryExpression.Conversion);
                    AddToHashIfNotNull(binaryExpression.Method);

                    break;

                case BlockExpression blockExpression:
                    AddListToHash(blockExpression.Variables);
                    AddListToHash(blockExpression.Expressions);
                    break;

                case ConditionalExpression conditionalExpression:
                    hash.Add(conditionalExpression.Test, this);
                    hash.Add(conditionalExpression.IfTrue, this);
                    hash.Add(conditionalExpression.IfFalse, this);
                    break;

                case ConstantExpression constantExpression:
                    constantVals.Add(constantExpression);
                    switch (constantExpression.Value)
                    {
                        case IQueryable:
                        case null:
                            break;

                        case IStructuralEquatable structuralEquatable:
                            hash.Add(structuralEquatable.GetHashCode(StructuralComparisons.StructuralEqualityComparer));
                            break;

                        default:
                            hash.Add(constantExpression.Value.GetType());
                            break;
                    }

                    break;

                case DefaultExpression:
                    // Intentionally empty. No additional members
                    break;

                case GotoExpression gotoExpression:
                    hash.Add(gotoExpression.Value, this);
                    hash.Add(gotoExpression.Kind);
                    hash.Add(gotoExpression.Target);
                    break;

                case IndexExpression indexExpression:
                    hash.Add(indexExpression.Object, this);
                    AddListToHash(indexExpression.Arguments);
                    hash.Add(indexExpression.Indexer);
                    break;

                case InvocationExpression invocationExpression:
                    hash.Add(invocationExpression.Expression, this);
                    AddListToHash(invocationExpression.Arguments);
                    break;

                case LabelExpression labelExpression:
                    AddExpressionToHashIfNotNull(labelExpression.DefaultValue);
                    hash.Add(labelExpression.Target);
                    break;

                case LambdaExpression lambdaExpression:
                    hash.Add(lambdaExpression.Body, this);
                    AddListToHash(lambdaExpression.Parameters);
                    hash.Add(lambdaExpression.ReturnType);
                    break;

                case ListInitExpression listInitExpression:
                    hash.Add(listInitExpression.NewExpression, this);
                    AddInitializersToHash(listInitExpression.Initializers);
                    break;

                case LoopExpression loopExpression:
                    hash.Add(loopExpression.Body, this);
                    AddToHashIfNotNull(loopExpression.BreakLabel);
                    AddToHashIfNotNull(loopExpression.ContinueLabel);
                    break;

                case MemberExpression memberExpression:
                    hash.Add(memberExpression.Expression, this);
                    hash.Add(memberExpression.Member);
                    break;

                case MemberInitExpression memberInitExpression:
                    hash.Add(memberInitExpression.NewExpression, this);
                    AddMemberBindingsToHash(memberInitExpression.Bindings);
                    break;

                case MethodCallExpression methodCallExpression:
                    hash.Add(methodCallExpression.Object, this);
                    AddListToHash(methodCallExpression.Arguments);
                    hash.Add(methodCallExpression.Method);
                    break;

                case NewArrayExpression newArrayExpression:
                    AddListToHash(newArrayExpression.Expressions);
                    break;

                case NewExpression newExpression:
                    AddListToHash(newExpression.Arguments);
                    hash.Add(newExpression.Constructor);

                    if (newExpression.Members != null)
                    {
                        for (var i = 0; i < newExpression.Members.Count; i++)
                        {
                            hash.Add(newExpression.Members[i]);
                        }
                    }

                    break;

                case ParameterExpression parameterExpression:
                    AddToHashIfNotNull(parameterExpression.Name);
                    break;

                case RuntimeVariablesExpression runtimeVariablesExpression:
                    AddListToHash(runtimeVariablesExpression.Variables);
                    break;

                case SwitchExpression switchExpression:
                    hash.Add(switchExpression.SwitchValue, this);
                    AddExpressionToHashIfNotNull(switchExpression.DefaultBody);
                    AddToHashIfNotNull(switchExpression.Comparison);
                    for (var i = 0; i < switchExpression.Cases.Count; i++)
                    {
                        var @case = switchExpression.Cases[i];
                        hash.Add(@case.Body, this);
                        AddListToHash(@case.TestValues);
                    }

                    break;

                case TryExpression tryExpression:
                    hash.Add(tryExpression.Body, this);
                    AddExpressionToHashIfNotNull(tryExpression.Fault);
                    AddExpressionToHashIfNotNull(tryExpression.Finally);
                    if (tryExpression.Handlers != null)
                    {
                        for (var i = 0; i < tryExpression.Handlers.Count; i++)
                        {
                            var handler = tryExpression.Handlers[i];
                            hash.Add(handler.Body, this);
                            AddExpressionToHashIfNotNull(handler.Variable);
                            AddExpressionToHashIfNotNull(handler.Filter);
                            hash.Add(handler.Test);
                        }
                    }

                    break;

                case TypeBinaryExpression typeBinaryExpression:
                    hash.Add(typeBinaryExpression.Expression, this);
                    hash.Add(typeBinaryExpression.TypeOperand);
                    break;

                case UnaryExpression unaryExpression:
                    hash.Add(unaryExpression.Operand, this);
                    AddToHashIfNotNull(unaryExpression.Method);
                    break;

                default:
                    if (obj.NodeType == ExpressionType.Extension)
                    {
                        hash.Add(obj);
                        break;
                    }

                    throw new NotSupportedException(string.Format("不支持的类型：{0}",obj.NodeType));
            }

            return hash.ToHashCode();

            void AddToHashIfNotNull(object? t)
            {
                if (t != null)
                {
                    hash.Add(t);
                }
            }

            void AddExpressionToHashIfNotNull(Expression? t)
            {
                if (t != null)
                {
                    hash.Add(t, this);
                }
            }

            void AddListToHash<T>(IReadOnlyList<T> expressions)
                where T : Expression
            {
                for (var i = 0; i < expressions.Count; i++)
                {
                    hash.Add(expressions[i], this);
                }
            }

            void AddInitializersToHash(IReadOnlyList<ElementInit> initializers)
            {
                for (var i = 0; i < initializers.Count; i++)
                {
                    AddListToHash(initializers[i].Arguments);
                    hash.Add(initializers[i].AddMethod);
                }
            }

            void AddMemberBindingsToHash(IReadOnlyList<MemberBinding> memberBindings)
            {
                for (var i = 0; i < memberBindings.Count; i++)
                {
                    var memberBinding = memberBindings[i];

                    hash.Add(memberBinding.Member);
                    hash.Add(memberBinding.BindingType);

                    switch (memberBinding)
                    {
                        case MemberAssignment memberAssignment:
                            hash.Add(memberAssignment.Expression, this);
                            break;

                        case MemberListBinding memberListBinding:
                            AddInitializersToHash(memberListBinding.Initializers);
                            break;

                        case MemberMemberBinding memberMemberBinding:
                            AddMemberBindingsToHash(memberMemberBinding.Bindings);
                            break;
                    }
                }
            }
        }
    }

    /// <inheritdoc />
    public bool Equals(Expression? x, Expression? y)
        => new ExpressionComparer().Compare(x, y);


}