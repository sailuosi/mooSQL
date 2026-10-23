using System;
using System.Linq.Expressions;

// ReSharper disable CheckNamespace

namespace mooSQL.linq
{
	using mooSQL.linq.utils;
	using mooSQL.linq.mapping;
    using mooSQL.data;
    using mooSQL.data.Mapping;
    using mooSQL.data.model;

	using mooSQL.linq.provider;
	using mooSQL.linq.clause;
	partial class DbFunc
	{
		/// <summary>
		/// TableFunctionAttribute
		/// </summary>
		[Serializable]
		[AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
		public class TableFunctionAttribute : MappingAttribute
		{
			/// <summary>
			/// Initializes a new instance of the <see cref="DbFunc.TableFunctionAttribute"/> class.
			/// </summary>
			/// <summary>
			/// Initializes a new instance of the <see cref="DbFunc.TableFunctionAttribute"/> class.
			/// </summary>
			public TableFunctionAttribute()
			{
			}
			/// <summary>
			/// Initializes a new instance of the <see cref="DbFunc.TableFunctionAttribute"/> class.
			/// </summary>

			public TableFunctionAttribute(string name)
			{
				Name = name;
			}
			/// <summary>
			/// Initializes a new instance of the <see cref="DbFunc.TableFunctionAttribute"/> class.
			/// </summary>

			public TableFunctionAttribute(string name, params int[] argIndices)
			{
				Name        = name;
				ArgIndices  = argIndices;
			}
			/// <summary>
			/// Initializes a new instance of the <see cref="DbFunc.TableFunctionAttribute"/> class.
			/// </summary>

			public TableFunctionAttribute(string configuration, string name)
			{
				Configuration = configuration;
				Name          = name;
			}
			/// <summary>
			/// Initializes a new instance of the <see cref="DbFunc.TableFunctionAttribute"/> class.
			/// </summary>

			public TableFunctionAttribute(string configuration, string name, params int[] argIndices)
			{
				Configuration = configuration;
				Name          = name;
				ArgIndices    = argIndices;
			}
			/// <summary>
			/// Gets or sets Name.
			/// </summary>

			public string? Name          { get; set; }
			/// <summary>
			/// Schema
			/// </summary>
			/// <summary>
			/// Gets or sets Schema.
			/// </summary>
			public string? Schema        { get; set; }
			/// <summary>
			/// Database
			/// </summary>
			/// <summary>
			/// Gets or sets Database.
			/// </summary>
			public string? Database      { get; set; }
			/// <summary>
			/// Server
			/// </summary>
			/// <summary>
			/// Gets or sets Server.
			/// </summary>
			public string? Server        { get; set; }
			/// <summary>
			/// Package
			/// </summary>
			/// <summary>
			/// Gets or sets Package.
			/// </summary>
			public string? Package       { get; set; }
			/// <summary>
			/// ArgIndices
			/// </summary>
			/// <summary>
			/// Gets or sets ArgIndices.
			/// </summary>
			public int[]?  ArgIndices    { get; set; }
/// <summary>
/// Gets GetObjectID.
/// </summary>
	


			public override string GetObjectID()
			{
				return $".{Configuration}.{Name}.{Schema}.{Database}.{Server}.{Package}.{IdentifierBuilder.GetObjectID(ArgIndices)}.";
			}
		}
	}
}
