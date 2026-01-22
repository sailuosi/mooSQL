using mooSQL.data.context;
using mooSQL.utils;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.Common;
using System.Globalization;
using System.Linq;
using System.Reflection.Emit;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using mooSQL.data.reader;

namespace mooSQL.data
{
    /// <summary>
    /// 打包器，负责将数据库读取器中的数据反序列化为对象
    /// </summary>
    public partial class PackUp
    {




        internal  Func<DbDataReader,DBInstance, object> GetPacker(Type type, DbDataReader reader, int startBound, int length, bool returnNullIfFirstMissing,DBInstance db)
        {


            // dynamic is passed in as Object ... by c# design
            if (type == typeof(object) || type == typeof(SooRow))
            {
                return GetSooRowPacker(reader, startBound, length, returnNullIfFirstMissing);
            }

            Type underlyingType = null;
            bool useGetFieldValue = false;
            if (typeMap.TryGetValue(type, out var mapEntry))
            {
                useGetFieldValue = (mapEntry.Flags & TypeMapEntryFlags.UseGetFieldValue) != 0;
            }
            else if (!(type.IsEnum || type.IsArray || type.FullName == MapperUntils.LinqBinary
                || (type.IsValueType && (underlyingType = Nullable.GetUnderlyingType(type)) != null && underlyingType.IsEnum)))
            {
                if (typeHandlers.TryGetValue(type, out ITypeParser handler))
                {
                    return GetParserPacker(handler, type, startBound);
                }
                return GetTypePacker(this,type, reader, startBound, length, returnNullIfFirstMissing,db);
            }
            return GetStructPacker(type, underlyingType ?? type, startBound, useGetFieldValue);
        }

        private Func<DbDataReader, DBInstance, object> GetParserPacker(ITypeParser parser, Type type, int startBound)
        {
            return (reader,db) => parser.Parse(type, reader.GetValue(startBound));
        }


        internal Func<DbDataReader, DBInstance, object> GetSooRowPacker(DbDataReader reader, int startBound, int length, bool returnNullIfFirstMissing)
        {
            var fieldCount = reader.FieldCount;
            if (length == -1)
            {
                length = fieldCount - startBound;
            }

            if (fieldCount <= startBound)
            {
                throw MultiMapException(reader);
            }

            var effectiveFieldCount = Math.Min(fieldCount - startBound, length);

            SooTable table = null;

            return
                (r,db) =>
                {
                    if (table is null)
                    {
                        string[] names = new string[effectiveFieldCount];
                        for (int i = 0; i < effectiveFieldCount; i++)
                        {
                            names[i] = r.GetName(i + startBound);
                        }
                        table = new SooTable(names);
                    }

                    var values = new object[effectiveFieldCount];

                    if (returnNullIfFirstMissing)
                    {
                        values[0] = r.GetValue(startBound);
                        if (values[0] is DBNull)
                        {
                            return null;
                        }
                    }

                    if (startBound == 0)
                    {
                        for (int i = 0; i < values.Length; i++)
                        {
                            object val = r.GetValue(i);
                            values[i] = val is DBNull ? null : val;
                        }
                    }
                    else
                    {
                        var begin = returnNullIfFirstMissing ? 1 : 0;
                        for (var iter = begin; iter < effectiveFieldCount; ++iter)
                        {
                            object obj = r.GetValue(iter + startBound);
                            values[iter] = obj is DBNull ? null : obj;
                        }
                    }
                    return new SooRow(table, values);
                };
        }
        private Func<DbDataReader, DBInstance, object> GetStructPacker(Type type, Type effectiveType, int index, bool useGetFieldValue)
        {
            // no point using special per-type handling here; it boils down to the same, plus not all are supported anyway (see: SqlDataReader.GetChar - not supported!)
#pragma warning disable 618
            if (type == typeof(char))
            { // this *does* need special handling, though
                return (r, db) => MapperUntils.ReadChar(r.GetValue(index));
            }
            if (type == typeof(char?))
            {
                return (r,db) => MapperUntils.ReadNullableChar(r.GetValue(index));
            }
            if (type.FullName == MapperUntils.LinqBinary)
            {
                return (r, db) => Activator.CreateInstance(type, r.GetValue(index));
            }
#pragma warning restore 618

            if (effectiveType.IsEnum)
            {   // assume the value is returned as the correct type (int/byte/etc), but box back to the typed enum
                return (r,db) =>
                {
                    var val = r.GetValue(index);
                    if (val is float || val is double || val is decimal)
                    {
                        val = Convert.ChangeType(val, Enum.GetUnderlyingType(effectiveType), CultureInfo.InvariantCulture);
                    }
                    return val is DBNull ? null : Enum.ToObject(effectiveType, val);
                };
            }
            if (typeHandlers.TryGetValue(type, out var handler))
            {
                return (r,db) =>
                {
                    var val = r.GetValue(index);
                    return val is DBNull ? null : handler.Parse(type, val);
                };
            }
            return useGetFieldValue ? ReadViaGetFieldValueFactory(type, index) : ReadViaGetValue(index);


        }



    }
}
