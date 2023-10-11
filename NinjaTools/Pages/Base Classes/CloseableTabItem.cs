using System;

namespace NinjaTools.Pages.BaseClasses
{
	public abstract class CloseableTabItem : Screen, IDisposable
	{
		public void Dispose()
		{
			Dispose(true);
		}

		protected virtual void Dispose(bool disposing)
		{
		}

		protected override void OnActivate()
		{
			base.OnActivate();
		}

		protected override void OnClose()
		{
			base.OnClose();
		}

		protected override void OnDeactivate()
		{
			base.OnDeactivate();
		}
	}
}