using IndexExercise.Index.FileSystem;

namespace IndexExercise.Index.Test
{
	public abstract class WatchUtilityBase : FileSystemUtility
	{
		protected WatchUtilityBase()
		{
			Watcher = new Watcher();
			Watcher.ChangeDetected += (sender, change) => Log.Debug(change.ToString);
			Watcher.Error += (sender, args) => Log.Debug(args.Exception, $"Failed watching {args.Target}");
		}

		public override void Dispose()
		{
			Watcher.Dispose();
			base.Dispose();
		}

		public Watcher Watcher { get; }
	}
}