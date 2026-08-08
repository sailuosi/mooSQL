using mooSQL.linq;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace mooSQL.linq.Linq
{
	/// <summary>
	/// Structural expression comparer for query plan cache keys.
	/// Constant nodes compare by type only (not runtime value).
	/// </summary>
	sealed class ExtExpressionStructuralComparer : IEqualityComparer<Expression?>
	{
		public static ExtExpressionStructuralComparer Instance { get; } = new();

		ExtExpressionStructuralComparer()
		{
		}

		public int GetHashCode(Expression? obj)
		{
			if (obj is null)
				return 0;

			return new ClipExpSameCheckor().GetHashCode(obj);
		}

		public bool Equals(Expression? x, Expression? y)
		{
			if (ReferenceEquals(x, y))
				return true;

			if (x is null || y is null)
				return false;

			return new Comparer().Compare(x, y);
		}

		sealed class Comparer
		{
			ConcurrentDictionary<ParameterExpression, ParameterExpression>? _parameterScope;

			public bool Compare(Expression? left, Expression? right)
			{
				if (ReferenceEquals(left, right))
					return true;

				if (left is null || right is null)
					return false;

				if (left.NodeType != right.NodeType || left.Type != right.Type)
					return false;

				return left switch
				{
					BinaryExpression leftBinary => CompareBinary(leftBinary, (BinaryExpression)right),
					ConditionalExpression leftConditional => CompareConditional(leftConditional, (ConditionalExpression)right),
					ConstantExpression leftConstant => CompareConstant(leftConstant, (ConstantExpression)right),
					DefaultExpression => true,
					InvocationExpression leftInvocation => CompareInvocation(leftInvocation, (InvocationExpression)right),
					LambdaExpression leftLambda => CompareLambda(leftLambda, (LambdaExpression)right),
					ListInitExpression leftListInit => CompareListInit(leftListInit, (ListInitExpression)right),
					MemberExpression leftMember => CompareMember(leftMember, (MemberExpression)right),
					MemberInitExpression leftMemberInit => CompareMemberInit(leftMemberInit, (MemberInitExpression)right),
					MethodCallExpression leftMethodCall => CompareMethodCall(leftMethodCall, (MethodCallExpression)right),
					NewArrayExpression leftNewArray => CompareNewArray(leftNewArray, (NewArrayExpression)right),
					NewExpression leftNew => CompareNew(leftNew, (NewExpression)right),
					ParameterExpression leftParameter => CompareParameter(leftParameter, (ParameterExpression)right),
					TypeBinaryExpression leftTypeBinary => CompareTypeBinary(leftTypeBinary, (TypeBinaryExpression)right),
					UnaryExpression leftUnary => CompareUnary(leftUnary, (UnaryExpression)right),

					_ => left.NodeType == ExpressionType.Extension
						? ReferenceEquals(left, right)
							|| (left.GetType() == right.GetType() && left.NodeType == right.NodeType)
						: false
				};
			}

			static bool CompareConstant(ConstantExpression a, ConstantExpression b)
			{
				if (a.Type != b.Type)
					return false;

				var v1 = a.Value;
				var v2 = b.Value;

				if (v1 is null && v2 is null)
					return true;

				if (v1 is IQueryable && v2 is IQueryable)
					return true;

				if (v1 is null || v2 is null)
					return true;

				if (v1 is IStructuralEquatable structuralEquatable
					&& v2 is IStructuralEquatable structuralEquatable2
					&& v1.GetType() == v2.GetType())
				{
					return structuralEquatable.Equals(v2, StructuralComparisons.StructuralEqualityComparer);
				}

				return true;
			}

			bool CompareBinary(BinaryExpression a, BinaryExpression b)
				=> Equals(a.Method, b.Method)
					&& a.IsLifted == b.IsLifted
					&& a.IsLiftedToNull == b.IsLiftedToNull
					&& Compare(a.Left, b.Left)
					&& Compare(a.Right, b.Right)
					&& Compare(a.Conversion, b.Conversion);

			bool CompareConditional(ConditionalExpression a, ConditionalExpression b)
				=> Compare(a.Test, b.Test)
					&& Compare(a.IfTrue, b.IfTrue)
					&& Compare(a.IfFalse, b.IfFalse);

			bool CompareInvocation(InvocationExpression a, InvocationExpression b)
				=> Compare(a.Expression, b.Expression)
					&& CompareExpressionList(a.Arguments, b.Arguments);

			bool CompareLambda(LambdaExpression a, LambdaExpression b)
			{
				var n = a.Parameters.Count;

				if (b.Parameters.Count != n)
					return false;

				_parameterScope ??= new ConcurrentDictionary<ParameterExpression, ParameterExpression>();

				for (var i = 0; i < n; i++)
				{
					var p1 = a.Parameters[i];
					var p2 = b.Parameters[i];

					if (p1.Type != p2.Type || p1.Name != p2.Name)
					{
						ClearParameterScope(a, i);
						return false;
					}

					_parameterScope.TryAdd(p1, p2);
				}

				try
				{
					return a.ReturnType == b.ReturnType && Compare(a.Body, b.Body);
				}
				finally
				{
					ClearParameterScope(a, n);
				}
			}

			void ClearParameterScope(LambdaExpression lambda, int count)
			{
				for (var i = 0; i < count; i++)
					_parameterScope!.TryRemove(lambda.Parameters[i], out _);
			}

			bool CompareListInit(ListInitExpression a, ListInitExpression b)
				=> Compare(a.NewExpression, b.NewExpression)
					&& CompareElementInitList(a.Initializers, b.Initializers);

			bool CompareMember(MemberExpression a, MemberExpression b)
				=> Equals(a.Member, b.Member)
					&& Compare(a.Expression, b.Expression);

			bool CompareMemberInit(MemberInitExpression a, MemberInitExpression b)
				=> Compare(a.NewExpression, b.NewExpression)
					&& CompareMemberBindingList(a.Bindings, b.Bindings);

			bool CompareMethodCall(MethodCallExpression a, MethodCallExpression b)
				=> Equals(a.Method, b.Method)
					&& Compare(a.Object, b.Object)
					&& CompareExpressionList(a.Arguments, b.Arguments);

			bool CompareNewArray(NewArrayExpression a, NewArrayExpression b)
				=> CompareExpressionList(a.Expressions, b.Expressions);

			bool CompareNew(NewExpression a, NewExpression b)
				=> Equals(a.Constructor, b.Constructor)
					&& CompareExpressionList(a.Arguments, b.Arguments)
					&& CompareMemberList(a.Members, b.Members);

			bool CompareParameter(ParameterExpression a, ParameterExpression b)
			{
				if (a.Type != b.Type)
					return false;

				if (_parameterScope != null && _parameterScope.TryGetValue(a, out var mapped))
					return mapped.Name == b.Name && mapped.Type == b.Type;

				return a.Name == b.Name;
			}

			bool CompareTypeBinary(TypeBinaryExpression a, TypeBinaryExpression b)
				=> a.TypeOperand == b.TypeOperand
					&& Compare(a.Expression, b.Expression);

			bool CompareUnary(UnaryExpression a, UnaryExpression b)
				=> Equals(a.Method, b.Method)
					&& a.IsLifted == b.IsLifted
					&& a.IsLiftedToNull == b.IsLiftedToNull
					&& Compare(a.Operand, b.Operand);

			bool CompareExpressionList(IReadOnlyList<Expression> a, IReadOnlyList<Expression> b)
			{
				if (ReferenceEquals(a, b))
					return true;

				if (a.Count != b.Count)
					return false;

				for (var i = 0; i < a.Count; i++)
				{
					if (!Compare(a[i], b[i]))
						return false;
				}

				return true;
			}

			static bool CompareMemberList(IReadOnlyList<MemberInfo>? a, IReadOnlyList<MemberInfo>? b)
			{
				if (ReferenceEquals(a, b))
					return true;

				if (a is null || b is null || a.Count != b.Count)
					return false;

				for (var i = 0; i < a.Count; i++)
				{
					if (!Equals(a[i], b[i]))
						return false;
				}

				return true;
			}

			bool CompareMemberBindingList(IReadOnlyList<MemberBinding> a, IReadOnlyList<MemberBinding> b)
			{
				if (ReferenceEquals(a, b))
					return true;

				if (a.Count != b.Count)
					return false;

				for (var i = 0; i < a.Count; i++)
				{
					if (!CompareBinding(a[i], b[i]))
						return false;
				}

				return true;
			}

			bool CompareBinding(MemberBinding a, MemberBinding b)
			{
				if (a.BindingType != b.BindingType || !Equals(a.Member, b.Member))
					return false;

				return a switch
				{
					MemberAssignment memberAssignment => Compare(memberAssignment.Expression, ((MemberAssignment)b).Expression),
					MemberListBinding memberListBinding => CompareElementInitList(memberListBinding.Initializers, ((MemberListBinding)b).Initializers),
					MemberMemberBinding memberMemberBinding => CompareMemberBindingList(memberMemberBinding.Bindings, ((MemberMemberBinding)b).Bindings),
					_ => false
				};
			}

			bool CompareElementInitList(IReadOnlyList<ElementInit> a, IReadOnlyList<ElementInit> b)
			{
				if (ReferenceEquals(a, b))
					return true;

				if (a.Count != b.Count)
					return false;

				for (var i = 0; i < a.Count; i++)
				{
					if (!CompareElementInit(a[i], b[i]))
						return false;
				}

				return true;
			}

			bool CompareElementInit(ElementInit a, ElementInit b)
				=> Equals(a.AddMethod, b.AddMethod)
					&& CompareExpressionList(a.Arguments, b.Arguments);
		}
	}
}
