#if NET6_0_OR_GREATER
using mooSQL.data.model;
using mooSQL.linq;

namespace mooSQL.data
{
    public class DuckDBClauseTranslator : ClauseTranslateVisitor
    {
        public DuckDBClauseTranslator(Dialect dia) : base(dia) { }

        public override string TranslateObjectName(SqlObjectName name, ConvertType objectType, bool escape, TableOptions tableOptions, bool withoutSuffix = false)
        {
            var res = "";
            if (name.Database != null)
            {
                if (escape)
                    res += TranslateValue(name.Database, ConvertType.NameToDatabase);
                else
                    res += name.Database;
                res += ".";

                if (name.Schema == null)
                    res += ".";
            }

            if (name.Schema != null)
            {
                if (escape)
                    res += TranslateValue(name.Schema, ConvertType.NameToSchema);
                else
                    res += name.Schema;
                res += ".";
            }

            if (name.Package != null)
            {
                if (escape)
                    res += TranslateValue(name.Package, ConvertType.NameToPackage);
                else
                    res += name.Package;
                res += ".";
            }

            if (escape)
                res += TranslateValue(name.Name, objectType);
            else
                res += name.Name;

            return res;
        }
    }
}
#endif
