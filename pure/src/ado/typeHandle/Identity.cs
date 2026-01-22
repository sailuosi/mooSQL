using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Runtime.CompilerServices;

namespace mooSQL.data
{

    /// <summary>
    /// 具有七个类型参数的标识类，用于缓存查询结果
    /// </summary>
    /// <typeparam name="TFirst">第一个类型</typeparam>
    /// <typeparam name="TSecond">第二个类型</typeparam>
    /// <typeparam name="TThird">第三个类型</typeparam>
    /// <typeparam name="TFourth">第四个类型</typeparam>
    /// <typeparam name="TFifth">第五个类型</typeparam>
    /// <typeparam name="TSixth">第六个类型</typeparam>
    /// <typeparam name="TSeventh">第七个类型</typeparam>
    internal sealed class Identity<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TSeventh> : Identity
    {
        private static readonly int s_typeHash;
        private static readonly int s_typeCount = CountNonTrivial(out s_typeHash);

        internal Identity(string sql, CommandType commandType, string connectionString, Type type, Type parametersType, int gridIndex = 0)
            : base(sql, commandType, connectionString, type, parametersType, s_typeHash, gridIndex)
        { }
        internal Identity(string sql, CommandType commandType, IDbConnection connection, Type type, Type parametersType, int gridIndex = 0)
            : base(sql, commandType, connection.ConnectionString, type, parametersType, s_typeHash, gridIndex)
        { }

        static int CountNonTrivial(out int hashCode)
        {
            int hashCodeLocal = 0;
            int count = 0;
            bool Map<T>()
            {
                if (typeof(T) != typeof(DontMap))
                {
                    count++;
                    hashCodeLocal = (hashCodeLocal * 23) + (typeof(T).GetHashCode());
                    return true;
                }
                return false;
            }
            _ = Map<TFirst>() && Map<TSecond>() && Map<TThird>()
                && Map<TFourth>() && Map<TFifth>() && Map<TSixth>()
                && Map<TSeventh>();
            hashCode = hashCodeLocal;
            return count;
        }
        internal override int TypeCount => s_typeCount;
        internal override Type GetType(int index)
        {
            switch (index)
            {
                case 0: return typeof(TFirst);
                case 1:
                    return typeof(TSecond);
                case 2:
                    return typeof(TThird);
                case 3:
                    return typeof(TFourth);
                case 4:
                    return typeof(TFifth);
                case 5:
                    return typeof(TSixth);
                case 6:
                    return typeof(TSeventh);
                default: return base.GetType(index);
            }



        } 
    }
        /// <summary>
        /// 具有类型数组的标识类，用于缓存查询结果
        /// </summary>
        internal sealed class IdentityWithTypes : Identity
        {
            private readonly Type[] _types;

            internal IdentityWithTypes(string sql, CommandType commandType, string connectionString, Type type, Type parametersType, Type[] otherTypes, int gridIndex = 0)
                : base(sql, commandType, connectionString, type, parametersType, HashTypes(otherTypes), gridIndex)
            {
                _types = otherTypes ?? Type.EmptyTypes;
            }
            internal IdentityWithTypes(string sql, CommandType commandType, IDbConnection connection, Type type, Type parametersType, Type[] otherTypes, int gridIndex = 0)
                : base(sql, commandType, connection.ConnectionString, type, parametersType, HashTypes(otherTypes), gridIndex)
            {
                _types = otherTypes ?? Type.EmptyTypes;
            }

            internal override int TypeCount => _types.Length;

            internal override Type GetType(int index) => _types[index];

            static int HashTypes(Type[] types)
            {
                var hashCode = 0;
                if (types != null)
                {
                    foreach (var t in types)
                    {
                        hashCode = (hashCode * 23) + (t?.GetHashCode() ?? 0);
                    }
                }
                return hashCode;
            }
        }

        /// <summary>
        /// 用作缓存的id
        /// </summary>
        public class Identity : IEquatable<Identity>
        {
            internal virtual int TypeCount => 0;

