using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;



namespace mooSQL.linq.utils
{
	/// <summary>
	/// Ext LINQ 诊断活动钩子：调用点通过 <see cref="Start"/> 埋点；未注册工厂时恒返回 <c>null</c>（no-op）。
	/// 需要诊断时调用 <see cref="AddFactory"/> 注册实现（可多次叠加）。
	/// </summary>
	public static class ActivityService
	{
		internal static Func<ActivityID,IActivity?> Start { [MethodImpl(MethodImplOptions.AggressiveInlining)] get; private set; } = static _ => null;

		static IActivity? StartImpl(ActivityID activityID)
		{
			if (_factory == null)
				throw new InvalidOperationException();

			var factory = _factory;

			var list = factory.GetInvocationList();

			if (list.Length == 1)
				return factory(activityID);

			var activities = new IActivity?[list.Length];

			for (var i = 0; i < list.Length; i++)
				activities[i] = ((Func<ActivityID, IActivity>)list[i])(activityID);

			return new MultiActivity(activities);
		}

		internal sealed class AsyncDisposableWrapper
		{
			public IActivity activity;

            public AsyncDisposableWrapper(IActivity activity) { 
				this.activity = activity;
			}
#if NET5_0_OR_GREATER
            public ConfiguredValueTaskAwaitable DisposeAsync()
			{
				return activity.DisposeAsync().ConfigureAwait(mooSQL.linq.ExtLinqOptions.ContinueOnCapturedContext);
			}
#endif

		}

		internal static AsyncDisposableWrapper? StartAndConfigureAwait(ActivityID activityID)
		{
			var activity = Start(activityID);

			if (activity is null)
				return null;

			return new AsyncDisposableWrapper(activity);
		}

		static Func<ActivityID,IActivity?>? _factory;

		/// <summary>
		/// 注册诊断 Activity 工厂。未调用时 <see cref="Start"/> 恒为 no-op；可多次 <c>+=</c> 叠加多个工厂。
		/// 本仓库业务与测试默认不注册；保留供外部诊断 / 性能分析扩展。
		/// </summary>
		/// <param name="factory">返回 <see cref="IActivity"/> 或 <c>null</c> 的工厂。</param>
		public static void AddFactory(Func<ActivityID,IActivity?> factory)
		{
			if (_factory == null)
			{
				_factory += factory;
				Start    = static id => _factory(id);
			}
			else
			{
				_factory += factory;
				Start    = StartImpl;
			}
		}

		sealed class MultiActivity : ActivityBase
		{


			public IActivity?[] activities;

			public MultiActivity(IActivity?[] activities) { 
				this.activities = activities;
			}

            public override void Dispose()
			{
				foreach (var activity in activities)
					activity?.Dispose();
			}

#pragma warning disable CA2215
#if NET5_0_OR_GREATER
            public override async ValueTask DisposeAsync()
			{
				foreach (var activity in activities)
					if (activity is not null)
						await activity.DisposeAsync().ConfigureAwait(mooSQL.linq.ExtLinqOptions.ContinueOnCapturedContext);
			}
#endif

#pragma warning restore CA2215
		}
	}
}
