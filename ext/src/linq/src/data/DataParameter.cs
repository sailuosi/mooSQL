using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.Linq;
using System.Xml;
using System.Xml.Linq;

namespace mooSQL.linq.utils
{
	using mooSQL.linq.mapping;
	using mooSQL.data.model;
	using mooSQL.data;
	/// <summary>
	/// DataParameter type.
	/// </summary>


	public class DataParameter
	{
		private DbDataType? _dbDataType;
		/// <summary>
		/// DataParameter type.
		/// </summary>

		public DataParameter()
		{
		}
		/// <summary>
		/// DataParameter type.
		/// </summary>

		public DataParameter(string? name, object? value)
		{
			Name  = name;
			Value = value;
		}
		/// <summary>
		/// DataParameter type.
		/// </summary>

		public DataParameter(string? name, object? value, DbDataType dbDataType)
		{
			Name        = name;
			Value       = value;
			_dbDataType = dbDataType;
		}
		/// <summary>
		/// DataParameter type.
		/// </summary>

		public DataParameter(string? name, object? value, DataFam dataType)
		{
			Name     = name;
			Value    = value;
			DataType = dataType;
		}
		/// <summary>
		/// DataParameter type.
		/// </summary>

		public DataParameter(string? name, object? value, DataFam dataType, string? dbType)
		{
			Name     = name;
			Value    = value;
			DataType = dataType;
			DbType   = dbType;
		}
		/// <summary>
		/// DataParameter type.
		/// </summary>

		public DataParameter(string? name, object? value, string dbType)
		{
			Name     = name;
			Value    = value;
			DbType   = dbType;
		}

		/// <summary>
		/// 获取或设置参数的 <see cref="DataFam"/> 数据类型。
		/// </summary>
		/// <returns>
		/// <see cref="DataFam"/> 枚举值之一；默认为 <see cref="DataFam.Undefined"/>。
		/// </returns>
		public DataFam DataType
		{
			get => _dbDataType?.DataType ?? DataFam.Undefined;
			set => _dbDataType = DbDataType.WithDataType(value);
		}

		/// <summary>
		/// Gets or sets Database Type name of the parameter.
		/// </summary>
		/// <returns>
		/// Name of Database Type or empty string.
		/// </returns>
		public string? DbType
		{
			get => _dbDataType?.DbType;
			set => _dbDataType = DbDataType.WithDbType(value);
		}

		/// <summary>
		/// Gets or sets a value indicating whether the parameter is input-only, output-only, bidirectional, or a stored procedure return value parameter.
		/// </summary>
		/// <returns>
		/// One of the <see cref="ParameterDirection"/> values. The default is Input.
		/// </returns>
		public ParameterDirection? Direction { get; set; }

		/*
				/// <summary>
				/// Gets a value indicating whether the parameter accepts null values.
				/// </summary>
				/// <returns>
				/// true if null values are accepted; otherwise, false. The default is false.
				/// </returns>
				public bool IsNullable { get; set; }
		*/

		/// <summary>
		/// Gets or sets the name of the <see cref="DataParameter"/>.
		/// </summary>
		/// <returns>
		/// The name of the <see cref="DataParameter"/>. The default is an empty string.
		/// </returns>
		public string? Name { get; set; }
		/// <summary>
		/// Gets or sets IsArray.
		/// </summary>

		public bool IsArray { get; set; }

		/// <summary>
		/// Gets or sets precision for parameter type.
		/// </summary>
		public int? Precision
		{
			get => _dbDataType?.Precision;
			set => _dbDataType = DbDataType.WithPrecision(value);
		}

		/// <summary>
		/// Gets or sets scale for parameter type.
		/// </summary>
		public int? Scale
		{
			get => _dbDataType?.Scale;
			set => _dbDataType = DbDataType.WithScale(value);
		}

		/// <summary>
		/// Gets or sets the maximum size, in bytes, of the data within the column.
		/// </summary>
		///
		/// <returns>
		/// The maximum size, in bytes, of the data within the column. The default value is inferred from the parameter value.
		/// </returns>
		public int? Size
		{
			get => _dbDataType?.Length;
			set => _dbDataType = DbDataType.WithLength(value);
		}

