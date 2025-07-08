using System.Data;

namespace mooSQL.data
{
    /// <summary>
    /// Used to pass a DataTable as a TableValuedParameter
    /// </summary>
    internal sealed class TableValuedParameter : ICustomQueryParameter
    {


        internal static void Set(IDbDataParameter parameter, DataTable table, string typeName)
        {
#pragma warning disable 0618
            parameter.Value = ParameterHandler.SanitizeParameterValue(table);
#pragma warning restore 0618
            if (string.IsNullOrEmpty(typeName) && table != null)
            {
                typeName = table.GetTypeName();
            }
            if (!string.IsNullOrEmpty(typeName)) StructuredHelper.ConfigureTVP(parameter, typeName);
        }
    }
}
