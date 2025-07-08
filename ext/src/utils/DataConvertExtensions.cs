
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace mooSQL.utils
{

    /// <summary>
    /// 数据库数据转换拓展
    /// </summary>
    public static class DataConvertExtensions
    {
        /// <summary>
        /// 将 DataTable 转 List 集合
        /// </summary>
        /// <typeparam name="T">返回值类型</typeparam>
        /// <param name="dataTable">DataTable</param>
        /// <returns>List{T}</returns>
        public static List<T> ToList<T>(this DataTable dataTable)
        {
            return dataTable.ToList(typeof(List<T>)) as List<T>;
        }
        /// <summary>
        /// 自定义循环操作，转换List,带判重、null判断。将略过为null或者重复的数据。
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="dataTable"></param>
        /// <param name="doBuild"></param>
        /// <returns></returns>
        public static List<T> ToList<T>(this DataTable dataTable,Func<DataRow,T> doBuild)
        {
            var res= new List<T>();
            foreach (DataRow row in dataTable.Rows) { 
                var t=doBuild(row);
                if (t != null && res.Contains(t)==false) { 
                    res.Add(t);
                }
            }
            return res; 
        }

        public static T ToEntity<T>(this DataRow row, Func<DataRow, T> doBuild)
        {
            var t = doBuild(row);
            return t;
        }

        /// <summary>
        /// 将 DataTable 转 List 集合
        /// </summary>
        /// <typeparam name="T">返回值类型</typeparam>
        /// <param name="dataTable">DataTable</param>
        /// <returns>List{T}</returns>
        public static async Task<List<T>> ToListAsync<T>(this DataTable dataTable)
        {
            var list = await dataTable.ToListAsync(typeof(List<T>));
            return list as List<T>;
        }

        /// <summary>
        /// 将 DataSet 转 元组
        /// </summary>
        /// <typeparam name="T1">元组元素类型</typeparam>
        /// <param name="dataSet">DataSet</param>
        /// <returns>元组类型</returns>
        public static List<T1> ToList<T1>(this DataSet dataSet)
        {
            var tuple = dataSet.ToList(typeof(List<T1>));
            return tuple[0] as List<T1>;
        }


        public static T ToEntity<T>(this DataRow row)
        {
            var tuple = row.ToEntity(typeof(T));
            return (T)tuple;
        }






        /// <summary>
        /// 将 DataSet 转 特定类型
        /// </summary>
        /// <param name="dataSet">DataSet</param>
        /// <param name="returnTypes">特定类型集合</param>
        /// <returns>List{object}</returns>
        public static List<object> ToList(this DataSet dataSet, params Type[] returnTypes)
        {
            if (returnTypes == null || returnTypes.Length == 0) return default;

            // 处理元组类型
            if (returnTypes.Length == 1 && returnTypes[0].IsValueType)
            {
                returnTypes = returnTypes[0].GenericTypeArguments;
            }

            // 获取所有的 DataTable
            var dataTables = dataSet.Tables;

            // 处理 8 个结果集
            if (returnTypes.Length >= 8)
            {
                return new List<object>
                {
                    dataTables[0].ToList(returnTypes[0]),
                    dataTables[1].ToList(returnTypes[1]),
                    dataTables[2].ToList(returnTypes[2]),
                    dataTables[3].ToList(returnTypes[3]),
                    dataTables[4].ToList(returnTypes[4]),
                    dataTables[5].ToList(returnTypes[5]),
                    dataTables[6].ToList(returnTypes[6]),
                    dataTables[7].ToList(returnTypes[7])
                };
            }
            // 处理 7 个结果集
            else if (returnTypes.Length == 7)
            {
                return new List<object>
                {
                    dataTables[0].ToList(returnTypes[0]),
                    dataTables[1].ToList(returnTypes[1]),
                    dataTables[2].ToList(returnTypes[2]),
                    dataTables[3].ToList(returnTypes[3]),
                    dataTables[4].ToList(returnTypes[4]),
                    dataTables[5].ToList(returnTypes[5]),
                    dataTables[6].ToList(returnTypes[6])
                };
            }
            // 处理 6 个结果集
            else if (returnTypes.Length == 6)
            {
                return new List<object>
                {
                    dataTables[0].ToList(returnTypes[0]),
                    dataTables[1].ToList(returnTypes[1]),
                    dataTables[2].ToList(returnTypes[2]),
                    dataTables[3].ToList(returnTypes[3]),
                    dataTables[4].ToList(returnTypes[4]),
                    dataTables[5].ToList(returnTypes[5])
                };
            }
            // 处理 5 个结果集
            else if (returnTypes.Length == 5)
            {
                return new List<object>
                {
                    dataTables[0].ToList(returnTypes[0]),
                    dataTables[1].ToList(returnTypes[1]),
                    dataTables[2].ToList(returnTypes[2]),
                    dataTables[3].ToList(returnTypes[3]),
                    dataTables[4].ToList(returnTypes[4])
                };
            }
            // 处理 4 个结果集
            else if (returnTypes.Length == 4)
            {
                return new List<object>
                {
                    dataTables[0].ToList(returnTypes[0]),
                    dataTables[1].ToList(returnTypes[1]),
                    dataTables[2].ToList(returnTypes[2]),
                    dataTables[3].ToList(returnTypes[3])
                };
            }
            // 处理 3 个结果集
            else if (returnTypes.Length == 3)
            {
                return new List<object>
                {
                    dataTables[0].ToList(returnTypes[0]),
                    dataTables[1].ToList(returnTypes[1]),
                    dataTables[2].ToList(returnTypes[2])
                };
            }
            // 处理 2 个结果集
            else if (returnTypes.Length == 2)
            {
                return new List<object>
                {
                    dataTables[0].ToList(returnTypes[0]),
                    dataTables[1].ToList(returnTypes[1])
                };
            }
            // 处理 1 个结果集
            else
            {
                return new List<object>
                {
                    dataTables[0].ToList(returnTypes[0])
                };
            }
        }

        /// <summary>
        /// 将 DataSet 转 特定类型
        /// </summary>
        /// <param name="dataSet">DataSet</param>
        /// <param name="returnTypes">特定类型集合</param>
        /// <returns>object</returns>
        public static Task<List<object>> ToListAsync(this DataSet dataSet, params Type[] returnTypes)
        {
            return Task.FromResult(dataSet.ToList(returnTypes));
        }

        /// <summary>
        /// 将 DataTable 转 特定类型
        /// </summary>
        /// <param name="dataTable">DataTable</param>
        /// <param name="returnType">返回值类型</param>
        /// <returns>object</returns>
        public static object ToList(this DataTable dataTable, Type returnType)
        {
            var isGenericType = returnType.IsGenericType;
            // 获取类型真实返回类型
            var underlyingType = isGenericType ? returnType.GenericTypeArguments.First() : returnType;

            var resultType = typeof(List<>).MakeGenericType(underlyingType);
            var list = Activator.CreateInstance(resultType);
            var addMethod = resultType.GetMethod("Add");

            // 将 DataTable 转为行集合
            var dataRows = dataTable.AsEnumerable();

            // 如果是基元类型
            if (underlyingType.IsRichPrimitive())
            {
                // 遍历所有行
                foreach (var dataRow in dataRows)
                {
                    // 只取第一列数据
                    var firstColumnValue = dataRow[0];
                    // 转换成目标类型数据
                    var destValue = firstColumnValue?.ChangeType(underlyingType);
                    // 添加到集合中
                    _ = addMethod.Invoke(list, new[] { destValue });
                }
            }
            // 处理Object类型
            else if (underlyingType == typeof(object))
            {
                // 获取所有列名
                var columns = dataTable.Columns;

                // 遍历所有行
                foreach (var dataRow in dataRows)
                {
                    var dic = new Dictionary<string, object>();
                    foreach (DataColumn column in columns)
                    {
                        dic.Add(column.ColumnName, dataRow[column]);
                    }
                    _ = addMethod.Invoke(list, new[] { dic });
                }
            }
            else
            {
                // 获取所有的数据列和类公开实例属性
                var dataColumns = dataTable.Columns;
                var properties = underlyingType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
                //.Where(p => !p.IsDefined(typeof(NotMappedAttribute), true));  // sql 数据转换无需判断 [NotMapperd] 特性

                // 遍历所有行
                foreach (var dataRow in dataRows)
                {
                    var model = Activator.CreateInstance(underlyingType);

                    // 遍历所有属性并一一赋值
                    foreach (var property in properties)
                    {
                        // 获取属性对应的真实列名
                        var columnName = property.Name;
                        if (property.IsDefined(typeof(System.ComponentModel.DataAnnotations.Schema.ColumnAttribute), true))
                        {
                            var columnAttribute = property.GetCustomAttribute<System.ComponentModel.DataAnnotations.Schema.ColumnAttribute>(true);
                            if (!string.IsNullOrWhiteSpace(columnAttribute.Name)) columnName = columnAttribute.Name;
                        }

                        // 如果 DataTable 不包含该列名，则跳过
                        if (!dataColumns.Contains(columnName)) continue;

                        // 获取列值
                        var columnValue = dataRow[columnName];
                        // 如果列值未空，则跳过
                        if (columnValue == DBNull.Value) continue;

                        // 转换成目标类型数据
                        var destValue = columnValue?.ChangeType(property.PropertyType);
                        property.SetValue(model, destValue);
                    }

                    // 添加到集合中
                    _ = addMethod.Invoke(list, new[] { model });
                }
            }

            return list;
        }
        public static object ToEntity(this DataRow row, Type returnType)
        {
            var isGenericType = returnType.IsGenericType;
            // 获取类型真实返回类型
            var underlyingType = isGenericType ? returnType.GenericTypeArguments.First() : returnType;

            var resultType = typeof(List<>).MakeGenericType(underlyingType);
            var list = Activator.CreateInstance(resultType);
            var addMethod = resultType.GetMethod("Add");

            // 将 DataTable 转为行集合


            // 如果是基元类型
            if (underlyingType.IsRichPrimitive())
            {
                // 遍历所有行

                // 只取第一列数据
                var firstColumnValue = row[0];
                // 转换成目标类型数据
                var destValue = firstColumnValue?.ChangeType(underlyingType);
                // 添加到集合中
                return destValue;
                
            }
            // 处理Object类型
            else if (underlyingType == typeof(object))
            {
                // 获取所有列名
                var columns = row.Table.Columns;

                // 遍历所有行

                var dic = new Dictionary<string, object>();
                foreach (DataColumn column in columns)
                {
                    dic.Add(column.ColumnName, row[column]);
                }
                return dic;
                
            }
            else
            {
                // 获取所有的数据列和类公开实例属性
                var dataColumns = row.Table.Columns;
                var properties = underlyingType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
                //.Where(p => !p.IsDefined(typeof(NotMappedAttribute), true));  // sql 数据转换无需判断 [NotMapperd] 特性

                // 遍历所有行

                var model = Activator.CreateInstance(underlyingType);

                // 遍历所有属性并一一赋值
                foreach (var property in properties)
                {
                    // 获取属性对应的真实列名
                    var columnName = property.Name;
                    if (property.IsDefined(typeof(System.ComponentModel.DataAnnotations.Schema.ColumnAttribute), true))
                    {
                        var columnAttribute = property.GetCustomAttribute<System.ComponentModel.DataAnnotations.Schema.ColumnAttribute>(true);
                        if (!string.IsNullOrWhiteSpace(columnAttribute.Name)) columnName = columnAttribute.Name;
                    }

                    // 如果 DataTable 不包含该列名，则跳过
                    if (!dataColumns.Contains(columnName)) continue;

                    // 获取列值
                    var columnValue = row[columnName];
                    // 如果列值未空，则跳过
                    if (columnValue == DBNull.Value) continue;

                    // 转换成目标类型数据
                    var destValue = columnValue?.ChangeType(property.PropertyType);
                    property.SetValue(model, destValue);
                }

                // 添加到集合中
                _ = addMethod.Invoke(list, new[] { model });
                return model;
            }


        }
        /// <summary>
        /// 将 DataTable 转 特定类型
        /// </summary>
        /// <param name="dataTable">DataTable</param>
        /// <param name="returnType">返回值类型</param>
        /// <returns>object</returns>
        public static Task<object> ToListAsync(this DataTable dataTable, Type returnType)
        {
            return Task.FromResult(dataTable.ToList(returnType));
        }

        /// <summary>
        /// 处理元组类型返回值
        /// </summary>
        /// <param name="dataSet">数据集</param>
        /// <param name="tupleType">返回值类型</param>
        /// <returns></returns>
        internal static object ToValueTuple(this DataSet dataSet, Type tupleType)
        {
            // 获取元组最底层类型
            var underlyingTypes = tupleType.GetGenericArguments().Select(u => u.IsGenericType ? u.GetGenericArguments().First() : u);

            var toListMethod = typeof(DataConvertExtensions)
                .GetMethods(BindingFlags.Public | BindingFlags.Static)
                .First(u => u.Name == "ToList" && u.IsGenericMethod && u.GetGenericArguments().Length == tupleType.GetGenericArguments().Length)
                .MakeGenericMethod(underlyingTypes.ToArray());

            return toListMethod.Invoke(null, new[] { dataSet });
        }
    }
}