		/// <summary>
		/// Gets or sets the value of the parameter.
		/// </summary>
		/// <returns>
		/// An <see cref="object"/> that is the value of the parameter. The default value is null.
		/// </returns>
		public object? Value { get; set; }

		/// <summary>
		/// Provider's parameter instance for out, in-out, return parameters.
		/// Could be used to read parameter value for complex types like Oracle's BFile.
		/// </summary>
		public DbParameter? Output { get; internal set; }

		/// <summary>
		/// Parameter <see cref="DbDataType"/> type.
		/// </summary>
		public DbDataType DbDataType
		{
			get => _dbDataType ??= new DbDataType(Value?.GetType() ?? typeof(object), DataType, DbType, Size, Precision, Scale);
			set => _dbDataType = value;
		}

		internal DbDataType GetOrSetDbDataType(DbDataType? columnType) => _dbDataType ?? columnType ?? DbDataType;
		/// <summary>
		/// Char
		/// </summary>

		public static DataParameter Char          (string? name, char           value) { return new DataParameter { DataType = DataFam.Char,           Name = name, Value = value, }; }
		/// <summary>
		/// Char
		/// </summary>
		/// <summary>
		/// Char
		/// </summary>
		public static DataParameter Char          (string? name, string?        value) { return new DataParameter { DataType = DataFam.Char,           Name = name, Value = value, }; }
		/// <summary>
		/// VarChar
		/// </summary>
		/// <summary>
		/// VarChar
		/// </summary>
		public static DataParameter VarChar       (string? name, char           value) { return new DataParameter { DataType = DataFam.VarChar,        Name = name, Value = value, }; }
		/// <summary>
		/// VarChar
		/// </summary>
		/// <summary>
		/// VarChar
		/// </summary>
		public static DataParameter VarChar       (string? name, string?        value) { return new DataParameter { DataType = DataFam.VarChar,        Name = name, Value = value, }; }
		/// <summary>
		/// Text
		/// </summary>
		/// <summary>
		/// Text
		/// </summary>
		public static DataParameter Text          (string? name, string?        value) { return new DataParameter { DataType = DataFam.Text,           Name = name, Value = value, }; }
		/// <summary>
		/// NChar
		/// </summary>
		/// <summary>
		/// NChar
		/// </summary>
		public static DataParameter NChar         (string? name, char           value) { return new DataParameter { DataType = DataFam.NChar,          Name = name, Value = value, }; }
		/// <summary>
		/// NChar
		/// </summary>
		/// <summary>
		/// NChar
		/// </summary>
		public static DataParameter NChar         (string? name, string?        value) { return new DataParameter { DataType = DataFam.NChar,          Name = name, Value = value, }; }
		/// <summary>
		/// NVarChar
		/// </summary>
		/// <summary>
		/// NVarChar
		/// </summary>
		public static DataParameter NVarChar      (string? name, char           value) { return new DataParameter { DataType = DataFam.NVarChar,       Name = name, Value = value, }; }
		/// <summary>
		/// NVarChar
		/// </summary>
		/// <summary>
		/// NVarChar
		/// </summary>
		public static DataParameter NVarChar      (string? name, string?        value) { return new DataParameter { DataType = DataFam.NVarChar,       Name = name, Value = value, }; }
		/// <summary>
		/// NText
		/// </summary>
		/// <summary>
		/// NText
		/// </summary>
		public static DataParameter NText         (string? name, string?        value) { return new DataParameter { DataType = DataFam.NText,          Name = name, Value = value, }; }
		/// <summary>
		/// Binary
		/// </summary>
		/// <summary>
		/// Binary
		/// </summary>
		public static DataParameter Binary        (string? name, byte[]?        value) { return new DataParameter { DataType = DataFam.Binary,         Name = name, Value = value, }; }
		/// <summary>
		/// Binary
		/// </summary>
		/// <summary>
		/// Binary
		/// </summary>
		public static DataParameter Binary        (string? name, Binary?        value) { return new DataParameter { DataType = DataFam.Binary,         Name = name, Value = value, }; }
		/// <summary>
		/// Blob
		/// </summary>
		/// <summary>
		/// Blob
		/// </summary>
		public static DataParameter Blob          (string? name, byte[]?        value) { return new DataParameter { DataType = DataFam.Blob,           Name = name, Value = value, }; }
		/// <summary>
		/// VarBinary
		/// </summary>
		/// <summary>
		/// VarBinary
		/// </summary>
		public static DataParameter VarBinary     (string? name, byte[]?        value) { return new DataParameter { DataType = DataFam.VarBinary,      Name = name, Value = value, }; }
		/// <summary>
		/// VarBinary
		/// </summary>
		/// <summary>
		/// VarBinary
		/// </summary>
		public static DataParameter VarBinary     (string? name, Binary?        value) { return new DataParameter { DataType = DataFam.VarBinary,      Name = name, Value = value, }; }
		/// <summary>
		/// Image
		/// </summary>
		/// <summary>
		/// Image
		/// </summary>
		public static DataParameter Image         (string? name, byte[]?        value) { return new DataParameter { DataType = DataFam.Image,          Name = name, Value = value, }; }
		/// <summary>
		/// Boolean
		/// </summary>
		/// <summary>
		/// Boolean
		/// </summary>
		public static DataParameter Boolean       (string? name, bool           value) { return new DataParameter { DataType = DataFam.Boolean,        Name = name, Value = value, }; }
		/// <summary>
		/// SByte
		/// </summary>
		[CLSCompliant(false)]
		public static DataParameter SByte         (string? name, sbyte          value) { return new DataParameter { DataType = DataFam.SByte,          Name = name, Value = value, }; }
		/// <summary>
		/// Int16
		/// </summary>
		/// <summary>
		/// Int16
		/// </summary>
		public static DataParameter Int16         (string? name, short          value) { return new DataParameter { DataType = DataFam.Int16,          Name = name, Value = value, }; }
		/// <summary>
		/// Int32
		/// </summary>
		/// <summary>
		/// Int32
		/// </summary>
		public static DataParameter Int32         (string? name, int            value) { return new DataParameter { DataType = DataFam.Int32,          Name = name, Value = value, }; }
		/// <summary>
		/// Int64
		/// </summary>
		/// <summary>
		/// Int64
		/// </summary>
		public static DataParameter Int64         (string? name, long           value) { return new DataParameter { DataType = DataFam.Int64,          Name = name, Value = value, }; }
		/// <summary>
		/// Byte
		/// </summary>
		/// <summary>
		/// Byte
		/// </summary>
		public static DataParameter Byte          (string? name, byte           value) { return new DataParameter { DataType = DataFam.Byte,           Name = name, Value = value, }; }
		/// <summary>
		/// UInt16
		/// </summary>
		[CLSCompliant(false)]
		public static DataParameter UInt16        (string? name, ushort         value) { return new DataParameter { DataType = DataFam.UInt16,         Name = name, Value = value, }; }
		/// <summary>
		/// UInt32
		/// </summary>
		[CLSCompliant(false)]
		public static DataParameter UInt32        (string? name, uint           value) { return new DataParameter { DataType = DataFam.UInt32,         Name = name, Value = value, }; }
		/// <summary>
		/// UInt64
		/// </summary>
		[CLSCompliant(false)]
		public static DataParameter UInt64        (string? name, ulong          value) { return new DataParameter { DataType = DataFam.UInt64,         Name = name, Value = value, }; }
		/// <summary>
		/// Single
		/// </summary>
		/// <summary>
		/// Single
		/// </summary>
		public static DataParameter Single        (string? name, float          value) { return new DataParameter { DataType = DataFam.Single,         Name = name, Value = value, }; }
		/// <summary>
		/// Double
		/// </summary>
		/// <summary>
		/// Double
		/// </summary>
		public static DataParameter Double        (string? name, double         value) { return new DataParameter { DataType = DataFam.Double,         Name = name, Value = value, }; }
		/// <summary>
		/// Decimal
		/// </summary>
		/// <summary>
		/// Decimal
		/// </summary>
		public static DataParameter Decimal       (string? name, decimal        value) { return new DataParameter { DataType = DataFam.Decimal,        Name = name, Value = value, }; }
		/// <summary>
		/// Money
		/// </summary>
		/// <summary>
		/// Money
		/// </summary>
		public static DataParameter Money         (string? name, decimal        value) { return new DataParameter { DataType = DataFam.Money,          Name = name, Value = value, }; }
		/// <summary>
		/// SmallMoney
		/// </summary>
		/// <summary>
		/// SmallMoney
		/// </summary>
		public static DataParameter SmallMoney    (string? name, decimal        value) { return new DataParameter { DataType = DataFam.SmallMoney,     Name = name, Value = value, }; }
		/// <summary>
		/// Guid
		/// </summary>
		/// <summary>
		/// Guid
		/// </summary>
		public static DataParameter Guid          (string? name, Guid           value) { return new DataParameter { DataType = DataFam.Guid,           Name = name, Value = value, }; }
		/// <summary>
		/// Date
		/// </summary>
		/// <summary>
		/// Date
		/// </summary>
		public static DataParameter Date          (string? name, DateTime       value) { return new DataParameter { DataType = DataFam.Date,           Name = name, Value = value, }; }
		/// <summary>
		/// Time
		/// </summary>
		/// <summary>
		/// Time
		/// </summary>
		public static DataParameter Time          (string? name, TimeSpan       value) { return new DataParameter { DataType = DataFam.Time,           Name = name, Value = value, }; }
		/// <summary>
		/// DateTime
		/// </summary>
		/// <summary>
		/// DateTime
		/// </summary>
		public static DataParameter DateTime      (string? name, DateTime       value) { return new DataParameter { DataType = DataFam.DateTime,       Name = name, Value = value, }; }
		/// <summary>
		/// DateTime2
		/// </summary>
		/// <summary>
		/// DateTime2
		/// </summary>
		public static DataParameter DateTime2     (string? name, DateTime       value) { return new DataParameter { DataType = DataFam.DateTime2,      Name = name, Value = value, }; }
		/// <summary>
		/// SmallDateTime
		/// </summary>
		/// <summary>
		/// SmallDateTime
		/// </summary>
		public static DataParameter SmallDateTime (string? name, DateTime       value) { return new DataParameter { DataType = DataFam.SmallDateTime,  Name = name, Value = value, }; }
		/// <summary>
		/// DateTimeOffset
		/// </summary>
		/// <summary>
		/// DateTimeOffset
		/// </summary>
		public static DataParameter DateTimeOffset(string? name, DateTimeOffset value) { return new DataParameter { DataType = DataFam.DateTimeOffset, Name = name, Value = value, }; }
		/// <summary>
		/// Timestamp
		/// </summary>
		/// <summary>
		/// Timestamp
		/// </summary>
		public static DataParameter Timestamp     (string? name, byte[]?        value) { return new DataParameter { DataType = DataFam.Timestamp,      Name = name, Value = value, }; }
		/// <summary>
		/// Xml
		/// </summary>
		/// <summary>
		/// Xml
		/// </summary>
		public static DataParameter Xml           (string? name, string?        value) { return new DataParameter { DataType = DataFam.Xml,            Name = name, Value = value, }; }
		/// <summary>
		/// Xml
		/// </summary>
		/// <summary>
		/// Xml
		/// </summary>
		public static DataParameter Xml           (string? name, XDocument?     value) { return new DataParameter { DataType = DataFam.Xml,            Name = name, Value = value, }; }
		/// <summary>
		/// Xml
		/// </summary>
		/// <summary>
		/// Xml
		/// </summary>
		public static DataParameter Xml           (string? name, XmlDocument?   value) { return new DataParameter { DataType = DataFam.Xml,            Name = name, Value = value, }; }
		/// <summary>
		/// BitArray
		/// </summary>
		/// <summary>
		/// BitArray
		/// </summary>
		public static DataParameter BitArray      (string? name, BitArray?      value) { return new DataParameter { DataType = DataFam.BitArray,       Name = name, Value = value, }; }
		/// <summary>
		/// Variant
		/// </summary>
		/// <summary>
		/// Variant
		/// </summary>
		public static DataParameter Variant       (string? name, object?        value) { return new DataParameter { DataType = DataFam.Variant,        Name = name, Value = value, }; }
		/// <summary>
		/// VarNumeric
		/// </summary>
		/// <summary>
		/// VarNumeric
		/// </summary>
		public static DataParameter VarNumeric    (string? name, decimal        value) { return new DataParameter { DataType = DataFam.VarNumeric,     Name = name, Value = value, }; }
		/// <summary>
		/// Udt
		/// </summary>
		/// <summary>
		/// Udt
		/// </summary>
		public static DataParameter Udt           (string? name, object?        value) { return new DataParameter { DataType = DataFam.Udt,            Name = name, Value = value, }; }
		/// <summary>
		/// Dictionary
		/// </summary>
		/// <summary>
		/// Dictionary
		/// </summary>
		public static DataParameter Dictionary    (string? name, IDictionary?   value) { return new DataParameter { DataType = DataFam.Dictionary,     Name = name, Value = value, }; }
		/// <summary>
		/// Creates Create.
		/// </summary>

