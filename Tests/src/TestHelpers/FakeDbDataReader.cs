using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;

namespace mooSQL.Pure.Tests.TestHelpers
{
    internal sealed class FakeDbDataReader : DbDataReader
    {
        private readonly string[] _names;
        private readonly object[] _values;
        private readonly Type[] _types;
        private int _index = -1;

        public FakeDbDataReader(IReadOnlyList<(string Name, object Value, Type Type)> columns)
        {
            _names = columns.Select(c => c.Name).ToArray();
            _values = columns.Select(c => c.Value ?? DBNull.Value).ToArray();
            _types = columns.Select(c => c.Type).ToArray();
        }

        public override int FieldCount => _names.Length;
        public override bool HasRows => _names.Length > 0;
        public override bool IsClosed => false;
        public override int RecordsAffected => 0;
        public override int Depth => 0;

        public override bool Read()
        {
            _index++;
            return _index == 0;
        }

        public override bool NextResult() => false;

        protected override void Dispose(bool disposing)
        {
        }

        public override string GetName(int ordinal) => _names[ordinal];
        public override Type GetFieldType(int ordinal) => _types[ordinal];
        public override object GetValue(int ordinal) => _values[ordinal];
        public override int GetValues(object[] values) => throw new NotSupportedException();
        public override bool IsDBNull(int ordinal) => _values[ordinal] is DBNull;

        public override int GetOrdinal(string name)
        {
            for (int i = 0; i < _names.Length; i++)
                if (string.Equals(_names[i], name, StringComparison.OrdinalIgnoreCase))
                    return i;
            throw new IndexOutOfRangeException(name);
        }

        public override bool GetBoolean(int ordinal) => (bool)_values[ordinal];
        public override byte GetByte(int ordinal) => (byte)_values[ordinal];
        public override long GetBytes(int ordinal, long dataOffset, byte[]? buffer, int bufferOffset, int length) => throw new NotSupportedException();
        public override char GetChar(int ordinal) => (char)_values[ordinal];
        public override long GetChars(int ordinal, long dataOffset, char[]? buffer, int bufferOffset, int length) => throw new NotSupportedException();
        public override string GetDataTypeName(int ordinal) => GetFieldType(ordinal).Name;
        public override DateTime GetDateTime(int ordinal) => (DateTime)_values[ordinal];
        public override decimal GetDecimal(int ordinal) => (decimal)_values[ordinal];
        public override double GetDouble(int ordinal) => (double)_values[ordinal];
        public override float GetFloat(int ordinal) => (float)_values[ordinal];
        public override Guid GetGuid(int ordinal) => (Guid)_values[ordinal];
        public override short GetInt16(int ordinal) => (short)_values[ordinal];
        public override int GetInt32(int ordinal) => (int)_values[ordinal];
        public override long GetInt64(int ordinal) => (long)_values[ordinal];
        public override string GetString(int ordinal) => (string)_values[ordinal];
        public override object this[int ordinal] => GetValue(ordinal);
        public override object this[string name] => GetValue(GetOrdinal(name));
        public override IEnumerator GetEnumerator() => throw new NotSupportedException();
    }
}
