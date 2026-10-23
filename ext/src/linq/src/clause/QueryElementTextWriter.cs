using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using mooSQL.data.model;
using mooSQL.linq.utils;

namespace mooSQL.linq.clause
{
	/// <summary>
	/// QueryElementTextWriter type.
	/// </summary>
	/// <summary>
	/// QueryElementTextWriter type.
	/// </summary>
	public class QueryElementTextWriter
	{
		HashSet<ISQLNode>? _visited;
		readonly SqlTextWriter  _writer = new();
		/// <summary>
		/// Gets or sets Nullability.
		/// </summary>

		public NullabilityContext Nullability { get; }

#if DEBUG
		// character added to visually detect end of string
		/// <summary>
		/// DebugString
		/// </summary>
		/// <summary>
		/// DebugString
		/// </summary>
		public string DebugString => _writer + "¶";
#endif
		/// <summary>
		/// QueryElementTextWriter type.
		/// </summary>

		public QueryElementTextWriter() : this(NullabilityContext.NonQuery)
		{
		}
		/// <summary>
		/// QueryElementTextWriter type.
		/// </summary>

		public QueryElementTextWriter(NullabilityContext nullability)
		{
			Nullability = nullability;
		}
		/// <summary>
		/// Length
		/// </summary>

		public int Length
		{
			get => _writer.Length;
			set => _writer.Length = value;
		}
		/// <summary>
		/// AddVisited
		/// </summary>

		public bool AddVisited(ISQLNode element)
		{
			_visited ??= new();
			return _visited.Add(element);
		}
		/// <summary>
		/// RemoveVisited
		/// </summary>

		public void RemoveVisited(ISQLNode element)
		{
			_visited?.Remove(element);
		}
		/// <summary>
		/// AppendIdentCheck
		/// </summary>

		public QueryElementTextWriter AppendIdentCheck(string str)
		{
			_writer.AppendIdentCheck(str);
			return this;
		}
		/// <summary>
		/// ToString
		/// </summary>

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public string ToString(int startIndex, int length)
		{
			return _writer.ToString(startIndex, length);
		}
		/// <summary>
		/// ToString
		/// </summary>
	

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public override string ToString()
		{
			return _writer.ToString();
		}
		/// <summary>
		/// IndentScope
		/// </summary>

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public SqlTextWriter.IndentScope IndentScope()
		{
			return _writer.Indent();
		}
		/// <summary>
		/// Indent
		/// </summary>

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public int Indent()
		{
			return _writer.IncrementIndent();
		}
		/// <summary>
		/// UnIndent
		/// </summary>

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public int UnIndent()
		{
			return _writer.DecrementIndent();
		}
		/// <summary>
		/// Append
		/// </summary>

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public QueryElementTextWriter Append(object? value)
		{
			_writer.Append(value);
			return this;
		}
		/// <summary>
		/// Append
		/// </summary>

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public QueryElementTextWriter Append(string? value)
		{
			_writer.Append(value);
			return this;
		}
		/// <summary>
		/// Append
		/// </summary>

		[CLSCompliant(false)]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public QueryElementTextWriter Append(sbyte value)
		{
			_writer.Append(value);
			return this;
		}
		/// <summary>
		/// Append
		/// </summary>

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public QueryElementTextWriter Append(byte value)
		{
			_writer.Append(value);
			return this;
		}
		/// <summary>
		/// Append
		/// </summary>

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public QueryElementTextWriter Append(short value)
		{
			_writer.Append(value);
			return this;
		}
		/// <summary>
		/// Append
		/// </summary>

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public QueryElementTextWriter Append(int value)
		{
			_writer.Append(value);
			return this;
		}
		/// <summary>
		/// Append
		/// </summary>

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public QueryElementTextWriter Append(long value)
		{
			_writer.Append(value);
			return this;
		}
		/// <summary>
		/// Append
		/// </summary>

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public QueryElementTextWriter Append(float value)
		{
			_writer.Append(value);
			return this;
		}
		/// <summary>
		/// Append
		/// </summary>

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public QueryElementTextWriter Append(double value)
		{
			_writer.Append(value);
			return this;
		}
		/// <summary>
		/// Append
		/// </summary>

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public QueryElementTextWriter Append(decimal value)
		{
			_writer.Append(value);
			return this;
		}
		/// <summary>
		/// Append
		/// </summary>

		[CLSCompliant(false)]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public QueryElementTextWriter Append(char value)
		{
			_writer.Append(value);
			return this;
		}
		/// <summary>
		/// Append
		/// </summary>

		[CLSCompliant(false)]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public QueryElementTextWriter Append(ushort value)
		{
			_writer.Append(value);
			return this;
		}
		/// <summary>
		/// Append
		/// </summary>

		[CLSCompliant(false)]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public QueryElementTextWriter Append(uint value)
		{
			_writer.Append(value);
			return this;
		}
		/// <summary>
		/// Append
		/// </summary>

		[CLSCompliant(false)]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public QueryElementTextWriter Append(ulong value)
		{
			_writer.Append(value);
			return this;
		}
		/// <summary>
		/// Append
		/// </summary>

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public QueryElementTextWriter Append(StringBuilder? value)
		{
			if (value != null)
				Append(value.ToString());
			return this;
		}
		/// <summary>
		/// Append
		/// </summary>

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public QueryElementTextWriter Append(char value, int repeatCount)
		{
			_writer.Append(value, repeatCount);
			return this;
		}
		/// <summary>
		/// AppendLine
		/// </summary>

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public QueryElementTextWriter AppendLine()
		{
			_writer.AppendLine();
			return this;
		}
		/// <summary>
		/// AppendLine
		/// </summary>

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public QueryElementTextWriter AppendLine(string str)
		{
			Append(str);
			return AppendLine();
		}
		/// <summary>
		/// AppendLine
		/// </summary>

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public QueryElementTextWriter AppendLine(char value)
		{
			Append(value);
			return AppendLine();
		}
		/// <summary>
		/// AppendFormat
		/// </summary>

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public QueryElementTextWriter AppendFormat(string format, object arg0)
		{
			_writer.AppendFormat(format, arg0);
			return this;
		}
		/// <summary>
		/// AppendFormat
		/// </summary>

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public QueryElementTextWriter AppendFormat(string format, params object[] args)
		{
			_writer.AppendFormat(format, args);
			return this;
		}
		/// <summary>
		/// Replace
		/// </summary>

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public QueryElementTextWriter Replace(string oldValue, string newValue, int startIndex, int count)
		{
			_writer.Replace(oldValue, newValue, startIndex, count);
			return this;
		}
	}
}
