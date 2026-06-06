namespace mooSQL.linq;

/// <summary>Ext LINQ 函数族薄入口；链式 Extension API 见 <see cref="SooFunctionExtension"/>。</summary>
public static partial class SooFunc
{
	/// <summary>窗口 / 分析 / 字符串聚合等 Extension 链起点。</summary>
	public static SooFunctionExtension.ISqlExtension? Ext => SooFunctionExtension.Ext;
}
