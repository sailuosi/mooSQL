using mooSQL.data.model;
using mooSQL.linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data
{
    /// <summary>
    /// MySQLClauseTranslator type.
    /// </summary>
    /// <summary>
    /// MySQLClauseTranslator type.
    /// </summary>
    public class MySQLClauseTranslator:ClauseTranslateVisitor
    {
        /// <summary>
        /// MySQLClauseTranslator type.
        /// </summary>

        public MySQLClauseTranslator(Dialect dia) : base(dia) { 
        
        }
/// <summary>
/// Returns the INSERT ON DUPLICATE KEY suffix.
/// </summary>
 

        protected override string? GetInsertOrUpdateDuplicateSuffix() => "ON DUPLICATE KEY UPDATE";
 /// <summary>
 /// Translates an object name into dialect-specific SQL.
 /// </summary>
 
        public override string TranslateObjectName(SqlObjectName name, ConvertType objectType, bool escape, TableOptions tableOptions, bool withoutSuffix = false)
        {
            var res = "";
            if (name.Database != null)
            {
                if (escape)
                {
                    res += TranslateValue(name.Database, ConvertType.NameToDatabase);
                }
                else
                {
                    res += name.Database;
                }
                res += ".";

                if (name.Schema == null)
                    res += ('.');
            }

            if (name.Package != null)
            {
                if (escape)
                {
                    res += TranslateValue(name.Package, ConvertType.NameToPackage);
                }
                else
                {
                    res += name.Package;
                }
                res += ".";

            }

            if (escape)
            {
                res += TranslateValue(name.Name, objectType);
            }
            else
            {
                res += name.Name;
            }

            return res;
        }
    }
}
