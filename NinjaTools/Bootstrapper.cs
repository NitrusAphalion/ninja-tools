using NinjaTools.Pages;
using System.Windows;
using System.Windows.Threading;

namespace NinjaTools
{
	public class AppBootstrapper : Bootstrapper<AppViewModel>
	{
		protected override void OnUnhandledException(DispatcherUnhandledExceptionEventArgs e)
		{
			MessageBox.Show(e.Exception.ToString());
		}
	}
}