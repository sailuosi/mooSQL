using System;

namespace mooSQL.data.health
{
    /// <summary>
    /// 数据库实例不可用。
    /// </summary>
    public class DBUnavailableException : Exception
    {
        public DBInstance Instance { get; }

        public DBUnavailableException(DBInstance instance, string message)
            : base(message)
        {
            Instance = instance;
        }
    }
}
