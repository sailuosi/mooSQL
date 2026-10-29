using System;

namespace mooSQL.data
{
    /// <summary>
    /// DuckDB Provider 显式注册（模型 1：应用引用包后调用 UseDuckDB）。
    /// </summary>
    public static class DuckDBProviderExtensions
    {
        /// <summary>
        /// 向客户端构建器注册 DuckDB 方言（枚举 + 字符串双通道）。
        /// </summary>
        public static BaseClientBuilder UseDuckDB(this BaseClientBuilder builder)
        {
            if (builder == null) throw new ArgumentNullException(nameof(builder));
            return builder
                .useDialect(DataBaseType.DuckDB, () => new DuckDBDialect())
                .useDialect(DialectKeys.DuckDB, () => new DuckDBDialect());
        }

        /// <summary>
        /// 向方言工厂注册 DuckDB（便于测试与自定义工厂）。
        /// </summary>
        public static IDialectFactory UseDuckDB(this IDialectFactory factory)
        {
            if (factory == null) throw new ArgumentNullException(nameof(factory));
            factory.useDialect(DataBaseType.DuckDB, () => new DuckDBDialect());
            factory.useDialect(DialectKeys.DuckDB, () => new DuckDBDialect());
            return factory;
        }
    }
}
