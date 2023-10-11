using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace NinjaTools.Pages
{
	public class DatabaseItem
	{
		public string IconPath => Level == 0 ? "pack://application:,,,/Icons/MainMenu/file_database.png" : "pack://application:,,,/Icons/MainMenu/table.png";
		public bool IsExpanded { get; set; }
		public int Level { get; set; }
		public string Name { get; set; }
		public List<DatabaseItem> SubItems { get; set; }

		public DatabaseItem(int level, string name)
		{
			IsExpanded = true;
			Level = level;
			Name = name;
			SubItems = new List<DatabaseItem>();
		}
	}

	public class SdfDocumentViewModel : BaseClasses.CloseableTabItem, Interfaces.ICloseableTabItem, Interfaces.IDelayedLoading, Interfaces.IStatusBarText, Interfaces.IDocumentType
	{
		public DocumentType DocumentType => model.DocumentType;
		public bool Loaded => model.Loaded;
		public string Path => model.Path;
		public string StatusBarText => $"{model.Path} ({Math.Round(model.Size, 2)} KB)";
		public string TabHeader => System.IO.Path.GetFileName(model.Path);
		public BindableCollection<DatabaseItem> TreeViewPaths { get; set; }

		private Models.Database database => model?.File as Models.Database;
		private Models.Document model = null;

		public SdfDocumentViewModel(Models.Document _model, bool loadNow)
		{
			model = _model;
			if (loadNow)
				Load();
		}

		public object Clone()
		{
			return new SdfDocumentViewModel(new Models.Document(model.Path, typeof(Models.Database), DocumentType.Database), Loaded);
		}

		public void Load()
		{
			model.Load();
			SetupTreeViewPaths();
		}

		public void TreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<Object> e)
		{
			if (database.Tables.Contains((e.NewValue as DatabaseItem).Name))
			{
				database.QueryTable((e.NewValue as DatabaseItem).Name);
				(View as SdfDocumentView).dataGrid.DataContext = database.DisplayData.Tables[0];
			}
		}

		protected override void OnViewLoaded()
		{
			if (Loaded)
			{
				database.QueryTable(TreeViewPaths.First().SubItems.First().Name);
				(View as SdfDocumentView).dataGrid.DataContext = database.DisplayData.Tables[0];
			}
			base.OnViewLoaded();
		}
		private void SetupTreeViewPaths()
		{
			TreeViewPaths = new BindableCollection<DatabaseItem>();
			DatabaseItem root = new DatabaseItem(0, model.Path);

			foreach (string table in database.Tables)
				root.SubItems.Add(new DatabaseItem(1, table));

			TreeViewPaths.Add(root);
		}
	}
}