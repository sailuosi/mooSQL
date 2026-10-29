using System;

namespace mooSQL.data
{
    /// <summary>
    /// CockroachDB Provider 显式注册。
    /// </summary>
    public static class CockroachDBProviderExtensions
    {
        /// <summary>注册 CockroachDB 方言。</summary>
        public static BaseClientBuilder UseCockroachDB(this BaseClientBuilder builder)
        {
            if (builder == null) throw new ArgumentNullException(nameof(builder));
            return builder
                .useDialect(DataBaseType.CockroachDB, () => new CockroachDBDialect())
                .useDialect(DialectKeys.CockroachDB, () => new CockroachDBDialect())
                .useDialect("cockroach", () => new CockroachDBDialect())
                .useDialect("CRDB", () => new CockroachDBDialect());
        }

        /// <summary>向工厂注册 CockroachDB。</summary>
        public static IDialectFactory UseCockroachDB(this IDialectFactory factory)
        {
            if (factory == null) throw new ArgumentNullException(nameof(factory));
            factory.useDialect(DataBaseType.CockroachDB, () => new CockroachDBDialect());
            factory.useDialect(DialectKeys.CockroachDB, () => new CockroachDBDialect());
            factory.useDialect("cockroach", () => new CockroachDBDialect());
            factory.useDialect("CRDB", () => new CockroachDBDialect());
            return factory;
        }
    }
}
