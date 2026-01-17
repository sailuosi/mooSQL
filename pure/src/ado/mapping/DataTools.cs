using System;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace mooSQL.data.mapping
{


	public static class DataTools
	{

		static readonly char[] _escapes = { '\x0', '\'' };

		public static string ConvertStringToSql(
			
			string plusOperator,
			string? startPrefix,
			Action<StringBuilder,int> appendConversion,
			string value,
			char[]? extraEscapes)
		{
            StringBuilder stringBuilder= new StringBuilder();

            if (value.Length > 0
				&& (value.IndexOfAny(_escapes) >= 0 || (extraEscapes != null && value.IndexOfAny(extraEscapes) >= 0)))
			{
				var isInString = false;

				for (var i = 0; i < value.Length; i++)
				{
					var c = value[i];

					switch (c)
					{
						case '\x0' :
							if (isInString)
							{
								isInString = false;
								stringBuilder
									.Append('\'');
							}

							if (i != 0)
								stringBuilder
									.Append(' ')
									.Append(plusOperator)
									.Append(' ')
									;

							appendConversion(stringBuilder, c);

							break;

						case '\''  :
							if (!isInString)
							{
								isInString = true;

								if (i != 0)
									stringBuilder
										.Append(' ')
										.Append(plusOperator)
										.Append(' ')
										;

								stringBuilder.Append(startPrefix).Append('\'');
							}

							stringBuilder.Append("''");

							break;

						default   :
							if (extraEscapes != null && extraEscapes.Any(e => e == c))
							{
								if (isInString)
								{
									isInString = false;
									stringBuilder
										.Append('\'');
								}

								if (i != 0)
									stringBuilder
										.Append(' ')
										.Append(plusOperator)
										.Append(' ')
										;

								appendConversion(stringBuilder, c);
								break;
							}

							if (!isInString)
							{
								isInString = true;

								if (i != 0)
									stringBuilder
										.Append(' ')
										.Append(plusOperator)
										.Append(' ')
										;

								stringBuilder.Append(startPrefix).Append('\'');
							}

							stringBuilder.Append(c);

							break;
					}
				}

				if (isInString)
					stringBuilder.Append('\'');
			}
			else
			{
				stringBuilder
					.Append(startPrefix)
					.Append('\'')
					.Append(value)
					.Append('\'')
					;
			}
			return stringBuilder.ToString();
		}

		public static string ConvertCharToSql( string startString, Action<StringBuilder,int> appendConversion, char value)
		{
            StringBuilder stringBuilder = new StringBuilder();

            switch (value)
			{
				case '\x0' :
					appendConversion(stringBuilder,value);
					break;

				case '\''  :
					stringBuilder
						.Append(startString)
						.Append("''")
						.Append('\'')
						;
					break;

				default    :
					stringBuilder
						.Append(startString)
						.Append(value)
						.Append('\'')
						;
					break;
			}
			return stringBuilder.ToString();
		}

		public static Expression<Func<DbDataReader, int, string>> GetCharExpression = (dr, i) => GetCharFromString(dr.GetString(i));

		private static string GetCharFromString(string str)
		{
			if (str.Length > 0)
				return str[0].ToString();

			return string.Empty;
		}

		#region Create/Drop Database

		internal static void CreateFileDatabase(
			string databaseName,
			bool deleteIfExists,
			string extension,
			Action<string> createDatabase)
		{
			databaseName = databaseName.Trim();

			if (!databaseName.ToLowerInvariant().EndsWith(extension))
				databaseName += extension;

			if (File.Exists(databaseName))
			{
				if (!deleteIfExists)
					return;
				File.Delete(databaseName);
			}

			createDatabase(databaseName);
		}

		internal static void DropFileDatabase(string databaseName, string extension)
		{
			databaseName = databaseName.Trim();

			if (File.Exists(databaseName))
			{
				File.Delete(databaseName);
			}
			else
			{
				if (!databaseName.ToLowerInvariant().EndsWith(extension))
				{
					databaseName += extension;

					if (File.Exists(databaseName))
						File.Delete(databaseName);
				}
			}
		}
		#endregion
	}
}
