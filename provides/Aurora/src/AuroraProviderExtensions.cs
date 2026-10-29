using System;

namespace mooSQL.data
{
    /// <summary>
    /// Aurora Provider 显式注册。
    /// </summary>
    public static class AuroraProviderExtensions
    {
        /// <summary>注册 Aurora MySQL 方言。</summary>
        public static BaseClientBuilder UseAuroraMySQL(this BaseClientBuilder builder)
        {
            if (builder == null) throw new ArgumentNullException(nameof(builder));
            return builder
                .useDialect(DataBaseType.AuroraMySQL, () => new AuroraMySQLDialect())
                .useDialect(DialectKeys.AuroraMySQL, () => new AuroraMySQLDialect())
                .useDialect("aurora-mysql", () => new AuroraMySQLDialect());
        }

        /// <summary>注册 Aurora PostgreSQL 方言。</summary>
        public static BaseClientBuilder UseAuroraPostgreSQL(this BaseClientBuilder builder)
        {
            if (builder == null) throw new ArgumentNullException(nameof(builder));
            return builder
                .useDialect(DataBaseType.AuroraPostgreSQL, () => new AuroraPostgreSQLDialect())
                .useDialect(DialectKeys.AuroraPostgreSQL, () => new AuroraPostgreSQLDialect())
                .useDialect("aurora-postgresql", () => new AuroraPostgreSQLDialect())
                .useDialect("AuroraPG", () => new AuroraPostgreSQLDialect());
        }

        /// <summary>同时注册 Aurora MySQL 与 PostgreSQL。</summary>
        public static BaseClientBuilder UseAurora(this BaseClientBuilder builder)
            => builder.UseAuroraMySQL().UseAuroraPostgreSQL();

        /// <summary>向工厂注册 Aurora MySQL。</summary>
        public static IDialectFactory UseAuroraMySQL(this IDialectFactory factory)
        {
            if (factory == null) throw new ArgumentNullException(nameof(factory));
            factory.useDialect(DataBaseType.AuroraMySQL, () => new AuroraMySQLDialect());
            factory.useDialect(DialectKeys.AuroraMySQL, () => new AuroraMySQLDialect());
            factory.useDialect("aurora-mysql", () => new AuroraMySQLDialect());
            return factory;
        }

        /// <summary>向工厂注册 Aurora PostgreSQL。</summary>
        public static IDialectFactory UseAuroraPostgreSQL(this IDialectFactory factory)
        {
            if (factory == null) throw new ArgumentNullException(nameof(factory));
            factory.useDialect(DataBaseType.AuroraPostgreSQL, () => new AuroraPostgreSQLDialect());
            factory.useDialect(DialectKeys.AuroraPostgreSQL, () => new AuroraPostgreSQLDialect());
            factory.useDialect("aurora-postgresql", () => new AuroraPostgreSQLDialect());
            factory.useDialect("AuroraPG", () => new AuroraPostgreSQLDialect());
            return factory;
        }

        /// <summary>向工厂同时注册两种 Aurora。</summary>
        public static IDialectFactory UseAurora(this IDialectFactory factory)
            => factory.UseAuroraMySQL().UseAuroraPostgreSQL();
    }
}
