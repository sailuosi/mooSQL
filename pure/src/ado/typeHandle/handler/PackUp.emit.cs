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
        /*
         * IL 指令速查（面向不熟悉 il.Emit 的阅读者）
         * ------------------------------------------------------------
         * 1) 入栈/出栈（把值压到“计算栈”上，或从栈里取走）
         *    - Ldarg_n       : 读取第 n 个方法参数并压栈
         *                     伪代码：stack.Push(argN)
         *    - Ldloc / Ldloca: 读取局部变量值 / 地址并压栈
         *                     伪代码：stack.Push(local) / stack.Push(&local)
         *    - Stloc         : 把栈顶值写入局部变量（并弹栈）
         *                     伪代码：local = stack.Pop()
         *    - Ldc_I4_x      : 把整数常量压栈
         *                     伪代码：stack.Push(constInt)
         *    - Ldnull        : 压入 null
         *    - Dup           : 复制栈顶元素（常用于“一个值要消费两次”）
         *    - Pop           : 丢弃栈顶元素（常用于分支后栈平衡）
         *
         * 2) 调用与对象构造
         *    - Call          : 调用静态方法或非虚实例方法
         *    - Callvirt      : 调用实例虚方法（也常用于普通实例方法调用）
         *    - Newobj        : 调用构造函数，创建对象并把新对象压栈
         *                     伪代码：stack.Push(new T(args...))
         *
         * 3) 类型相关
         *    - Box           : 值类型 -> object（装箱）
         *    - Unbox_Any     : object -> 指定值类型（拆箱并取值）
         *    - Castclass     : 引用类型强制转换（失败抛异常）
         *    - Isinst        : 运行时类型测试（成功返回该类型引用，失败返回 null）
         *    - Initobj       : 把某地址处值初始化为 default(T)
         *    - Ldtoken + Type.GetTypeFromHandle:
         *                     把类型元数据 token 转成 System.Type
         *
         * 4) 跳转与流程控制
         *    - DefineLabel / MarkLabel: 定义并标记跳转目标
         *    - Br / Br_S     : 无条件跳转
         *    - Brtrue / Brtrue_S:
         *                     栈顶为 true（或非 null）则跳转
         *
         * 5) 常见“等价 C# 思维模型”
         *    - reader[index] + DBNull 判断：
         *        object value = reader[index];
         *        if (value is DBNull) { ... } else { ... }
         *    - 值类型返回：
         *        return (object)someStruct; // 对应 Box + Ret
         *    - Nullable 包装：
         *        new Nullable<T>(value);    // 对应 Newobj(nullableCtor)
         *
         * 备注：
         * - IL 是“栈机”模型：每条指令都在操作计算栈。
         * - 看不懂时，优先盯住每步注释里的 stack 变化，就能把流程还原成 C#。
         */
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
            // DynamicMethod 签名：
            // (DbDataReader reader, DBInstance db) => returnType
            // 这里 returnType 对值类型统一提升为 object，便于统一委托返回。
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
            // 这一分支专门处理 ValueTuple / Nullable<ValueTuple> 的反序列化：
            // 整体策略是“按 Item 顺序把每个元素值压栈 -> 倒序 Newobj 组装嵌套 tuple -> 返回 object”
            LocalBuilder localDb = il.DeclareLocal(typeof(DBInstance));
            // 等价 C#: localDb = db;
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Stloc, localDb);

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
                    // 读取第 i 列并尝试转换成 tuple 对应元素类型：
                    // 成功时，栈顶得到 [elementValue]
                    // DBNull 时，跳转到 isDbNullLabel，再压默认值
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
                    // 正常分支：直接跳过 DBNull 分支
                    il.Emit(OpCodes.Br_S, finishLabel);
                    il.MarkLabel(isDbNullLabel);
                    if (popWhenNull)
                    {
                        // 清理 DBNull 分支中可能残留的 value/object
                        il.Emit(OpCodes.Pop);
                    }

                    // DBNull 分支：补元素默认值（default(TItem)）
                    LoadDefaultValue(il, targetType);

                    il.MarkLabel(finishLabel);
                }
                else
                {
                    // reader 列数不足时，后续 Item 统一补默认值
                    LoadDefaultValue(il, targetType);
                }
            }

            for (var i = constructors.Count - 1; i >= 0; i--)
            {
                // 逆序组装嵌套 ValueTuple：
                // 栈上参数顺序已经准备好，Newobj 会消费参数并压回构造结果
                // 等价 C#: vt = new ValueTuple<...>(item1, item2, ..., rest)
                il.Emit(OpCodes.Newobj, constructors[i]);
            }

            if (nullableUnderlyingType != null)
            {
                var nullableTupleConstructor = valueTupleType.GetConstructor(new[] { nullableUnderlyingType });

                // Nullable<ValueTuple> 包装
                // 等价 C#: new Nullable<ValueTuple>(tupleValue)
                il.Emit(OpCodes.Newobj, nullableTupleConstructor);
            }

            // DynamicMethod 返回 object：值类型需要装箱
            // 等价 C#: return (object)tuple;
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
            // 诊断用：记录当前正在处理的列索引，异常时用于拼接错误信息
            var currentIndexDiagnosticLocal = il.DeclareLocal(typeof(int));
            // 最终返回对象（或结构体）的本地变量
            var returnValueLocal = il.DeclareLocal(type);

            LocalBuilder localDb = il.DeclareLocal(typeof(DBInstance));
            // Ldarg_1: 把第2个参数压栈（这里是 DBInstance db）
            // Stloc:   把栈顶值保存到局部变量 localDb
            // 等价 C#: localDb = db;
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Stloc, localDb);

            // Ldc_I4_0: 压栈整数 0；Stloc: 存入局部变量
            // 等价 C#: currentIndexDiagnosticLocal = 0;
            il.Emit(OpCodes.Ldc_I4_0);
            il.Emit(OpCodes.Stloc, currentIndexDiagnosticLocal);

            // 从 reader 当前区间提取列名，后续用于成员匹配（构造参数/属性/字段）
            var names = Enumerable.Range(startBound, length).Select(i => reader.GetName(i)).ToArray();

            ITypeMap typeMap = GetTypeMap(type);

            int index = startBound;
            ConstructorInfo specializedConstructor = null;

            bool supportInitialize = false;
            Dictionary<Type, LocalBuilder> structLocals = null;
            if (type.IsValueType)
            {
                // 值类型先做 Initobj，保证有一个可写入的默认实例
                // Ldloca: 取局部变量地址；Initobj: 将该地址对应值置为 default(T)
                // 等价 C#: returnValueLocal = default(T);
                il.Emit(OpCodes.Ldloca, returnValueLocal);
                il.Emit(OpCodes.Initobj, type);
            }
            else
            {
                // 引用类型：根据列名+列类型尝试匹配最合适的构造函数
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
                    // 无参构造：先创建对象，再逐列赋值到属性/字段
                    // Newobj: 调用构造函数并把新对象压栈；Stloc: 保存到 returnValueLocal
                    // 等价 C#: returnValueLocal = new T();
                    il.Emit(OpCodes.Newobj, ctor);
                    il.Emit(OpCodes.Stloc, returnValueLocal);
                    supportInitialize = typeof(ISupportInitialize).IsAssignableFrom(type);
                    if (supportInitialize)
                    {
                        // 支持初始化协议的对象，在批量赋值前调用 BeginInit
                        // Ldloc + Callvirt: 调用实例虚方法
                        // 等价 C#: ((ISupportInitialize)returnValueLocal).BeginInit();
                        il.Emit(OpCodes.Ldloc, returnValueLocal);
                        il.EmitCall(OpCodes.Callvirt, typeof(ISupportInitialize).GetMethod(nameof(ISupportInitialize.BeginInit)), null);
                    }
                }
                else
                {
                    // 有参构造：循环中仅压栈各参数值，循环结束后一次性 Newobj
                    specializedConstructor = ctor;
                }
                
            }

            // 外围异常包装：任何列解析异常都转为包含列索引/列值的诊断异常
            il.BeginExceptionBlock();
            if (type.IsValueType)
            {
                // Ldloca: 压入目标结构体地址（后续 setter/stfld 需要“可写地址”）
                il.Emit(OpCodes.Ldloca, returnValueLocal); // [target]
            }
            else if (specializedConstructor is null)
            {
                // Ldloc: 压入目标对象引用（后续属性/字段赋值会消费这个引用）
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
                        // Dup: 复制栈顶 target。一个留给下一列，另一个给本列 setter/stfld 消费
                        // 等价思路：保留 thisObj，同时把 thisObj 传给当前成员赋值
                        il.Emit(OpCodes.Dup); // stack is now [target][target]
                    Label finishLabel = il.DefineLabel();
                    Type memberType = item.MemberType;

                    // Save off the current index for access if an exception is thrown
                    EmitInt32(il, index);
                    // Stloc: currentIndexDiagnosticLocal = index;
                    il.Emit(OpCodes.Stloc, currentIndexDiagnosticLocal);

                    LoadReaderValueOrBranchToDBNullLabel(il, index, ref stringEnumLocal, valueCopyDiagnosticLocal, reader.GetFieldType(index), memberType, localDb, out var isDbNullLabel, out bool popWhenNull);

                    if (specializedConstructor is null)
                    {
                        // 无参构造路径：直接把解析后的值写入属性/字段
                        if (item.Property != null)
                        {
                            // Call/Callvirt: 调用属性 setter
                            // 等价 C#: target.<Prop> = convertedValue;
                            il.Emit(type.IsValueType ? OpCodes.Call : OpCodes.Callvirt, DefaultTypeMap.GetPropertySetterOrThrow(item.Property, type));
                        }
                        else
                        {
                            // Stfld: 字段赋值
                            // 等价 C#: target.<Field> = convertedValue;
                            il.Emit(OpCodes.Stfld, item.Field); // stack is now [target]
                        }
                    }

                    // Br_S: 无条件短跳转到 finishLabel，跳过 DBNull 分支
                    il.Emit(OpCodes.Br_S, finishLabel); // stack is now [target]

                    il.MarkLabel(isDbNullLabel); // incoming stack: [target][target][(and possibly value)]
                    if (popWhenNull) il.Emit(OpCodes.Pop); // stack is now [target][target]
                    if (specializedConstructor != null)
                    {
                        // 有参构造路径：DBNull 时压入该参数类型默认值，保持参数数量一致
                        LoadDefaultValue(il, item.MemberType);
                    }
                    else if (applyNullSetting && (!memberType.IsValueType || Nullable.GetUnderlyingType(memberType) != null))
                    {
                        // 无参构造路径：允许将 null 写入可空成员（引用类型或 Nullable<T>）
                        if (memberType.IsValueType)
                        { // must be Nullable<T> for some T
                            GetTempLocal(il, ref structLocals, memberType, true); // stack is now [target][target][null]
                        }
                        else
                        { // regular reference-type
                            // Ldnull: 压入 null
                            il.Emit(OpCodes.Ldnull); // stack is now [target][target][null]
                        }

                        // Store the value in the property/field
                        if (item.Property != null)
                        {
                            // 等价 C#: target.<Prop> = null; (或 Nullable<T> 的 null)
                            il.Emit(type.IsValueType ? OpCodes.Call : OpCodes.Callvirt, DefaultTypeMap.GetPropertySetterOrThrow(item.Property, type));
                            // stack is now [target]
                        }
                        else
                        {
                            // 等价 C#: target.<Field> = null;
                            il.Emit(OpCodes.Stfld, item.Field); // stack is now [target]
                        }
                    }
                    else
                    {
                        // Pop: 丢弃多余 target（本列不赋值，维持栈平衡）
                        il.Emit(OpCodes.Pop); // stack is now [target]
                    }

                    if (first && returnNullIfFirstMissing)
                    {
                        // 可选行为：第一列缺失/为 DBNull 时直接返回 null（常用于左连接场景）
                        // Pop: 清理 target；Ldnull+Stloc: returnValueLocal = null；Br: 跳到收尾
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
                // 值类型路径中，栈上仍有 target 地址副本，Pop 清理掉
                il.Emit(OpCodes.Pop);
            }
            else
            {
                if (specializedConstructor != null)
                {
                    // 有参构造路径：循环中已压栈所有参数，这里一次性构造目标对象
                    // Newobj specializedConstructor 等价 C#: new T(arg1, arg2, ...)
                    il.Emit(OpCodes.Newobj, specializedConstructor);
                }
                // Stloc: returnValueLocal = 构造结果（或无参路径已有对象）
                il.Emit(OpCodes.Stloc, returnValueLocal); // stack is empty
                if (supportInitialize)
                {
                    // 与 BeginInit 配对，通知对象初始化结束
                    // 等价 C#: ((ISupportInitialize)returnValueLocal).EndInit();
                    il.Emit(OpCodes.Ldloc, returnValueLocal);
                    il.EmitCall(OpCodes.Callvirt, typeof(ISupportInitialize).GetMethod(nameof(ISupportInitialize.EndInit)), null);
                }
            }
            il.MarkLabel(allDone);
            il.BeginCatchBlock(typeof(Exception)); // stack is Exception
            // 统一抛出携带上下文的异常：包含列序号、reader、列值副本，便于定位转换失败
            // Ldloc/Ldarg 依次准备参数，Call 调用静态方法 ThrowDataException(ex, index, reader, value)
            il.Emit(OpCodes.Ldloc, currentIndexDiagnosticLocal); // stack is Exception, index
            il.Emit(OpCodes.Ldarg_0); // stack is Exception, index, reader
            il.Emit(OpCodes.Ldloc, valueCopyDiagnosticLocal); // stack is Exception, index, reader, value
            //var mu = typeof(MapperUntils).GetMethods( BindingFlags.Static);
            //var me = typeof(MapperUntils).GetMethod(nameof(MapperUntils.ThrowDataException));
            //注意，这里方法必须为public 否则取不到
            il.EmitCall(OpCodes.Call, typeof(MapperUntils).GetMethod(nameof(MapperUntils.ThrowDataException)), null);
            il.EndExceptionBlock();

            // 返回值阶段：Ldloc 取 returnValueLocal；值类型需 Box 成 object 再 Ret
            // 等价 C#: return (object)returnValueLocal;
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
                // 快路径：用 reader.GetFieldValue<T>() 读取强类型值，避免 object 装箱/拆箱
                LoadReaderValueViaGetFieldValue(il, index, memberType, valueCopyLocal, isDbNullLabel, out popWhenNull);
                return;
            }

            popWhenNull = true;
            // Ldarg_0 + index + Callvirt(getItem) 等价 C#: object value = reader[index];
            il.Emit(OpCodes.Ldarg_0); // stack is now [...][reader]


            EmitInt32(il, index); // stack is now [...][reader][index]
            // default impl: use GetValue
            il.Emit(OpCodes.Callvirt, MapperUntils.getItem); // stack is now [...][value-as-object]

            if (valueCopyLocal != null)
            {
                // Dup + Stloc: 复制一份原始值用于异常诊断，不影响主流程消费
                il.Emit(OpCodes.Dup); // stack is now [...][value-as-object][value-as-object]
                il.Emit(OpCodes.Stloc, valueCopyLocal); // stack is now [...][value-as-object]
            }

            if (memberType == typeof(char) || memberType == typeof(char?))
            {
                // char 特殊处理：统一调用工具方法从 object 安全转换到 char / char?
                il.EmitCall(OpCodes.Call, typeof(MapperUntils).GetMethod(
                    memberType == typeof(char) ? nameof(MapperUntils.ReadChar) : nameof(MapperUntils.ReadNullableChar), BindingFlags.Static | BindingFlags.Public), null); // stack is now [...][typed-value]
            }
            else
            {
                // DBNull 检测模板：
                // Dup; Isinst DBNull; Brtrue_S isDbNullLabel
                // 等价 C#:
                // if (value is DBNull) goto isDbNullLabel;
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
                        // 字符串枚举解析路径，等价 C#:
                        // var s = (string)value;
                        // var e = (Enum)Enum.Parse(unboxType, s, ignoreCase: true);
                        // typed = (TEnum)e;
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
                            // 有 TypeHandler：调用 Parse(object) 交给自定义处理器
#pragma warning disable 618
                            il.EmitCall(OpCodes.Call, typeof(TypeHandlerCache<>).MakeGenericType(unboxType).GetMethod(nameof(TypeHandlerCache<int>.Parse)), null); // stack is now [...][typed-value]
#pragma warning restore 618
                        }
                        else
                        {
                            // 可直接拆箱/解包，等价 C#: (T)value
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
                            // 目标是 Nullable<T> 时，把底层值包成 new Nullable<T>(value)
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
            // 约定：调用此方法前，栈顶是 boxed object 值（来自 reader.GetValue）
            // 目标：把栈顶转换成 to（或 via）并保持“一个转换后的值”在栈顶
            MethodInfo op;
            if (from == (via ?? to))
            {
                // 直接类型匹配，直接拆箱为目标类型
                // 等价 C#: (TTo)value
                il.Emit(OpCodes.Unbox_Any, to); // stack is now [target][target][typed-value]
            }
            else if ((op = GetOperator(from, to)) != null)
            {
                // this is handy for things like decimal <===> double
                // 两步：先按源类型拆箱，再调用用户定义/内置运算符
                // 等价 C#: op_Implicit/op_Explicit((TFrom)value)
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
                    // 数值转换路径：
                    // (TFrom)value -> Conv_xxx -> TTo
                    il.Emit(OpCodes.Unbox_Any, from); // stack is now [target][target][col-typed-value]
                    il.Emit(opCode); // stack is now [target][target][typed-value]
                    if (to == typeof(bool))
                    { // compare to zero; I checked "csc" - this is the trick it uses; nice
                        // 布尔值规范化：value != 0
                        // IL 里用两次 Ceq 实现 “是否非零”
                        il.Emit(OpCodes.Ldc_I4_0);
                        il.Emit(OpCodes.Ceq);
                        il.Emit(OpCodes.Ldc_I4_0);
                        il.Emit(OpCodes.Ceq);
                    }
                }
                else if (from == typeof(string) && to == typeof(Guid))
                {
                    // 特殊分支：string -> Guid
                    // 等价 C#: ReadTypeConverter.StringToGuid<string, Guid>((string)value, db)
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
                    // 特殊分支：byte[] -> string
                    // 等价 C#: ReadTypeConverter.ByteArrToString<byte[], string>((byte[])value, db)
                    il.Emit(OpCodes.Ldloc, dblocal);
                    var mt = typeof(ReadTypeConverter).GetMethod(nameof(ReadTypeConverter.ByteArrToString));
                    var met = mt.MakeGenericMethod(from, to);
                    il.Emit(OpCodes.Call, met); // stack is now [target][target][Guid-value]
                }
                else if (to == typeof(string) && from != typeof(string))
                {
                    // 通用字符串分支：目标是 string，源不是 string 时调用对象 ToString()
                    // 等价 C#: value.ToString()
                    il.EmitCall(OpCodes.Callvirt, typeof(object).GetMethod(nameof(ToString)), null);
                }
                else
                {
                    // 通用兜底分支：
                    // 1) 压入目标 Type
                    // 2) 压入 InvariantCulture
                    // 3) 调用 ChangeType(object, Type, IFormatProvider)
                    // 4) 把结果拆箱为 to
                    // 等价 C#:
                    // var obj = ReadTypeConverter.ChangeType(value, typeof(TTo), CultureInfo.InvariantCulture);
                    // return (TTo)obj;
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
            // 等价 C#:
            // if (reader.IsDBNull(index)) goto isDbNullLabel;
            il.Emit(OpCodes.Ldarg_0); // stack is now [...][reader]
            EmitInt32(il, index); // stack is now [...][reader][index]
            il.Emit(OpCodes.Callvirt, MapperUntils.isDbNull); // stack is now [...][bool]
            il.Emit(OpCodes.Brtrue_S, isDbNullLabel);

            // DB reports not null; read the value
            // 等价 C#: T value = reader.GetFieldValue<T>(index);
            il.Emit(OpCodes.Ldarg_0); // stack is now [...][reader]
            EmitInt32(il, index); // stack is now [...][reader][index]
            il.Emit(OpCodes.Callvirt, MapperUntils.getFieldValueT.MakeGenericMethod(underlyingType)); // stack is now [...][T]
            if (valueCopyLocal != null)
            {
                // 诊断副本：保存一份原值到 object 局部变量（异常时可带出）
                il.Emit(OpCodes.Dup); // stack is now [...][T][T]
                if (underlyingType.IsValueType)
                {
                    // 值类型存 object 前需要装箱
                    il.Emit(OpCodes.Box, underlyingType); // stack is now [...][T][value-as-object]
                }
                il.Emit(OpCodes.Stloc, valueCopyLocal); // stack is now [...][T]
            }
            if (underlyingType != memberType)
            {
                // Nullable<T>; wrap it
                // 等价 C#: new Nullable<T>(value)
                il.Emit(OpCodes.Newobj, memberType.GetConstructor(new[] { underlyingType })); // stack is now [...][T?]
            }
        }


    }
}
