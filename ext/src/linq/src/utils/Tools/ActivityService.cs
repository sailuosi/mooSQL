using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;



namespace mooSQL.linq.Tools
{
	/// <summary>
	/// Provides API to register factory methods that return an Activity object or <c>null</c> for provided <see cref="ActivityID"/> event.
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
				return activity.DisposeAsync().ConfigureAwait(Common.Configuration.ContinueOnCapturedContext);
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
		/// Adds a factory method that returns an Activity object or <c>null</c> for provided <see cref="ActivityID"/> event.
		/// </summary>
		/// <param name="factory">A factory method.</param>
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
						await activity.DisposeAsync().ConfigureAwait(Common.Configuration.ContinueOnCapturedContext);
			}
#endif

#pragma warning restore CA2215
		}
	}
}
