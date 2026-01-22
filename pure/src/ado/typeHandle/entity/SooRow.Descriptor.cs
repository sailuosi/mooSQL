using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace mooSQL.data
{

    [TypeDescriptionProvider(typeof(SooRowTypeDescriptionProvider))]
    internal sealed partial class SooRow
    {
        /// <summary>
        /// SooRow 的类型描述提供程序，用于支持属性描述符
        /// </summary>
        private sealed class SooRowTypeDescriptionProvider : TypeDescriptionProvider
        {
            public override ICustomTypeDescriptor GetExtendedTypeDescriptor(object instance)
                => new SooRowTypeDescriptor(instance);
            public override ICustomTypeDescriptor GetTypeDescriptor(Type objectType, object instance)
                => new SooRowTypeDescriptor(instance);
        }

        /// <summary>
        /// SooRow 的自定义类型描述符，提供属性描述符集合
        /// </summary>
        private sealed class SooRowTypeDescriptor : ICustomTypeDescriptor
        {
            private readonly SooRow _row;
            public SooRowTypeDescriptor(object instance)
                => _row = (SooRow)instance;

            AttributeCollection ICustomTypeDescriptor.GetAttributes()
                => AttributeCollection.Empty;

            string ICustomTypeDescriptor.GetClassName() => typeof(SooRow).FullName;

            string ICustomTypeDescriptor.GetComponentName() => null;

            private static readonly TypeConverter s_converter = new ExpandableObjectConverter();
            TypeConverter ICustomTypeDescriptor.GetConverter() => s_converter;

            EventDescriptor ICustomTypeDescriptor.GetDefaultEvent() => null;

            PropertyDescriptor ICustomTypeDescriptor.GetDefaultProperty() => null;

            object ICustomTypeDescriptor.GetEditor(Type editorBaseType) => null;

            EventDescriptorCollection ICustomTypeDescriptor.GetEvents() => EventDescriptorCollection.Empty;

            EventDescriptorCollection ICustomTypeDescriptor.GetEvents(Attribute[] attributes) => EventDescriptorCollection.Empty;

            internal static PropertyDescriptorCollection GetProperties(SooRow row) => GetProperties(row?.table, row);
            internal static PropertyDescriptorCollection GetProperties(SooTable table, IDictionary<string,object> row = null)
            {
                string[] names = table?.FieldNames;
                if (names is null || names.Length == 0) return PropertyDescriptorCollection.Empty;
                var arr = new PropertyDescriptor[names.Length];
                for (int i = 0; i < arr.Length; i++)
                {
                    var type = row != null && row.TryGetValue(names[i], out var value) && value != null
                        ? value.GetType() : typeof(object);
                    arr[i] = new RowBoundPropertyDescriptor(type, names[i], i);
                }
                return new PropertyDescriptorCollection(arr, true);
            }
            PropertyDescriptorCollection ICustomTypeDescriptor.GetProperties() => GetProperties(_row);

            PropertyDescriptorCollection ICustomTypeDescriptor.GetProperties(Attribute[] attributes) => GetProperties(_row);

            object ICustomTypeDescriptor.GetPropertyOwner(PropertyDescriptor pd) => _row;
        }

        /// <summary>
        /// 行绑定的属性描述符，将 SooRow 的字段映射为属性
        /// </summary>
        private sealed class RowBoundPropertyDescriptor : PropertyDescriptor
        {
            private readonly Type _type;
            private readonly int _index;
            public RowBoundPropertyDescriptor(Type type, string name, int index) : base(name, null)
            {
                _type = type;
                _index = index;
            }
            public override bool CanResetValue(object component) => true;
            public override void ResetValue(object component) => ((SooRow)component).Remove(_index);
            public override bool IsReadOnly => false;
            public override bool ShouldSerializeValue(object component) => ((SooRow)component).TryGetValue(_index, out _);
            public override Type ComponentType => typeof(SooRow);
            public override Type PropertyType => _type;
            public override object GetValue(object component)
                => ((SooRow)component).TryGetValue(_index, out var val) ? (val ?? DBNull.Value): DBNull.Value;
            public override void SetValue(object component, object value)
                => ((SooRow)component).SetValue(_index, value is DBNull ? null : value);
        }
    }
    
}