            internal virtual Type GetType(int index) => throw new IndexOutOfRangeException(nameof(index));

#pragma warning disable CS0618 // Type or member is obsolete
            internal Identity ForGrid<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TSeventh>(Type primaryType, int gridIndex) =>
                new Identity<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TSeventh>(sql, commandType, connectionString, primaryType, parametersType, gridIndex);

            internal Identity ForGrid(Type primaryType, int gridIndex) =>
                new Identity(sql, commandType, connectionString, primaryType, parametersType, 0, gridIndex);

            internal Identity ForGrid(Type primaryType, Type[] otherTypes, int gridIndex) =>
                (otherTypes is null || otherTypes.Length == 0)
                ? new Identity(sql, commandType, connectionString, primaryType, parametersType, 0, gridIndex)
                : new IdentityWithTypes(sql, commandType, connectionString, primaryType, parametersType, otherTypes, gridIndex);

            /// <summary>
            /// 创建用于 DynamicParameters 的标识，仅供内部使用。
            /// </summary>
            /// <param name="type">要为其创建 <see cref="Identity"/> 的参数类型。</param>
            /// <returns></returns>
            public Identity ForDynamicParameters(Type type) =>
                new Identity(sql, commandType, connectionString, this.type, type, 0, -1);
#pragma warning restore CS0618 // Type or member is obsolete

            internal Identity(string sql, CommandType commandType, IDbConnection connection, Type type, Type parametersType)
                : this(sql, commandType, connection.ConnectionString, type, parametersType, 0, 0) { /* base call */ }

            private protected Identity(string sql, CommandType commandType, string connectionString, Type type, Type parametersType, int otherTypesHash, int gridIndex)
            {
#pragma warning disable CS0618 // Type or member is obsolete
                this.sql = sql;
                this.commandType = commandType;
                this.connectionString = connectionString;
                this.type = type;
                this.parametersType = parametersType;
                this.gridIndex = gridIndex;
                unchecked
                {
                    hashCode = 17; // we *know* we are using this in a dictionary, so pre-compute this
                    hashCode = (hashCode * 23) + commandType.GetHashCode();
                    hashCode = (hashCode * 23) + gridIndex.GetHashCode();
                    hashCode = (hashCode * 23) + (sql?.GetHashCode() ?? 0);
                    hashCode = (hashCode * 23) + (type?.GetHashCode() ?? 0);
                    hashCode = (hashCode * 23) + otherTypesHash;
                    hashCode = (hashCode * 23) + (connectionString is null ? 0 : connectionStringComparer.GetHashCode(connectionString));
                    hashCode = (hashCode * 23) + (parametersType?.GetHashCode() ?? 0);
                }
#pragma warning restore CS0618 // Type or member is obsolete
            }

            /// <summary>
            /// 判断此 <see cref="Identity"/> 是否与另一个相等。
            /// </summary>
            /// <param name="obj">要比较的另一个 <see cref="object"/>。</param>
            public override bool Equals(object obj) => Equals(obj as Identity);

