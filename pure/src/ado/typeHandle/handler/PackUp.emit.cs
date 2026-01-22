using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.Common;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using mooSQL.data.reader;

namespace mooSQL.data
{
    public partial class PackUp
    {
        /// <summary>
        /// 动态创建代码的起点。
        /// </summary>
        /// <param name="type"></param>
        /// <param name="reader"></param>
        /// <param name="startBound"></param>
        /// <param name="length"></param>
        /// <param name="returnNullIfFirstMissing"></param>
        /// <param name="db"></param>
        /// <returns></returns>
        internal Func<DbDataReader, DBInstance, object> GetTypePackImpl(
    Type type, DbDataReader reader, int startBound = 0, int length = -1, bool returnNullIfFirstMissing = false, DBInstance db = null
)
        {
            if (length == -1)
            {
                length = reader.FieldCount - startBound;
            }

            if (reader.FieldCount <= startBound)
            {
                throw MultiMapException(reader);
            }

            var returnType = type.IsValueType ? typeof(object) : type;
            var dm = new DynamicMethod("Deserialize" + Guid.NewGuid().ToString(), returnType, new[] { typeof(DbDataReader), typeof(DBInstance) }, type, true);
            var il = dm.GetILGenerator();

            if (MapperUntils.IsValueTuple(type))
            {
                GenerateValueTupleDeserializer(type, reader, startBound, length, il);
            }
            else
            {
                GenePackFromMap(type, reader, startBound, length, returnNullIfFirstMissing, il);
            }

            var funcType = System.Linq.Expressions.Expression.GetFuncType(typeof(DbDataReader), typeof(DBInstance), returnType);
            return (Func<DbDataReader, DBInstance, object>)dm.CreateDelegate(funcType);
        }