		public static DataParameter Create        (string? name, char           value) { return new DataParameter { DataType = DataFam.NChar,          Name = name, Value = value, }; }
		/// <summary>
		/// Creates Create.
		/// </summary>
		/// <summary>
		/// Creates Create.
		/// </summary>
		public static DataParameter Create        (string? name, string?        value) { return new DataParameter { DataType = DataFam.NVarChar,       Name = name, Value = value, }; }
		/// <summary>
		/// Creates Create.
		/// </summary>
		/// <summary>
		/// Creates Create.
		/// </summary>
		public static DataParameter Create        (string? name, byte[]?        value) { return new DataParameter { DataType = DataFam.VarBinary,      Name = name, Value = value, }; }
		/// <summary>
		/// Creates Create.
		/// </summary>
		/// <summary>
		/// Creates Create.
		/// </summary>
		public static DataParameter Create        (string? name, Binary?        value) { return new DataParameter { DataType = DataFam.VarBinary,      Name = name, Value = value, }; }
		/// <summary>
		/// Creates Create.
		/// </summary>
		/// <summary>
		/// Creates Create.
		/// </summary>
		public static DataParameter Create        (string? name, bool           value) { return new DataParameter { DataType = DataFam.Boolean,        Name = name, Value = value, }; }
		/// <summary>
		/// Creates Create.
		/// </summary>
		[CLSCompliant(false)]
		public static DataParameter Create        (string? name, sbyte          value) { return new DataParameter { DataType = DataFam.SByte,          Name = name, Value = value, }; }
		/// <summary>
		/// Creates Create.
		/// </summary>
		/// <summary>
		/// Creates Create.
		/// </summary>
		public static DataParameter Create        (string? name, short          value) { return new DataParameter { DataType = DataFam.Int16,          Name = name, Value = value, }; }
		/// <summary>
		/// Creates Create.
		/// </summary>
		/// <summary>
		/// Creates Create.
		/// </summary>
		public static DataParameter Create        (string? name, int            value) { return new DataParameter { DataType = DataFam.Int32,          Name = name, Value = value, }; }
		/// <summary>
		/// Creates Create.
		/// </summary>
		/// <summary>
		/// Creates Create.
		/// </summary>
		public static DataParameter Create        (string? name, long           value) { return new DataParameter { DataType = DataFam.Int64,          Name = name, Value = value, }; }
		/// <summary>
		/// Creates Create.
		/// </summary>
		/// <summary>
		/// Creates Create.
		/// </summary>
		public static DataParameter Create        (string? name, byte           value) { return new DataParameter { DataType = DataFam.Byte,           Name = name, Value = value, }; }
		/// <summary>
		/// Creates Create.
		/// </summary>
		[CLSCompliant(false)]
		public static DataParameter Create        (string? name, ushort         value) { return new DataParameter { DataType = DataFam.UInt16,         Name = name, Value = value, }; }
		/// <summary>
		/// Creates Create.
		/// </summary>
		[CLSCompliant(false)]
		public static DataParameter Create        (string? name, uint           value) { return new DataParameter { DataType = DataFam.UInt32,         Name = name, Value = value, }; }
		/// <summary>
		/// Creates Create.
		/// </summary>
		[CLSCompliant(false)]
		public static DataParameter Create        (string? name, ulong          value) { return new DataParameter { DataType = DataFam.UInt64,         Name = name, Value = value, }; }
		/// <summary>
		/// Creates Create.
		/// </summary>
		/// <summary>
		/// Creates Create.
		/// </summary>
		public static DataParameter Create        (string? name, float          value) { return new DataParameter { DataType = DataFam.Single,         Name = name, Value = value, }; }
		/// <summary>
		/// Creates Create.
		/// </summary>
		/// <summary>
		/// Creates Create.
		/// </summary>
		public static DataParameter Create        (string? name, double         value) { return new DataParameter { DataType = DataFam.Double,         Name = name, Value = value, }; }
		/// <summary>
		/// Creates Create.
		/// </summary>
		/// <summary>
		/// Creates Create.
		/// </summary>
		public static DataParameter Create        (string? name, decimal        value) { return new DataParameter { DataType = DataFam.Decimal,        Name = name, Value = value, }; }
		/// <summary>
		/// Creates Create.
		/// </summary>
		/// <summary>
		/// Creates Create.
		/// </summary>
		public static DataParameter Create        (string? name, Guid           value) { return new DataParameter { DataType = DataFam.Guid,           Name = name, Value = value, }; }
		/// <summary>
		/// Creates Create.
		/// </summary>
		/// <summary>
		/// Creates Create.
		/// </summary>
		public static DataParameter Create        (string? name, TimeSpan       value) { return new DataParameter { DataType = DataFam.Time,           Name = name, Value = value, }; }
		/// <summary>
		/// Creates Create.
		/// </summary>
		/// <summary>
		/// Creates Create.
		/// </summary>
		public static DataParameter Create        (string? name, DateTime       value) { return new DataParameter { DataType = DataFam.DateTime2,      Name = name, Value = value, }; }
		/// <summary>
		/// Creates Create.
		/// </summary>
		/// <summary>
		/// Creates Create.
		/// </summary>
		public static DataParameter Create        (string? name, DateTimeOffset value) { return new DataParameter { DataType = DataFam.DateTimeOffset, Name = name, Value = value, }; }
		/// <summary>
		/// Creates Create.
		/// </summary>
		/// <summary>
		/// Creates Create.
		/// </summary>
		public static DataParameter Create        (string? name, XDocument?     value) { return new DataParameter { DataType = DataFam.Xml,            Name = name, Value = value, }; }
		/// <summary>
		/// Creates Create.
		/// </summary>
		/// <summary>
		/// Creates Create.
		/// </summary>
		public static DataParameter Create        (string? name, XmlDocument?   value) { return new DataParameter { DataType = DataFam.Xml,            Name = name, Value = value, }; }
		/// <summary>
		/// Creates Create.
		/// </summary>
		/// <summary>
		/// Creates Create.
		/// </summary>
		public static DataParameter Create        (string? name, BitArray?      value) { return new DataParameter { DataType = DataFam.BitArray,       Name = name, Value = value, }; }
		/// <summary>
		/// Creates Create.
		/// </summary>
		/// <summary>
		/// Creates Create.
		/// </summary>
		public static DataParameter Create        (string? name, Dictionary<string,string>? value) { return new DataParameter { DataType = DataFam.Dictionary,     Name = name, Value = value, }; }
		/// <summary>
		/// Json
		/// </summary>
		/// <summary>
		/// Json
		/// </summary>
		public static DataParameter Json          (string? name, string?        value) { return new DataParameter { DataType = DataFam.Json,           Name = name, Value = value,}; }
		/// <summary>
		/// BinaryJson
		/// </summary>
		/// <summary>
		/// BinaryJson
		/// </summary>
		public static DataParameter BinaryJson    (string? name, string?        value) { return new DataParameter { DataType = DataFam.BinaryJson,     Name = name, Value = value, }; }
		/// <summary>
		/// toPara
		/// </summary>

		public Parameter toPara() { 
			var res= new Parameter(Name, Value);
			res.dbType = this.DbDataType;
			return res;
		}
	}
}
