using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data.linq
{
    internal struct ExpressionComparer
    {
        private Dictionary<ParameterExpression, ParameterExpression> _parameterScope;

        public bool Compare(Expression? left, Expression? right)
        {
            if (left == right)
            {
                return true;
            }

            if (left == null
                || right == null)
            {
                return false;
            }

            if (left.NodeType != right.NodeType)
            {
                return false;
            }

            if (left.Type != right.Type)
            {
                return false;
            }

            return left switch
            {
                BinaryExpression leftBinary => CompareBinary(leftBinary, (BinaryExpression)right),
                BlockExpression leftBlock => CompareBlock(leftBlock, (BlockExpression)right),
                ConditionalExpression leftConditional => CompareConditional(leftConditional, (ConditionalExpression)right),
                ConstantExpression leftConstant => CompareConstant(leftConstant, (ConstantExpression)right),
                DefaultExpression => true, // Intentionally empty. No additional members
                GotoExpression leftGoto => CompareGoto(leftGoto, (GotoExpression)right),
                IndexExpression leftIndex => CompareIndex(leftIndex, (IndexExpression)right),
                InvocationExpression leftInvocation => CompareInvocation(leftInvocation, (InvocationExpression)right),
                LabelExpression leftLabel => CompareLabel(leftLabel, (LabelExpression)right),
                LambdaExpression leftLambda => CompareLambda(leftLambda, (LambdaExpression)right),
                ListInitExpression leftListInit => CompareListInit(leftListInit, (ListInitExpression)right),
                LoopExpression leftLoop => CompareLoop(leftLoop, (LoopExpression)right),
                MemberExpression leftMember => CompareMember(leftMember, (MemberExpression)right),
                MemberInitExpression leftMemberInit => CompareMemberInit(leftMemberInit, (MemberInitExpression)right),
                MethodCallExpression leftMethodCall => CompareMethodCall(leftMethodCall, (MethodCallExpression)right),
                NewArrayExpression leftNewArray => CompareNewArray(leftNewArray, (NewArrayExpression)right),
                NewExpression leftNew => CompareNew(leftNew, (NewExpression)right),
                ParameterExpression leftParameter => CompareParameter(leftParameter, (ParameterExpression)right),
                RuntimeVariablesExpression leftRuntimeVariables => CompareRuntimeVariables(
                    leftRuntimeVariables, (RuntimeVariablesExpression)right),
                SwitchExpression leftSwitch => CompareSwitch(leftSwitch, (SwitchExpression)right),
                TryExpression leftTry => CompareTry(leftTry, (TryExpression)right),
                TypeBinaryExpression leftTypeBinary => CompareTypeBinary(leftTypeBinary, (TypeBinaryExpression)right),
                UnaryExpression leftUnary => CompareUnary(leftUnary, (UnaryExpression)right),

                _ => left.NodeType == ExpressionType.Extension
                    ? left.Equals(right)
                    : throw new InvalidOperationException(string.Format("不支持的类型：{0}", left.NodeType))
            };
        }

        private bool CompareBinary(BinaryExpression a, BinaryExpression b)
            => Equals(a.Method, b.Method)
                && a.IsLifted == b.IsLifted
                && a.IsLiftedToNull == b.IsLiftedToNull
                && Compare(a.Left, b.Left)
                && Compare(a.Right, b.Right)
                && Compare(a.Conversion, b.Conversion);

        private bool CompareBlock(BlockExpression a, BlockExpression b)
            => CompareExpressionList(a.Variables, b.Variables)
                && CompareExpressionList(a.Expressions, b.Expressions);

        private bool CompareConditional(ConditionalExpression a, ConditionalExpression b)
            => Compare(a.Test, b.Test)
                && Compare(a.IfTrue, b.IfTrue)
                && Compare(a.IfFalse, b.IfFalse);

        private static bool CompareConstant(ConstantExpression a, ConstantExpression b)
        {
            //var (v1, v2) = (a.Value, b.Value);
            var v1 = a.Value;
            var v2 = b.Value;
            return Equals(v1, v2)
                || (v1 is IStructuralEquatable array1 && array1.Equals(v2, StructuralComparisons.StructuralEqualityComparer));
        }

        private bool CompareGoto(GotoExpression a, GotoExpression b)
            => a.Kind == b.Kind
                && Equals(a.Target, b.Target)
                && Compare(a.Value, b.Value);

        private bool CompareIndex(IndexExpression a, IndexExpression b)
            => Equals(a.Indexer, b.Indexer)
                && Compare(a.Object, b.Object)
                && CompareExpressionList(a.Arguments, b.Arguments);

        private bool CompareInvocation(InvocationExpression a, InvocationExpression b)
            => Compare(a.Expression, b.Expression)
                && CompareExpressionList(a.Arguments, b.Arguments);

        private bool CompareLabel(LabelExpression a, LabelExpression b)
            => Equals(a.Target, b.Target)
                && Compare(a.DefaultValue, b.DefaultValue);

        private bool CompareLambda(LambdaExpression a, LambdaExpression b)
        {
            var n = a.Parameters.Count;

            if (b.Parameters.Count != n)
            {
                return false;
            }

            _parameterScope ??= new Dictionary<ParameterExpression, ParameterExpression>();

            for (var i = 0; i < n; i++)
            {
                //var (p1, p2) = (a.Parameters[i], b.Parameters[i]);
                var p1 = a.Parameters[i];
                var p2 = b.Parameters[i];


                if (p1.Type != p2.Type)
                {
                    for (var j = 0; j < i; j++)
                    {
                        _parameterScope.Remove(a.Parameters[j]);
                    }

                    return false;
                }
#if NET5_0_OR_GREATER
                if (!_parameterScope.TryAdd(p1, p2))
                {
                    throw new InvalidOperationException(string.Format("不支持的类型：{0}", p1.Name));
                }
#else
                _parameterScope.Add(p1, p2);
#endif

            }

            try
            {
                return Compare(a.Body, b.Body);
            }
            finally
            {
                for (var i = 0; i < n; i++)
                {
                    _parameterScope.Remove(a.Parameters[i]);
                }
            }
        }

        private bool CompareListInit(ListInitExpression a, ListInitExpression b)
            => Compare(a.NewExpression, b.NewExpression)
                && CompareElementInitList(a.Initializers, b.Initializers);

        private bool CompareLoop(LoopExpression a, LoopExpression b)
            => Equals(a.BreakLabel, b.BreakLabel)
                && Equals(a.ContinueLabel, b.ContinueLabel)
                && Compare(a.Body, b.Body);

        private bool CompareMember(MemberExpression a, MemberExpression b)
            => Equals(a.Member, b.Member)
                && Compare(a.Expression, b.Expression);

        private bool CompareMemberInit(MemberInitExpression a, MemberInitExpression b)
            => Compare(a.NewExpression, b.NewExpression)
                && CompareMemberBindingList(a.Bindings, b.Bindings);

        private bool CompareMethodCall(MethodCallExpression a, MethodCallExpression b)
            => Equals(a.Method, b.Method)
                && Compare(a.Object, b.Object)
                && CompareExpressionList(a.Arguments, b.Arguments);

        private bool CompareNewArray(NewArrayExpression a, NewArrayExpression b)
            => CompareExpressionList(a.Expressions, b.Expressions);

        private bool CompareNew(NewExpression a, NewExpression b)
            => Equals(a.Constructor, b.Constructor)
                && CompareExpressionList(a.Arguments, b.Arguments)
                && CompareMemberList(a.Members, b.Members);

        private bool CompareParameter(ParameterExpression a, ParameterExpression b)
            => _parameterScope != null
                && _parameterScope.TryGetValue(a, out var mapped)
                    ? mapped.Name == b.Name
                    : a.Name == b.Name;

        private bool CompareRuntimeVariables(RuntimeVariablesExpression a, RuntimeVariablesExpression b)
            => CompareExpressionList(a.Variables, b.Variables);

        private bool CompareSwitch(SwitchExpression a, SwitchExpression b)
            => Equals(a.Comparison, b.Comparison)
                && Compare(a.SwitchValue, b.SwitchValue)
                && Compare(a.DefaultBody, b.DefaultBody)
                && CompareSwitchCaseList(a.Cases, b.Cases);

        private bool CompareTry(TryExpression a, TryExpression b)
            => Compare(a.Body, b.Body)
                && Compare(a.Fault, b.Fault)
                && Compare(a.Finally, b.Finally)
                && CompareCatchBlockList(a.Handlers, b.Handlers);

        private bool CompareTypeBinary(TypeBinaryExpression a, TypeBinaryExpression b)
            => a.TypeOperand == b.TypeOperand
                && Compare(a.Expression, b.Expression);

        private bool CompareUnary(UnaryExpression a, UnaryExpression b)
            => Equals(a.Method, b.Method)
                && a.IsLifted == b.IsLifted
                && a.IsLiftedToNull == b.IsLiftedToNull
                && Compare(a.Operand, b.Operand);

        private bool CompareExpressionList(IReadOnlyList<Expression> a, IReadOnlyList<Expression> b)
        {
            if (ReferenceEquals(a, b))
            {
                return true;
            }

            if (a == null
                || b == null
                || a.Count != b.Count)
            {
                return false;
            }

            for (int i = 0, n = a.Count; i < n; i++)
            {
                if (!Compare(a[i], b[i]))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool CompareMemberList(IReadOnlyList<MemberInfo>? a, IReadOnlyList<MemberInfo>? b)
        {
            if (ReferenceEquals(a, b))
            {
                return true;
            }

            if (a == null
                || b == null
                || a.Count != b.Count)
            {
                return false;
            }

            for (int i = 0, n = a.Count; i < n; i++)
            {
                if (!Equals(a[i], b[i]))
                {
                    return false;
                }
            }

            return true;
        }

        private bool CompareMemberBindingList(IReadOnlyList<MemberBinding> a, IReadOnlyList<MemberBinding> b)
        {
            if (ReferenceEquals(a, b))
            {
                return true;
            }

            if (a == null
                || b == null
                || a.Count != b.Count)
            {
                return false;
            }

            for (int i = 0, n = a.Count; i < n; i++)
            {
                if (!CompareBinding(a[i], b[i]))
                {
                    return false;
                }
            }

            return true;
        }

        private bool CompareBinding(MemberBinding a, MemberBinding b)
        {
            if (a == b)
            {
                return true;
            }

            if (a == null
                || b == null)
            {
                return false;
            }

            if (a.BindingType != b.BindingType)
            {
                return false;
            }

            if (!Equals(a.Member, b.Member))
            {
                return false;
            }

#pragma warning disable IDE0066 // Convert switch statement to expression
            switch (a)
#pragma warning restore IDE0066 // Convert switch statement to expression
            {
                case MemberAssignment aMemberAssignment:
                    return Compare(aMemberAssignment.Expression, ((MemberAssignment)b).Expression);

                case MemberListBinding aMemberListBinding:
                    return CompareElementInitList(aMemberListBinding.Initializers, ((MemberListBinding)b).Initializers);

                case MemberMemberBinding aMemberMemberBinding:
                    return CompareMemberBindingList(aMemberMemberBinding.Bindings, ((MemberMemberBinding)b).Bindings);

                default:
                    throw new InvalidOperationException(string.Format("不支持的类型：{0}", a.BindingType));
            }
        }

        private bool CompareElementInitList(IReadOnlyList<ElementInit> a, IReadOnlyList<ElementInit> b)
        {
            if (ReferenceEquals(a, b))
            {
                return true;
            }

            if (a == null
                || b == null
                || a.Count != b.Count)
            {
                return false;
            }

            for (int i = 0, n = a.Count; i < n; i++)
            {
                if (!CompareElementInit(a[i], b[i]))
                {
                    return false;
                }
            }

            return true;
        }

        private bool CompareElementInit(ElementInit a, ElementInit b)
            => Equals(a.AddMethod, b.AddMethod)
                && CompareExpressionList(a.Arguments, b.Arguments);

        private bool CompareSwitchCaseList(IReadOnlyList<SwitchCase> a, IReadOnlyList<SwitchCase> b)
        {
            if (ReferenceEquals(a, b))
            {
                return true;
            }

            if (a == null
                || b == null
                || a.Count != b.Count)
            {
                return false;
            }

            for (int i = 0, n = a.Count; i < n; i++)
            {
                if (!CompareSwitchCase(a[i], b[i]))
                {
                    return false;
                }
            }

            return true;
        }

        private bool CompareSwitchCase(SwitchCase a, SwitchCase b)
            => Compare(a.Body, b.Body)
                && CompareExpressionList(a.TestValues, b.TestValues);

        private bool CompareCatchBlockList(IReadOnlyList<CatchBlock> a, IReadOnlyList<CatchBlock> b)
        {
            if (ReferenceEquals(a, b))
            {
                return true;
            }

            if (a == null
                || b == null
                || a.Count != b.Count)
            {
                return false;
            }

            for (int i = 0, n = a.Count; i < n; i++)
            {
                if (!CompareCatchBlock(a[i], b[i]))
                {
                    return false;
                }
            }

            return true;
        }

        private bool CompareCatchBlock(CatchBlock a, CatchBlock b)
            => Equals(a.Test, b.Test)
                && Compare(a.Body, b.Body)
                && Compare(a.Filter, b.Filter)
                && Compare(a.Variable, b.Variable);
    }
}