            /// <summary>
            /// 原始 SQL 命令。
            /// </summary>
            [Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
            [Obsolete("Please use " + nameof(Sql) + ". This API may be removed at a later date.")]
            public readonly string sql;

            /// <summary>
            /// 原始 SQL 命令。
            /// </summary>
#pragma warning disable CS0618 // Type or member is obsolete
            public string Sql => sql;
#pragma warning restore CS0618 // Type or member is obsolete

            /// <summary>
            /// SQL 命令类型。
            /// </summary>
            [Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
            [Obsolete("Please use " + nameof(CommandType) + ". This API may be removed at a later date.")]
            public readonly CommandType commandType;

            /// <summary>
            /// SQL 命令类型。
            /// </summary>
#pragma warning disable CS0618 // Type or member is obsolete
            public CommandType CommandType => commandType;
#pragma warning restore CS0618 // Type or member is obsolete

            /// <summary>
            /// 此 Identity 的哈希码。
            /// </summary>
            [Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
            [Obsolete("Please use " + nameof(GetHashCode) + ". This API may be removed at a later date.")]
            public readonly int hashCode;

            /// <summary>
            /// 此 Identity 的网格索引（在读取器中的位置）。
            /// </summary>
            [Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
            [Obsolete("Please use " + nameof(GridIndex) + ". This API may be removed at a later date.")]
            public readonly int gridIndex;

            /// <summary>
            /// 此 Identity 的网格索引（在读取器中的位置）。
            /// </summary>
#pragma warning disable CS0618 // Type or member is obsolete
            public int GridIndex => gridIndex;
#pragma warning restore CS0618 // Type or member is obsolete

            /// <summary>
            /// 此 Identity 的 <see cref="Type"/>。
            /// </summary>
            [Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
            [Obsolete("Please use " + nameof(Type) + ". This API may be removed at a later date.")]
            public readonly Type type;

            /// <summary>
            /// 此 Identity 的 <see cref="Type"/>。
            /// </summary>
#pragma warning disable CS0618 // Type or member is obsolete
            public Type Type => type;
#pragma warning restore CS0618 // Type or member is obsolete

            /// <summary>
            /// 此 Identity 的连接字符串。
            /// </summary>
            [Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
            [Obsolete("This API may be removed at a later date.")]
            public readonly string connectionString;

            /// <summary>
            /// 此 Identity 的参数对象类型。
            /// </summary>
            [Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
            [Obsolete("Please use " + nameof(ParametersType) + ". This API may be removed at a later date.")]
            public readonly Type parametersType;

            /// <summary>
            /// 此 Identity 的参数对象类型。
            /// </summary>
#pragma warning disable CS0618 // Type or member is obsolete
            public Type ParametersType => parametersType;
#pragma warning restore CS0618 // Type or member is obsolete

            /// <summary>
            /// 获取此标识的哈希码。
            /// </summary>
            /// <returns></returns>
#pragma warning disable CS0618 // Type or member is obsolete
            public override int GetHashCode() => hashCode;
#pragma warning restore CS0618 // Type or member is obsolete

            /// <summary>
            /// 参见 object.ToString()
            /// </summary>
#pragma warning disable CS0618 // Type or member is obsolete
            public override string ToString() => sql;
#pragma warning restore CS0618 // Type or member is obsolete

            /// <summary>
            /// 比较两个 Identity 对象
            /// </summary>
            /// <param name="other">要比较的另一个 <see cref="Identity"/> 对象。</param>
            /// <returns>两者是否相等</returns>
            public bool Equals(Identity other)
            {
                if (ReferenceEquals(this, other)) return true;
                if (other is null) return false;

                int typeCount;
#pragma warning disable CS0618 // Type or member is obsolete
                return gridIndex == other.gridIndex
                    && type == other.type
                    && sql == other.sql
                    && commandType == other.commandType
                    && connectionStringComparer.Equals(connectionString, other.connectionString)
                    && parametersType == other.parametersType
                    && (typeCount = TypeCount) == other.TypeCount
                    && (typeCount == 0 || TypesEqual(this, other, typeCount));
#pragma warning restore CS0618 // Type or member is obsolete
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            private static bool TypesEqual(Identity x, Identity y, int count)
            {
                if (y.TypeCount != count) return false;
                for (int i = 0; i < count; i++)
                {
                    if (x.GetType(i) != y.GetType(i))
                        return false;
                }
                return true;
            }

            /// <summary>
            /// 应如何比较连接字符串的等价性？默认为 StringComparer.Ordinal。
            /// 提供自定义实现可用于允许具有相同架构的多租户数据库共享策略。
            /// 请注意，通常的等价规则适用：任何等价的连接字符串 <b>必须</b> 产生相同的哈希码。
            /// </summary>
            public static IEqualityComparer<string> ConnectionStringComparer
            {
                get { return connectionStringComparer; }
                set { connectionStringComparer = value ?? StringComparer.Ordinal; }
            }

            private static IEqualityComparer<string> connectionStringComparer = StringComparer.Ordinal;
        }
    
}
