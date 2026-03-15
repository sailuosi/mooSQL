using System;

namespace mooSQL.linq.Extensions
{
	/// <summary>
	/// Contains data manipulation helpers (e.g. for use in query parameters).
	/// </summary>
	static class ValueExtensions
	{
		internal static readonly int[] TICKS_DIVIDERS =
		new int[] {
			10000000,
			1000000,
			100000,
			10000,
			1000,
			100,
			10,
			1
		};


		public static DateTimeOffset WithPrecision(this DateTimeOffset dto, int precision)
		{
			if (precision >= 7)
				return dto;

			if (precision < 0)
				throw new InvalidOperationException($"Precision must be >= 0: {precision}");

			var delta = dto.Ticks % TICKS_DIVIDERS[precision];
			return delta == 0 ? dto : dto.AddTicks(-delta);
		}

		public static DateTime WithPrecision(this DateTime dt, int precision)
		{
			if (precision >= 7)
				return dt;

			if (precision < 0)
				throw new InvalidOperationException($"Precision must be >= 0: {precision}");

			var delta = dt.Ticks % TICKS_DIVIDERS[precision];
			return delta == 0 ? dt : dt.AddTicks(-delta);
		}
	}
}
