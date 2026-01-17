using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data.model
{
    internal static class ToDebugExtension
    {


        internal static string ToDebugString(this ISQLNode element, SelectQueryClause? selectQuery = null)
        {
            try
            {
                //var writer = new QueryElementTextWriter(NullabilityContext.GetContext(selectQuery));
                //writer.AppendElement(element);
                //return writer.ToString();
                return element.ToString();
            }
            catch
            {
                return $"FAIL ToDebugString('{element.GetType().Name}').";
            }
        }
    }
}