        private void GenerateValueTupleDeserializer(Type valueTupleType, DbDataReader reader, int startBound, int length, ILGenerator il)
        {

            LocalBuilder localDb = il.DeclareLocal(typeof(DBInstance));
            il.Emit(OpCodes.Ldarg_1); // 
            il.Emit(OpCodes.Stloc, localDb); // 

            var nullableUnderlyingType = Nullable.GetUnderlyingType(valueTupleType);
            var currentValueTupleType = nullableUnderlyingType ?? valueTupleType;

            var constructors = new List<ConstructorInfo>();
            var languageTupleElementTypes = new List<Type>();

            while (true)
            {
                var arity = int.Parse(currentValueTupleType.Name.Substring("ValueTuple`".Length), CultureInfo.InvariantCulture);
                var constructorParameterTypes = new Type[arity];
                var restField = (FieldInfo)null;

                foreach (var field in currentValueTupleType.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
                {
                    if (field.Name == "Rest")
                    {
                        restField = field;
                    }
                    else if (field.Name.StartsWith("Item", StringComparison.Ordinal))
                    {
                        var elementNumber = int.Parse(field.Name.Substring("Item".Length), CultureInfo.InvariantCulture);
                        constructorParameterTypes[elementNumber - 1] = field.FieldType;
                    }
                }

                var itemFieldCount = constructorParameterTypes.Length;
                if (restField != null) itemFieldCount--;

                for (var i = 0; i < itemFieldCount; i++)
                {
                    languageTupleElementTypes.Add(constructorParameterTypes[i]);
                }

                if (restField != null)
                {
                    constructorParameterTypes[constructorParameterTypes.Length - 1] = restField.FieldType;
                }

                constructors.Add(currentValueTupleType.GetConstructor(constructorParameterTypes));

                if (restField is null) break;

                currentValueTupleType = restField.FieldType;
                if (!MapperUntils.IsValueTuple(currentValueTupleType))
                {
                    throw new InvalidOperationException("The Rest field of a ValueTuple must contain a nested ValueTuple of arity 1 or greater.");
                }
            }

            var stringEnumLocal = (LocalBuilder)null;

            for (var i = 0; i < languageTupleElementTypes.Count; i++)
            {
                var targetType = languageTupleElementTypes[i];

                if (i < length)
                {
                    LoadReaderValueOrBranchToDBNullLabel(
                        il,
                        startBound + i,
                        ref stringEnumLocal,
                        valueCopyLocal: null,
                        reader.GetFieldType(startBound + i),
                        targetType,
                        localDb,
                        out var isDbNullLabel, out bool popWhenNull);

                    var finishLabel = il.DefineLabel();
                    il.Emit(OpCodes.Br_S, finishLabel);
                    il.MarkLabel(isDbNullLabel);
                    if (popWhenNull)
                    {
                        il.Emit(OpCodes.Pop);
                    }

                    LoadDefaultValue(il, targetType);

                    il.MarkLabel(finishLabel);
                }
                else
                {
                    LoadDefaultValue(il, targetType);
                }
            }

            for (var i = constructors.Count - 1; i >= 0; i--)
            {
                il.Emit(OpCodes.Newobj, constructors[i]);
            }

            if (nullableUnderlyingType != null)
            {
                var nullableTupleConstructor = valueTupleType.GetConstructor(new[] { nullableUnderlyingType });

                il.Emit(OpCodes.Newobj, nullableTupleConstructor);
            }

            il.Emit(OpCodes.Box, valueTupleType);
            il.Emit(OpCodes.Ret);
        }

        /// <summary>
        /// 第一分支
        /// </summary>
        /// <param name="type"></param>
        /// <param name="reader"></param>
        /// <param name="startBound"></param>
        /// <param name="length"></param>
        /// <param name="returnNullIfFirstMissing"></param>
        /// <param name="il"></param>
        /// <exception cref="InvalidOperationException"></exception>
        private void GenePackFromMap(Type type, DbDataReader reader, int startBound, int length, bool returnNullIfFirstMissing, ILGenerator il)
        {
            var currentIndexDiagnosticLocal = il.DeclareLocal(typeof(int));
            var returnValueLocal = il.DeclareLocal(type);

            LocalBuilder localDb = il.DeclareLocal(typeof(DBInstance));
            il.Emit(OpCodes.Ldarg_1); // 
            il.Emit(OpCodes.Stloc, localDb); // 

            il.Emit(OpCodes.Ldc_I4_0);
            il.Emit(OpCodes.Stloc, currentIndexDiagnosticLocal);

            var names = Enumerable.Range(startBound, length).Select(i => reader.GetName(i)).ToArray();

            ITypeMap typeMap = GetTypeMap(type);

            int index = startBound;
            ConstructorInfo specializedConstructor = null;

            bool supportInitialize = false;
            Dictionary<Type, LocalBuilder> structLocals = null;
            if (type.IsValueType)
            {
                il.Emit(OpCodes.Ldloca, returnValueLocal);
                il.Emit(OpCodes.Initobj, type);
            }
            else
            {
                var types = new Type[length];
                for (int i = startBound; i < startBound + length; i++)
                {
                    types[i - startBound] = reader.GetFieldType(i);
                }
                
                var ctor = typeMap.FindConstructor(names, types, this);
                if (ctor is null)
                {
                    string proposedTypes = "(" + string.Join(", ", types.Select((t, i) => t.FullName + " " + names[i]).ToArray()) + ")";
                    throw new InvalidOperationException($"A parameterless default constructor or one matching signature {proposedTypes} is required for {type.FullName} materialization");
                }

                if (ctor.GetParameters().Length == 0)
                {
                    il.Emit(OpCodes.Newobj, ctor);
                    il.Emit(OpCodes.Stloc, returnValueLocal);
                    supportInitialize = typeof(ISupportInitialize).IsAssignableFrom(type);
                    if (supportInitialize)
                    {
                        il.Emit(OpCodes.Ldloc, returnValueLocal);
                        il.EmitCall(OpCodes.Callvirt, typeof(ISupportInitialize).GetMethod(nameof(ISupportInitialize.BeginInit)), null);
                    }
                }
                else
                {
                    specializedConstructor = ctor;
                }
                
            }

            il.BeginExceptionBlock();
            if (type.IsValueType)
            {
                il.Emit(OpCodes.Ldloca, returnValueLocal); // [target]
            }
            else if (specializedConstructor is null)
            {
                il.Emit(OpCodes.Ldloc, returnValueLocal); // [target]
            }

            var members = (specializedConstructor != null
                ? names.Select(n => typeMap.GetConstructorParameter(specializedConstructor, n))
                : names.Select(n => typeMap.GetMember(n))).ToList();

            // stack is now [target]
            bool first = true;
            var allDone = il.DefineLabel();
            var stringEnumLocal = (LocalBuilder)null;
            var valueCopyDiagnosticLocal = il.DeclareLocal(typeof(object));
            bool applyNullSetting = Settings.ApplyNullValues;
            foreach (var item in members)
            {
                if (item != null)
                {
                    if (specializedConstructor is null)
                        il.Emit(OpCodes.Dup); // stack is now [target][target]
                    Label finishLabel = il.DefineLabel();
                    Type memberType = item.MemberType;

                    // Save off the current index for access if an exception is thrown
                    EmitInt32(il, index);
                    il.Emit(OpCodes.Stloc, currentIndexDiagnosticLocal);

                    LoadReaderValueOrBranchToDBNullLabel(il, index, ref stringEnumLocal, valueCopyDiagnosticLocal, reader.GetFieldType(index), memberType, localDb, out var isDbNullLabel, out bool popWhenNull);

                    if (specializedConstructor is null)
                    {
                        // Store the value in the property/field
                        if (item.Property != null)
                        {
                            il.Emit(type.IsValueType ? OpCodes.Call : OpCodes.Callvirt, DefaultTypeMap.GetPropertySetterOrThrow(item.Property, type));
                        }
                        else
                        {
                            il.Emit(OpCodes.Stfld, item.Field); // stack is now [target]
                        }
                    }

                    il.Emit(OpCodes.Br_S, finishLabel); // stack is now [target]

                    il.MarkLabel(isDbNullLabel); // incoming stack: [target][target][(and possibly value)]
                    if (popWhenNull) il.Emit(OpCodes.Pop); // stack is now [target][target]
                    if (specializedConstructor != null)
                    {
                        LoadDefaultValue(il, item.MemberType);
                    }
                    else if (applyNullSetting && (!memberType.IsValueType || Nullable.GetUnderlyingType(memberType) != null))
                    {
                        // can load a null with this value
                        if (memberType.IsValueType)
                        { // must be Nullable<T> for some T
                            GetTempLocal(il, ref structLocals, memberType, true); // stack is now [target][target][null]
                        }
                        else
                        { // regular reference-type
                            il.Emit(OpCodes.Ldnull); // stack is now [target][target][null]
                        }

                        // Store the value in the property/field
                        if (item.Property != null)
                        {
                            il.Emit(type.IsValueType ? OpCodes.Call : OpCodes.Callvirt, DefaultTypeMap.GetPropertySetterOrThrow(item.Property, type));
                            // stack is now [target]
                        }
                        else
                        {
                            il.Emit(OpCodes.Stfld, item.Field); // stack is now [target]
                        }
                    }
                    else
                    {
                        il.Emit(OpCodes.Pop); // stack is now [target]
                    }

                    if (first && returnNullIfFirstMissing)
                    {
                        il.Emit(OpCodes.Pop);
                        il.Emit(OpCodes.Ldnull); // stack is now [null]
                        il.Emit(OpCodes.Stloc, returnValueLocal);
                        il.Emit(OpCodes.Br, allDone);
                    }

                    il.MarkLabel(finishLabel);
                }
                first = false;
                index++;
            }
            if (type.IsValueType)
            {
                il.Emit(OpCodes.Pop);
            }
            else
            {
                if (specializedConstructor != null)
                {
                    il.Emit(OpCodes.Newobj, specializedConstructor);
                }
                il.Emit(OpCodes.Stloc, returnValueLocal); // stack is empty
                if (supportInitialize)
                {
                    il.Emit(OpCodes.Ldloc, returnValueLocal);
                    il.EmitCall(OpCodes.Callvirt, typeof(ISupportInitialize).GetMethod(nameof(ISupportInitialize.EndInit)), null);
                }
            }
            il.MarkLabel(allDone);
            il.BeginCatchBlock(typeof(Exception)); // stack is Exception
            il.Emit(OpCodes.Ldloc, currentIndexDiagnosticLocal); // stack is Exception, index
            il.Emit(OpCodes.Ldarg_0); // stack is Exception, index, reader
            il.Emit(OpCodes.Ldloc, valueCopyDiagnosticLocal); // stack is Exception, index, reader, value
            //var mu = typeof(MapperUntils).GetMethods( BindingFlags.Static);
            //var me = typeof(MapperUntils).GetMethod(nameof(MapperUntils.ThrowDataException));
            //注意，这里方法必须为public 否则取不到
            il.EmitCall(OpCodes.Call, typeof(MapperUntils).GetMethod(nameof(MapperUntils.ThrowDataException)), null);
            il.EndExceptionBlock();

            il.Emit(OpCodes.Ldloc, returnValueLocal); // stack is [rval]
            if (type.IsValueType)
            {
                il.Emit(OpCodes.Box, type);
            }
            il.Emit(OpCodes.Ret);
        }
        /// <summary>
        /// 加载值，空时跳转到空标签，否则读取值并赋值
        /// </summary>
        /// <param name="il"></param>
        /// <param name="index"></param>
        /// <param name="stringEnumLocal"></param>
        /// <param name="valueCopyLocal"></param>
        /// <param name="colType"></param>
        /// <param name="memberType"></param>
        /// <param name="isDbNullLabel"></param>
        /// <param name="popWhenNull"></param>
        private void LoadReaderValueOrBranchToDBNullLabel(ILGenerator il, int index, ref LocalBuilder stringEnumLocal, LocalBuilder valueCopyLocal, Type colType, Type memberType, LocalBuilder localDb, out Label isDbNullLabel, out bool popWhenNull)
        {
            isDbNullLabel = il.DefineLabel();
            if (UseGetFieldValue(memberType))
            {
                LoadReaderValueViaGetFieldValue(il, index, memberType, valueCopyLocal, isDbNullLabel, out popWhenNull);
                return;
            }

            popWhenNull = true;
            il.Emit(OpCodes.Ldarg_0); // stack is now [...][reader]


            EmitInt32(il, index); // stack is now [...][reader][index]
            // default impl: use GetValue
            il.Emit(OpCodes.Callvirt, MapperUntils.getItem); // stack is now [...][value-as-object]

            if (valueCopyLocal != null)
            {
                il.Emit(OpCodes.Dup); // stack is now [...][value-as-object][value-as-object]
                il.Emit(OpCodes.Stloc, valueCopyLocal); // stack is now [...][value-as-object]
            }

            if (memberType == typeof(char) || memberType == typeof(char?))
            {
                il.EmitCall(OpCodes.Call, typeof(MapperUntils).GetMethod(
                    memberType == typeof(char) ? nameof(MapperUntils.ReadChar) : nameof(MapperUntils.ReadNullableChar), BindingFlags.Static | BindingFlags.Public), null); // stack is now [...][typed-value]
            }
            else
            {
                il.Emit(OpCodes.Dup); // stack is now [...][value-as-object][value-as-object]
                il.Emit(OpCodes.Isinst, typeof(DBNull)); // stack is now [...][value-as-object][DBNull or null]
                il.Emit(OpCodes.Brtrue_S, isDbNullLabel); // stack is now [...][value-as-object]

                // unbox nullable enums as the primitive, i.e. byte etc

                var nullUnderlyingType = Nullable.GetUnderlyingType(memberType);
                var unboxType = nullUnderlyingType?.IsEnum == true ? nullUnderlyingType : memberType;

                if (unboxType.IsEnum)
                {
                    Type numericType = Enum.GetUnderlyingType(unboxType);
                    if (colType == typeof(string))
                    {
                        if (stringEnumLocal == null) stringEnumLocal = il.DeclareLocal(typeof(string));
                        il.Emit(OpCodes.Castclass, typeof(string)); // stack is now [...][string]
                        il.Emit(OpCodes.Stloc, stringEnumLocal); // stack is now [...]
                        il.Emit(OpCodes.Ldtoken, unboxType); // stack is now [...][enum-type-token]
                        il.EmitCall(OpCodes.Call, typeof(Type).GetMethod(nameof(Type.GetTypeFromHandle)), null);// stack is now [...][enum-type]
                        il.Emit(OpCodes.Ldloc, stringEnumLocal); // stack is now [...][enum-type][string]
                        il.Emit(OpCodes.Ldc_I4_1); // stack is now [...][enum-type][string][true]
                        il.EmitCall(OpCodes.Call, MapperUntils.enumParse, null); // stack is now [...][enum-as-object]
                        il.Emit(OpCodes.Unbox_Any, unboxType); // stack is now [...][typed-value]
                    }
                    else
                    {
                        //异常包裹
                        //Label coneTry = il.BeginExceptionBlock();

                        FlexibleConvertBoxedFromHeadOfStack(il, colType, unboxType, numericType, localDb, memberType);

                        //il.BeginCatchBlock(typeof(Exception)); // stack is Exception
                        //发生异常，则值置为DBNull
                        //il.Emit(OpCodes.Br, isDbNullLabel);
                        //il.EndExceptionBlock();

                    }

                    if (nullUnderlyingType != null)
                    {
                        il.Emit(OpCodes.Newobj, memberType.GetConstructor(new[] { nullUnderlyingType })); // stack is now [...][typed-value]
                    }
                }
                else if (memberType.FullName == MapperUntils.LinqBinary)
                {
                    il.Emit(OpCodes.Unbox_Any, typeof(byte[])); // stack is now [...][byte-array]
                    il.Emit(OpCodes.Newobj, memberType.GetConstructor(new Type[] { typeof(byte[]) }));// stack is now [...][binary]
                }
                else
                {
                    TypeCode dataTypeCode = Type.GetTypeCode(colType), unboxTypeCode = Type.GetTypeCode(unboxType);
                    bool hasTypeHandler;
                    if ((hasTypeHandler = typeHandlers.ContainsKey(unboxType)) || colType == unboxType || dataTypeCode == unboxTypeCode || dataTypeCode == Type.GetTypeCode(nullUnderlyingType))
                    {
                        if (hasTypeHandler)
                        {
#pragma warning disable 618
                            il.EmitCall(OpCodes.Call, typeof(TypeHandlerCache<>).MakeGenericType(unboxType).GetMethod(nameof(TypeHandlerCache<int>.Parse)), null); // stack is now [...][typed-value]
#pragma warning restore 618
                        }
                        else
                        {
                            il.Emit(OpCodes.Unbox_Any, unboxType); // stack is now [...][typed-value]
                        }
                    }
                    else
                    {
                        // not a direct match; need to tweak the unbox
                        //异常包裹
                        //Label convertTry = il.BeginExceptionBlock();
                        FlexibleConvertBoxedFromHeadOfStack(il, colType, nullUnderlyingType ?? unboxType, null, localDb,memberType);

                        //il.BeginCatchBlock(typeof(Exception)); // stack is Exception
                        //发生异常，则值置为DBNull
                        //il.Emit(OpCodes.Ldc_I4_1);
                        //il.Emit(OpCodes.Conv_I1);
                        //il.Emit(OpCodes.Brtrue, isDbNullLabel);
                        //il.EndExceptionBlock();


                        if (nullUnderlyingType != null)
                        {
                            il.Emit(OpCodes.Newobj, unboxType.GetConstructor(new[] { nullUnderlyingType })); // stack is now [...][typed-value]
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 做类型转换，把数据类型转换为成员类型。
        /// </summary>
        /// <param name="il"></param>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <param name="via"></param>
        private void FlexibleConvertBoxedFromHeadOfStack(ILGenerator il, Type from, Type to, Type via, LocalBuilder dblocal,Type memberType)
        {
            MethodInfo op;
            if (from == (via ?? to))
            {
                il.Emit(OpCodes.Unbox_Any, to); // stack is now [target][target][typed-value]
            }
            else if ((op = GetOperator(from, to)) != null)
            {
                // this is handy for things like decimal <===> double
                il.Emit(OpCodes.Unbox_Any, from); // stack is now [target][target][data-typed-value]
                il.Emit(OpCodes.Call, op); // stack is now [target][target][typed-value]
            }

            else
            {
                bool handled = false;
                OpCode opCode = default;
                switch (Type.GetTypeCode(from))
                {
                    case TypeCode.Boolean:
                    case TypeCode.Byte:
                    case TypeCode.SByte:
                    case TypeCode.Int16:
                    case TypeCode.UInt16:
                    case TypeCode.Int32:
                    case TypeCode.UInt32:
                    case TypeCode.Int64:
                    case TypeCode.UInt64:
                    case TypeCode.Single:
                    case TypeCode.Double:
                        handled = true;
                        switch (Type.GetTypeCode(via ?? to))
                        {
                            case TypeCode.Byte:
                                opCode = OpCodes.Conv_Ovf_I1_Un; break;
                            case TypeCode.SByte:
                                opCode = OpCodes.Conv_Ovf_I1; break;
                            case TypeCode.UInt16:
                                opCode = OpCodes.Conv_Ovf_I2_Un; break;
                            case TypeCode.Int16:
                                opCode = OpCodes.Conv_Ovf_I2; break;
                            case TypeCode.UInt32:
                                opCode = OpCodes.Conv_Ovf_I4_Un; break;
                            case TypeCode.Boolean: // boolean is basically an int, at least at this level
                            case TypeCode.Int32:
                                opCode = OpCodes.Conv_Ovf_I4; break;
                            case TypeCode.UInt64:
                                opCode = OpCodes.Conv_Ovf_I8_Un; break;
                            case TypeCode.Int64:
                                opCode = OpCodes.Conv_Ovf_I8; break;
                            case TypeCode.Single:
                                opCode = OpCodes.Conv_R4; break;
                            case TypeCode.Double:
                                opCode = OpCodes.Conv_R8; break;
                            default:
                                handled = false;
                                break;
                        }
                        break;
                }
                if (handled)
                {
                    il.Emit(OpCodes.Unbox_Any, from); // stack is now [target][target][col-typed-value]
                    il.Emit(opCode); // stack is now [target][target][typed-value]
                    if (to == typeof(bool))
                    { // compare to zero; I checked "csc" - this is the trick it uses; nice
                        il.Emit(OpCodes.Ldc_I4_0);
                        il.Emit(OpCodes.Ceq);
                        il.Emit(OpCodes.Ldc_I4_0);
                        il.Emit(OpCodes.Ceq);
                    }
                }
                else if (from == typeof(string) && to == typeof(Guid))
                {
                    // Special case for string to Guid conversion
                    //il.Emit(OpCodes.Pop);// stack is now [target][target][col-value]
                    //il.Emit(OpCodes.Ldarg_0);// stack is now [target][target][col-value]

                    il.Emit(OpCodes.Unbox_Any, from); // stack is now [target][target][string-value]
                                                      //il.Emit(OpCodes.Ldarg_0); // stack is now [target][target][string-value][reader]
                                                      //il.Emit(OpCodes.Pop);
                                                      //il.Emit(OpCodes.Ldarg_1);
                                                      //il.Emit(OpCodes.Ldarg_2);
                                                      //il.Emit(OpCodes.Ldarg_0); // 再次加载参数1（DbDataReader）
                                                      //il.Emit(OpCodes.Ldarg_1);
                                                      //il.Emit(OpCodes.Ldloc, dblocal); // 加载局部变量 db
                                                      //Debugger.Break();

                    //测试调用实例方法

                    //弹出当前栈顶值并临时存放

                    /*
                    LocalBuilder tmpVal=il.DeclareLocal(from);
                    il.Emit(OpCodes.Stloc, tmpVal); // 将当前栈顶值存储到局部变量中 //[target][target]
                    var ctor = typeof(TypeChangeReader).GetConstructor(new[] { typeof(DBInstance) });
                    var getValue = typeof(TypeChangeReader).GetMethod("Convert");
                    getValue = getValue.MakeGenericMethod(from, memberType);
                    il.Emit(OpCodes.Ldloc, dblocal);   // 加载参数（value） //[target][target]
                    il.Emit(OpCodes.Newobj, ctor); // 调用构造函数 
                    
                    il.Emit(OpCodes.Ldloc, tmpVal);
                    il.EmitCall(OpCodes.Call, getValue, null); // 调用方法
                    //il.Emit(OpCodes.Ret);         // 返回结果
                    */

                    il.Emit(OpCodes.Ldloc, dblocal);
                    var mt = typeof(ReadTypeConverter).GetMethod(nameof(ReadTypeConverter.StringToGuid));
                    var met = mt.MakeGenericMethod(from, to);
                    il.Emit(OpCodes.Call, met); // stack is now [target][target][Guid-value]

                    //ConvertValueType
                    // 假设栈上已经有一个 string 类型的值
                    //il.Emit(OpCodes.Ldarg_1); // 加载当前实例（如果方法是实例方法）
                    //il.Emit(OpCodes.Unbox_Any, from);// 假设 string 参数是方法的第一个参数（这里仅为示例，实际情况可能不同）

                    // 获取当前类的 ConvertStringToGuid 方法的 MethodInfo
                    //MethodInfo methodInfo = typeof(Deserializer).GetMethod(nameof(ConvertValueType), BindingFlags.Public | BindingFlags.Instance);
                    //var met = methodInfo.MakeGenericMethod(from, to);

                    // 发出调用指令
                    //il.Emit(OpCodes.Callvirt, met); // 使用 Callvirt 调用实例方法（如果是静态方法，则使用 Call）

                    // 此时栈上应该有一个 Guid 类型的值
                }
                else if (from == typeof(byte[]) && to == typeof(string)) {
                    il.Emit(OpCodes.Ldloc, dblocal);
                    var mt = typeof(ReadTypeConverter).GetMethod(nameof(ReadTypeConverter.ByteArrToString));
                    var met = mt.MakeGenericMethod(from, to);
                    il.Emit(OpCodes.Call, met); // stack is now [target][target][Guid-value]
                }
                else
                {
                    il.Emit(OpCodes.Ldtoken, via ?? to); // stack is now [target][target][value][member-type-token]
                    il.EmitCall(OpCodes.Call, typeof(Type).GetMethod(nameof(Type.GetTypeFromHandle)), null); // stack is now [target][target][value][member-type]
                    il.EmitCall(OpCodes.Call, MapperUntils.InvariantCulture, null); // stack is now [target][target][value][member-type][culture]
                    il.EmitCall(OpCodes.Call, typeof(ReadTypeConverter).GetMethod(nameof(ReadTypeConverter.ChangeType), new Type[] { typeof(object), typeof(Type), typeof(IFormatProvider) }), null); // stack is now [target][target][boxed-member-type-value]
                    il.Emit(OpCodes.Unbox_Any, to); // stack is now [target][target][typed-value]
                }
            }
        }



        private void LoadReaderValueViaGetFieldValue(ILGenerator il, int index, Type memberType, LocalBuilder valueCopyLocal, Label isDbNullLabel, out bool popWhenNull)
        {
            popWhenNull = false;
            var underlyingType = Nullable.GetUnderlyingType(memberType) ?? memberType;

            // for consistency, always do a null check (the GetValue approach always tests for DbNull and jumps)
            il.Emit(OpCodes.Ldarg_0); // stack is now [...][reader]
            EmitInt32(il, index); // stack is now [...][reader][index]
            il.Emit(OpCodes.Callvirt, MapperUntils.isDbNull); // stack is now [...][bool]
            il.Emit(OpCodes.Brtrue_S, isDbNullLabel);

            // DB reports not null; read the value
            il.Emit(OpCodes.Ldarg_0); // stack is now [...][reader]
            EmitInt32(il, index); // stack is now [...][reader][index]
            il.Emit(OpCodes.Callvirt, MapperUntils.getFieldValueT.MakeGenericMethod(underlyingType)); // stack is now [...][T]
            if (valueCopyLocal != null)
            {
                il.Emit(OpCodes.Dup); // stack is now [...][T][T]
                if (underlyingType.IsValueType)
                {
                    il.Emit(OpCodes.Box, underlyingType); // stack is now [...][T][value-as-object]
                }
                il.Emit(OpCodes.Stloc, valueCopyLocal); // stack is now [...][T]
            }
            if (underlyingType != memberType)
            {
                // Nullable<T>; wrap it
                il.Emit(OpCodes.Newobj, memberType.GetConstructor(new[] { underlyingType })); // stack is now [...][T?]
            }
        }


    }
}
