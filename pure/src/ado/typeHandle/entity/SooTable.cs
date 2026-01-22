using System;
using System.Collections.Generic;

namespace mooSQL.data
{

    /// <summary>
    /// 动态表对象，表示数据库查询结果的表结构（字段名和索引）
    /// </summary>
    internal sealed class SooTable
    {
        private string[] fieldNames;
        private readonly Dictionary<string, int> fieldNameLookup;

        internal string[] FieldNames => fieldNames;

        public SooTable(string[] fieldNames)
        {
            this.fieldNames = fieldNames ?? throw new ArgumentNullException(nameof(fieldNames));

            fieldNameLookup = new Dictionary<string, int>(fieldNames.Length, StringComparer.Ordinal);

            for (int i = fieldNames.Length - 1; i >= 0; i--)
            {
                string key = fieldNames[i];
                if (key != null) fieldNameLookup[key] = i;
            }
        }

        internal int IndexOfName(string name)
        {
            return (name != null && fieldNameLookup.TryGetValue(name, out int result)) ? result : -1;
        }

        internal int AddField(string name)
        {
            if (name is null) throw new ArgumentNullException(nameof(name));
            if (fieldNameLookup.ContainsKey(name)) throw new InvalidOperationException("Field already exists: " + name);
            int oldLen = fieldNames.Length;
            Array.Resize(ref fieldNames, oldLen + 1); 
            fieldNames[oldLen] = name;
            fieldNameLookup[name] = oldLen;
            return oldLen;
        }

        internal bool FieldExists(string key) => key != null && fieldNameLookup.ContainsKey(key);

        public int FieldCount => fieldNames.Length;
    }
    
}
