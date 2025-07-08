
using mooSQL.data.builder;

using System.Text;
using System.Text.RegularExpressions;


namespace mooSQL.data
{
    public partial class Dialect
    {

        public string BuildInsertOne(FragSQL frag) { 
            var sql= new StringBuilder();
            //INSERT INTO table_name VALUES (value1,value2,value3,...);
            sql.Append("INSERT INTO ");
            sql.Append(frag.insertInto+ " VALUES ");
            if (Regex.IsMatch(frag.insertValue,@"\s*[(].*[)]\s*")) {
                //包含开始
                sql.Append(frag.insertValue);
            }
            return sql.ToString();
        }
    }
}
