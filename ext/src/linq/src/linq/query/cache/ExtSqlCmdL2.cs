using mooSQL.data;
using mooSQL.data.model;
using System;
using System.Collections;
using System.Collections.Generic;

namespace mooSQL.linq.Linq
{
	/// <summary>
	/// L2：在 L1 <see cref="SentenceBag"/> 之上缓存 SQLCmd 文本模板。
	/// 安全门（首期）：全部参数非 null，且无 List/Enumerable（string/byte[] 除外）。
	/// </summary>
	internal static class ExtSqlCmdL2
	{
		/// <summary>安全门：可复用同一 SQL 文本、仅改 para。</summary>
		public static bool IsSafeGate(IReadOnlyList<ParameterAccessor> accessors, SqlParameterValues values)
		{
			if (accessors == null || accessors.Count == 0)
				return true;

			for (var i = 0; i < accessors.Count; i++)
			{
				var word = accessors[i].SqlParameter;
				if (!values.TryGetValue(word, out var pv) || pv == null)
					return false;

				if (!IsScalarNonNull(pv.ProviderValue))
					return false;
			}

			return true;
		}

		public static bool IsScalarNonNull(object? value)
		{
			if (value == null)
				return false;

			// string / byte[] 实现了 IEnumerable，但仍按标量参数处理
			if (value is string or byte[])
				return true;

			if (value is IEnumerable)
				return false;

			return true;
		}

		public static bool TryBuild(
			SentenceItem sentence,
			SqlParameterValues values,
			out SQLCmd? cmd)
		{
			cmd = null;
			var template = sentence.L2Template;
			if (template == null)
				return false;

			// 历史错误缓存：含 Live 壳的模板不可复用（会永久丢 DelayParas）
			if (template.Sql != null && template.Sql.IndexOf("moo.lp:", StringComparison.Ordinal) >= 0)
			{
				sentence.L2Template = null;
				return false;
			}

			// Skip/Take 烘焙进 OFFSET/LIMIT 的查询不可复用旧文本
			if (HasParameterizedPaging(sentence))
			{
				sentence.L2Template = null;
				return false;
			}

			if (!IsSafeGate(sentence.ParameterAccessors, values))
				return false;

			var paras = new Paras();
			for (var i = 0; i < sentence.ParameterAccessors.Count; i++)
			{
				var accessor = sentence.ParameterAccessors[i];
				if (!values.TryGetValue(accessor.SqlParameter, out var pv) || pv == null)
					return false;

				var key = i < template.ParaKeys.Length
					? template.ParaKeys[i]
					: NormalizeKey(accessor.SqlParameter.Name);

				var p = new Parameter(key, pv.ProviderValue)
				{
					dbType = pv.DbDataType
				};
				paras.Add(p);
			}

			cmd = new SQLCmd(template.Sql, paras)
			{
				type = template.Type,
				TargetTable = template.TargetTable ?? ""
			};
			return true;
		}

		public static void TryCapture(
			SentenceItem sentence,
			SQLCmd cmd,
			SqlParameterValues values)
		{
			if (cmd == null || string.IsNullOrEmpty(cmd.sql))
				return;

			// Live/Delay 占位未解析时不可缓存：复用只会带 Static para，DelayParas 会丢失，SQL 永久留 @@{{moo.lp:N}}。
			if (cmd.sql.IndexOf("moo.lp:", StringComparison.Ordinal) >= 0)
				return;

			// Skip/Take 常被 Visit 烘焙进 OFFSET/LIMIT 字面量；同 L1 不同分页值不能共用文本。
			if (HasParameterizedPaging(sentence))
				return;

			if (!IsSafeGate(sentence.ParameterAccessors, values))
				return;

			var accessors = sentence.ParameterAccessors;
			var keys = new string[accessors.Count];
			for (var i = 0; i < accessors.Count; i++)
			{
				var name = NormalizeKey(accessors[i].SqlParameter.Name);
				keys[i] = ResolveParaKey(cmd.para, name) ?? name;
			}

			sentence.L2Template = new ExtSqlCmdTemplate(
				cmd.sql,
				keys,
				cmd.type,
				cmd.TargetTable);
		}

		static bool HasParameterizedPaging(SentenceItem sentence)
		{
			var select = sentence.Statement?.SelectQuery?.Select;
			if (select == null)
				return false;
			return IsPagingParameter(select.SkipValue) || IsPagingParameter(select.TakeValue);
		}

		static bool IsPagingParameter(IExpWord? word)
			=> word is ParameterWord;

		public static SentenceCmds? TryBuildCmds(SentenceItem sentence, SqlParameterValues values)
		{
			if (!TryBuild(sentence, values, out var cmd) || cmd == null)
				return null;

			var cmds = new SentenceCmds { Sql = sentence.Statement };
			cmds.Add(cmd);
			return cmds;
		}

		public static void TryCaptureCmds(SentenceItem sentence, SentenceCmds cmds, SqlParameterValues values)
		{
			if (cmds?.cmds == null || cmds.cmds.Count != 1)
				return;

			TryCapture(sentence, cmds.cmds[0], values);
		}

		static string NormalizeKey(string? name)
		{
			if (string.IsNullOrEmpty(name))
				return "p";
			return name.TrimStart('@', ':', '?');
		}

		static string? ResolveParaKey(Paras? paras, string normalizedName)
		{
			if (paras?.value == null || paras.value.Count == 0)
				return null;

			foreach (var kv in paras.value)
			{
				var k = kv.Key;
				if (string.Equals(k, normalizedName, StringComparison.OrdinalIgnoreCase))
					return k;
				var stripped = NormalizeKey(k);
				if (string.Equals(stripped, normalizedName, StringComparison.OrdinalIgnoreCase))
					return k;
			}

			// 单参数时直接取唯一键
			if (paras.value.Count == 1)
			{
				foreach (var kv in paras.value)
					return kv.Key;
			}

			return null;
		}
	}

	/// <summary>挂在 <see cref="SentenceItem"/> 上的 SQL 文本模板（随 L1 bag 复用）。</summary>
	internal sealed class ExtSqlCmdTemplate
	{
		public ExtSqlCmdTemplate(string sql, string[] paraKeys, QueryType type, string? targetTable)
		{
			Sql = sql;
			ParaKeys = paraKeys ?? new string[0];
			Type = type;
			TargetTable = targetTable;
		}

		public string Sql { get; }
		public string[] ParaKeys { get; }
		public QueryType Type { get; }
		public string? TargetTable { get; }
	}
}
