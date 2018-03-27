using System;
using System.Threading;
using System.Windows.Forms;
using NLog;

namespace IndexExercise.Index.Demo
{
	internal static class Program
	{
		[STAThread]
		public static void Main()
		{
			Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
			AppDomain.CurrentDomain.UnhandledException += unhandledException;
			Application.ThreadException += threadException;

			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);

			var form = new MainForm(() => new DemoApplication());
			Application.Run(form);
		}

		private static void threadException(object sender, ThreadExceptionEventArgs e)
		{
			_log.Error(e.Exception);
			LogManager.Flush();
		}

		private static void unhandledException(object sender, UnhandledExceptionEventArgs e)
		{
			_log.Error(e.ExceptionObject);
			LogManager.Flush();
		}

		private static readonly Logger _log = LogManager.GetCurrentClassLogger();
	}
}
