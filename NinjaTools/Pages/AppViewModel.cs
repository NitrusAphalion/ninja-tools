using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace NinjaTools.Pages
{
	public class AppViewModel : Conductor<IScreen>.Collection.OneActive
	{
		private bool isStartup = true;

		public AppViewModel() { }

		protected override void OnActivate()
		{
			if (Helpers.TabHelpers.CmdLineTabs != null)
			{
				foreach (List<string> tab in Helpers.TabHelpers.CmdLineTabs)
				{
					if (tab.Count == 1 && System.IO.Path.GetExtension(tab[0]) == string.Empty) // Support opening folder
					{
						Helpers.TabHelpers.LoadTab(this, System.IO.Directory.GetFiles(tab[0]));
						continue;
					}
					Helpers.TabHelpers.LoadTab(this, tab.ToArray());
				}
			}
			else if (isStartup)
				Helpers.Serialization.Deserialize(this);

			isStartup = false;

			base.OnActivate();
		}

		public override Task<bool> CanCloseAsync()
		{
			Helpers.Serialization.Serialize(this);
			return base.CanCloseAsync();
		}

		public void OnCloseTab(object sender, EventArgs e)
		{
			CloseItem((IScreen)(sender as Button).Tag);
		}

		public void Grid_WindowDrag(object sender, MouseButtonEventArgs e)
		{
			if (e.ChangedButton == MouseButton.Left)
				(View as Window).DragMove();
		}

		public void OnToolBarClick(object sender, RoutedEventArgs e)
		{
			switch ((sender as Button).Tag)
			{
				case "OpenFile":
				case "OpenFolder":
					string[] paths = (string)(sender as Button).Tag == "OpenFile"
						? Helpers.TabHelpers.GetFiles("NinjaTrader Files (*.txt, *.xml, *.sdf, *.zip)|*.txt;*.xml;*.sdf;*.zip")
						: Helpers.TabHelpers.GetFilesByFolder();

					Helpers.TabHelpers.LoadTab(this, paths);
					break;

				case "CloseAllTabs":
					foreach (IScreen tab in Items.ToList())
						CloseItem(tab);
					break;

				case "Options":
					break;

				case "Minimize":
					SystemCommands.MinimizeWindow((Window)View);
					break;

				case "Maximize":
					if ((View as Window).WindowState == WindowState.Normal)
						SystemCommands.MaximizeWindow(View as Window);
					else
						SystemCommands.RestoreWindow(View as Window);
					break;

				case "Exit":
					SystemCommands.CloseWindow((Window)View);
					break;
			}
		}

		public void SelectionChanged(object sender, RoutedEventArgs e)
		{
			if (View == null)
				return;

			TabControl tc = (View as AppView).tabControl;

			if (tc.SelectedItem == null)
				tc.BorderThickness = new Thickness(0);
			else
				tc.BorderThickness = new Thickness(0, 1, 0, 0);
		}
	}
}