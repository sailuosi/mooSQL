using System;

namespace mooSQL.data.health
{
    /// <summary>
    /// 数据库实例不可用。
    /// </summary>
    public class DBUnavailableException : Exception
    {
        /// <summary>
        /// 属性 Instance（DBInstance）。
        /// </summary>
        public DBInstance Instance { get; }

        /// <summary>
        /// 初始化 DBUnavailableException（构造）。
        /// </summary>
        public DBUnavailableException(DBInstance instance, string message)
            : base(message)
        {
            Instance = instance;
        }
    }
}