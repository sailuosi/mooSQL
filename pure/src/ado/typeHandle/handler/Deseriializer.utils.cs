using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data
{
    /// <summary>
    /// 存放成员属性部分
    /// </summary>
    public partial class Deserializer
    {
        private Func<DbDataReader,DBInstance, object> ReadViaGetValue(int index)
        => (reader,db) =>
        {
            var val = reader.GetValue(index);
            return val is DBNull ? null : val;
        };
        private void LoadDefaultValue(ILGenerator il, Type type)
        {
            if (type.IsValueType)
            {
                var local = il.DeclareLocal(type);
                il.Emit(OpCodes.Ldloca, local);
                il.Emit(OpCodes.Initobj, type);
                il.Emit(OpCodes.Ldloc, local);
            }
            else
            {
                il.Emit(OpCodes.Ldnull);
            }
        }

        private int GetNextSplit(int startIdx, string splitOn, DbDataReader reader)
        {
            if (splitOn == "*")
            {
                return --startIdx;
            }

            for (var i = startIdx - 1; i > 0; --i)
            {
                if (string.Equals(splitOn, reader.GetName(i), StringComparison.OrdinalIgnoreCase))
                {
                    return i;
                }
            }

            throw MultiMapException(reader, splitOn);
        }

        private int GetNextSplitDynamic(int startIdx, string splitOn, DbDataReader reader)
        {
            if (startIdx == reader.FieldCount)
            {
                throw MultiMapException(reader, splitOn);
            }

            if (splitOn == "*")
            {
                return ++startIdx;
            }

            for (var i = startIdx + 1; i < reader.FieldCount; ++i)
            {
                if (string.Equals(splitOn, reader.GetName(i), StringComparison.OrdinalIgnoreCase))
                {
                    return i;
                }
            }

            return reader.FieldCount;
        }


        private MethodInfo GetOperator(Type from, Type to)
        {
            if (to is null) return null;
            MethodInfo[] fromMethods, toMethods;
            return ResolveOperator(fromMethods = from.GetMethods(BindingFlags.Static | BindingFlags.Public), from, to, "op_Implicit")
                ?? ResolveOperator(toMethods = to.GetMethods(BindingFlags.Static | BindingFlags.Public), from, to, "op_Implicit")
                ?? ResolveOperator(fromMethods, from, to, "op_Explicit")
                ?? ResolveOperator(toMethods, from, to, "op_Explicit");
        }

        private MethodInfo ResolveOperator(MethodInfo[] methods, Type from, Type to, string name)
        {
            for (int i = 0; i < methods.Length; i++)
            {
                if (methods[i].Name != name || methods[i].ReturnType != to) continue;
                var args = methods[i].GetParameters();
                if (args.Length != 1 || args[0].ParameterType != from) continue;
                return methods[i];
            }
            return null;
        }


        internal void EmitInt32(ILGenerator il, int value)
        {
            switch (value)
            {
                case -1: il.Emit(OpCodes.Ldc_I4_M1); break;
                case 0: il.Emit(OpCodes.Ldc_I4_0); break;
                case 1: il.Emit(OpCodes.Ldc_I4_1); break;
                case 2: il.Emit(OpCodes.Ldc_I4_2); break;
                case 3: il.Emit(OpCodes.Ldc_I4_3); break;
                case 4: il.Emit(OpCodes.Ldc_I4_4); break;
                case 5: il.Emit(OpCodes.Ldc_I4_5); break;
                case 6: il.Emit(OpCodes.Ldc_I4_6); break;
                case 7: il.Emit(OpCodes.Ldc_I4_7); break;
                case 8: il.Emit(OpCodes.Ldc_I4_8); break;
                default:
                    if (value >= -128 && value <= 127)
                    {
                        il.Emit(OpCodes.Ldc_I4_S, (sbyte)value);
                    }
                    else
                    {
                        il.Emit(OpCodes.Ldc_I4, value);
                    }
                    break;
            }
        }


        private Exception MultiMapException(IDataRecord reader, string splitOn = null)
        {
            bool hasFields = false;
            try { hasFields = reader != null && reader.FieldCount != 0; }
            catch { /* don't throw when trying to throw */ }
            if (hasFields)
            {
                return new ArgumentException(
                    string.IsNullOrEmpty(splitOn)
                    ? "When using the multi-mapping APIs ensure you set the splitOn param if you have keys other than Id"
                    : $"Multi-map error: splitOn column '{splitOn}' was not found - please ensure your splitOn parameter is set and in the correct order",
                    nameof(splitOn));
            }
            else
            {
                return new InvalidOperationException("No columns were selected");
            }
        }



        /// <summary>
        /// Internal use only
        /// </summary>
        /// <param name="type"></param>
        /// <param name="reader"></param>
        /// <param name="startBound"></param>
        /// <param name="length"></param>
        /// <param name="returnNullIfFirstMissing"></param>
        public Func<DbDataReader, DBInstance, object> GetTypeDeserializer(
            Deserializer deserializer,Type type, DbDataReader reader, int startBound = 0, int length = -1, bool returnNullIfFirstMissing = false,DBInstance db=null
        )
        {
            return TypeDeserializerCache.GetReader(deserializer,type, reader, startBound, length, returnNullIfFirstMissing, db);
        }

        private LocalBuilder GetTempLocal(ILGenerator il, ref Dictionary<Type, LocalBuilder> locals, Type type, bool initAndLoad)
        {
            if (type is null) throw new ArgumentNullException(nameof(type));
            if (locals == null) locals = new Dictionary<Type, LocalBuilder>();
            if (!locals.TryGetValue(type, out LocalBuilder found))
            {
                found = il.DeclareLocal(type);
                locals.Add(type, found);
            }
            if (initAndLoad)
            {
                il.Emit(OpCodes.Ldloca, found);
                il.Emit(OpCodes.Initobj, type);
                il.Emit(OpCodes.Ldloca, found);
                il.Emit(OpCodes.Ldobj, type);
            }
            return found;
        }

    }
}
