using mooSQL.data.model;
using mooSQL.utils;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.Common;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data.mapping
{
    /// <summary>
    /// 类型映射总控，用于各种方向的映射转换
    /// </summary>
    public class MappingPanel
    {
        /// <summary>
        /// 映射转换器集合
        /// </summary>
        public ConcurrentDictionary<string, MappingGroup> Converters { get; set; }
        /// <summary>
        /// 值转换器集合，将一个值转换为另外一个值的转换器，比如将枚举转换为int等
        /// </summary>
        public MappingGroup ValueConverter { get; set; }
        /// <summary>
        /// Sharp类型到数据库类型的映射集合
        /// </summary>
        public MappingValue<Type, DbDataType> SharpToDataType { get; set; }
        /// <summary>
        /// 类型默认值映射集合
        /// </summary>
        public MappingValue<Type, object> TypeDefaultValue { get; set; }

        /// <summary>
        /// 类型默认值映射集合
        /// </summary>
        public MappingValue<Type, bool> TypeNullable { get; set; }

        /// <summary>
        /// 属性 TypeScalar（MappingValue<Type,bool>）。
        /// </summary>
        public MappingValue<Type,bool> TypeScalar {  get; set; }
        /// <summary>
        /// 将Sharp类型转换为安全的SQL 
        /// </summary>
        public MappingGroup ValueToSQL { get; set; }

        /// <summary>
        /// 构造函数
        /// </summary>
        public MappingPanel()
        {
            Converters = new ConcurrentDictionary<string, MappingGroup>();
            SharpToDataType = new MappingValue<Type, DbDataType>("SharpToDataType");
            TypeDefaultValue = new MappingValue<Type, object>("TypeDefaultValue");
            ValueToSQL = new MappingGroup("ValueToSQL");
            TypeScalar = new MappingValue<Type, bool>("TypeScalar");
            TypeNullable = new MappingValue<Type, bool>("TypeNullable");

            this.ValueConverter = new MappingGroup("ValueConverter");
            this.Converters.TryAdd("ValueConverter", ValueConverter);
            this.Converters.TryAdd("ValueToSQL", ValueToSQL);
        }
        /// <summary>
        /// 设置类型映射
        /// </summary>
        /// <param name="type"></param>
        /// <param name="dataType"></param>
        protected void SetDataType(Type type, DataFam dataType)
        {
            SharpToDataType.Add(type,new DbDataType (dataType));
        }

        /// <summary>
        /// 获取DbDataType。
        /// </summary>
        public DbDataType GetDbDataType(Type type) { 
            return SharpToDataType.Get(type);
        }
        /// <summary>
        /// 设置类型映射
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="dataType"></param>
        protected void SetDataType<T>(DataFam dataType)
        {
            SharpToDataType.Add(typeof(T), new DbDataType(dataType));
        }

        /// <summary>
        /// 泛型方法 SetDataType（返回 void）。
        /// </summary>
        protected void SetDataType<T>(T defaultValue,bool canbeNull,DataFam dataType)
        {
            var t = typeof(T);
            SharpToDataType.Add(t, new DbDataType(dataType));
            TypeDefaultValue.Add(t, defaultValue);
            TypeNullable.Add(t, canbeNull);

        }


        /// <summary>
        /// 设置类型默认值
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="Value"></param>
        protected void SetDefaultValue<T>(T Value)
        {
            TypeDefaultValue.Add(typeof(T), Value);
        }

        /// <summary>
        /// 获取DefaultValue。
        /// </summary>
        public object GetDefaultValue(Type type)
        {
            var v = TypeDefaultValue.Get(type);
            return v;
        }
        /// <summary>
        /// 泛型方法 GetDefaultValue（返回 object）。
        /// </summary>
        public object GetDefaultValue<T>() { 
            var t = typeof(T);
            var v= TypeDefaultValue.Get(t);
            return v;
        }
        /// <summary>
        /// 设置类型是否可为空
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="nullable"></param>
        protected void SetNullable<T>(bool nullable) { 
            TypeNullable.Add(typeof(T), nullable);
        }

        /// <summary>
        /// 获取TypeNullable。
        /// </summary>
        public bool GetTypeNullable(Type t) {
            var res = TypeNullable.Get(t);
            return res;
        }

        /// <summary>
        /// 泛型方法 SetScalarType（返回 void）。
        /// </summary>
        protected void SetScalarType<T>()
        {
            TypeScalar.Add(typeof(T), true);
        }
        /// <summary>
        /// 设置ScalarType。
        /// </summary>
        protected void SetScalarType(Type t)
        {
            TypeScalar.Add(t, true);
        }
        /// <summary>
        /// 获取IsScalarType。
        /// </summary>
        public bool GetIsScalarType(Type t)
        {
            var res = TypeScalar.Get(t);
            return res;
        }

        /// <summary>
        /// 添加ScalarType。
        /// </summary>
        public void AddScalarType(Type type, DataFam dataType = DataFam.Undefined)
        {
            SetScalarType(type);

            if (dataType != DataFam.Undefined)
                SetDataType(type, dataType);
        }
        /// <summary>
        /// 类型是否为标量类型，标量类型可以直接用在SQL语句中，比如int,string等
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public bool IsScalarType(Type type)
        {
            if (TypeScalar.TryGet(type, out var val)) { 
                return val;
            }
            var ret = false;

            type = type.UnwrapNullable();

            if (type.IsEnum || type.IsPrimitive ||  type.IsValueType)
                ret = true;
            

            return ret;
        }

        /// <summary>
        /// 设置值为SQL的转换器
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="onConverting"></param>
        protected void SetValueToSql<T>(Func<T, string> onConverting)
        {
            ValueToSQL.Add(onConverting);
        }

        /// <summary>
        /// 泛型方法 CanConvertToSql（返回 bool）。
        /// </summary>
        public bool CanConvertToSql<T>(T value)
        {
            return ValueToSQL.CanConvert<T,string>();
        }

        /// <summary>
        /// 设置两个不同类型的值转换器
        /// </summary>
        /// <typeparam name="VFrom"></typeparam>
        /// <typeparam name="VTo"></typeparam>
        /// <param name="onConverting"></param>
        protected void SetValueConverter<VFrom,VTo>(Func<VFrom, VTo> onConverting)
        {
            ValueConverter.Add(onConverting);
        }
        /// <summary>
        /// 获取 VFrom 到 VTo 的值转换委托。
        /// </summary>
        public Func<VFrom, VTo> GetValueConverter<VFrom, VTo>()
        {
            return ValueConverter.Get<VFrom, VTo>();
        }
        /// <summary>
        /// 获取ValueConverter。
        /// </summary>
        public Delegate GetValueConverter(Type from,Type to)
        {
            return ValueConverter.Get(from,to);
        }

        /// <summary>
        /// 泛型方法 ChangeTypeTo（返回 T）。
        /// </summary>
        public T ChangeTypeTo<S, T>(S value) {
            try
            {

                if (ValueConverter.CanConvert<S, T>())
                {
                    var r = ValueConverter.Convert<S, T>(value);
                }
                var t= TypeConverterUtil.Convert<T>(value);
                return t;
            }
            catch (Exception ex) { 
                return default(T);
            }
        }
        /// <summary>
        /// 泛型方法 ChangeTypeTo（返回 T）。
        /// </summary>
        public T ChangeTypeTo<T>(object value)
        {
            try
            {
                var srct = value.GetType();
                if (ValueConverter.CanConvert(srct,typeof(T)))
                {
                    var r = ValueConverter.Convert<T>(value);
                }
                var t = TypeConverterUtil.Convert<T>(value);
                return t;
            }
            catch (Exception ex)
            {
                return default(T);
            }
        }
        /// <summary>
        /// ChangeTypeTo 方法（返回 object）。
        /// </summary>
        public object ChangeTypeTo(object value,Type Target)
        {
            try
            {
                var srct = value.GetType();
                if (ValueConverter.CanConvert(srct, Target))
                {
                    var r = ValueConverter.Convert(value,Target);
                }
                var t = TypeConverterUtil.Convert(value,Target, CultureInfo.CurrentCulture);
                return t;
            }
            catch (Exception ex)
            {
                return default;
            }
        }
        /// <summary>
        /// 将数据库类型转换为SQL中的类型声明
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public virtual string DbDataTypeToSQL(DbDataType type) { 
            return type.ToDBString();
        }

        /// <summary>
        /// ConvertParameterType 方法（返回 Type）。
        /// </summary>
        public virtual Type ConvertParameterType(Type type, DbDataType dataType) {
            return type;
        }

        /// <summary>
        /// 设置Parameter。
        /// </summary>
        public virtual void SetParameter(DbCommand cmd, DbParameter parameter, string name, DbDataType dataType, object? value) { 
        
        }

        /// <summary>
        /// 设置ParameterType。
        /// </summary>
        protected virtual void SetParameterType(DbCommand cmd, DbParameter parameter, DbDataType dataType) { 
        }

        /// <summary>
        /// 获取ProviderSpecificType。
        /// </summary>
        public virtual Type GetProviderSpecificType(string dataType) {
            return null;
        }

        /// <summary>
        /// 获取DataType。
        /// </summary>
        public virtual DataFam GetDataType(string? dataType, string? columnType=null) {
            return  DataFam.Undefined;
        }


    }
}