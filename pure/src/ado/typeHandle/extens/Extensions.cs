using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace mooSQL.data
{
    internal static class ExtensionsD
    {

        /// <summary>
        /// DataTable类型名
        /// </summary>
        private const string DataTableTypeNameKey = "moo:TypeName";


        /// <summary>
        /// 获取 <see cref="DataTable"/>.绑定的类型名
        /// </summary>
        /// <param name="table">The <see cref="DataTable"/> that has a type name associated with it.</param>
        public static string GetTypeName(this DataTable table) =>
            table?.ExtendedProperties[DataTableTypeNameKey] as string;



    }
}
