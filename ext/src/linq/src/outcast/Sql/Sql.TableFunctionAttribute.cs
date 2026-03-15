using System;
using System.Linq.Expressions;

// ReSharper disable CheckNamespace

namespace mooSQL.linq
{
	using Common.Internal;
	using Mapping;
    using mooSQL.data;
    using mooSQL.data.Mapping;
    using mooSQL.data.model;

	using SqlProvider;
	using SqlQuery;

	partial class Sql
	{
		[Serializable]
		[AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
		public class TableFunctionAttribute : MappingAttribute
		{
			public TableFunctionAttribute()
			{
			}

			public TableFunctionAttribute(string name)
			{
				Name = name;
			}

			public TableFunctionAttribute(string name, params int[] argIndices)
			{
				Name        = name;
				ArgIndices  = argIndices;
			}

			public TableFunctionAttribute(string configuration, string name)
			{
				Configuration = configuration;
				Name          = name;
			}

			public TableFunctionAttribute(string configuration, string name, params int[] argIndices)
			{
				Configuration = configuration;
				Name          = name;
				ArgIndices    = argIndices;
			}

			public string? Name          { get; set; }
			public string? Schema        { get; set; }
			public string? Database      { get; set; }
			public string? Server        { get; set; }
			public string? Package       { get; set; }
			public int[]?  ArgIndices    { get; set; }


			public override string GetObjectID()
			{
				return $".{Configuration}.{Name}.{Schema}.{Database}.{Server}.{Package}.{IdentifierBuilder.GetObjectID(ArgIndices)}.";
			}
		}
	}
}
