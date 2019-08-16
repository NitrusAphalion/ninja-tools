using Stylet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Xml.Linq;

namespace NinjaTools.Pages
{
    public class DocumentContainerViewModel : Conductor<IScreen>.Collection.OneActive, IScreen, Interfaces.ICloseableTabItem, Interfaces.IStatusBarText
    {
        public string                                   Path                        => (ActiveItem as Interfaces.IStatusBarText).Path;
        public string                                   StatusBarText               => (ActiveItem as Interfaces.IStatusBarText).StatusBarText;
        public string                                   TabHeader                   => $"[{Items.Count}] {(ActiveItem as Interfaces.ICloseableTabItem).TabHeader}";
        public BindableCollection<DocumentTreeViewItem> TreeViewPaths { get; set; }

        private DocumentType    documentType;
        private string[]        paths;
        private Type            type;

        public DocumentContainerViewModel(string[] _paths, IScreen[] viewModels)
        {
            paths = _paths;

            foreach (IScreen viewModel in viewModels)
                Items.Add(viewModel);

            SetupTreeViewPaths(paths);
            (Items[0] as Interfaces.IDelayedLoading).Load();
            ActiveItem = Items[0];
        }

        public DocumentContainerViewModel(string[] paths, Type type, DocumentType documentType)
        {
            this.paths          = paths;
            this.type           = type;
            this.documentType   = documentType;

            LoadDocuments(paths, type, documentType);
            SetupTreeViewPaths(paths);
            (Items[0] as Interfaces.IDelayedLoading).Load();
            ActiveItem = Items[0];
        }

        public object Clone()
        {
            if (type != null)
                return new DocumentContainerViewModel(paths, type, documentType);
            else
            {
                List<IScreen> viewModels = new List<IScreen>();
                foreach (string path in paths)
                {
                    IScreen viewModel = Helpers.TabHelpers.GetViewModelsByPath(path, false, false)?[0];
                    if (viewModel != null)
                        viewModels.Add(viewModel);
                };

                if (viewModels.Count > 0)
                    return new DocumentContainerViewModel(paths, viewModels.ToArray());
                return null;
            }
        }

        public void LoadDocuments(string[] paths, Type type, DocumentType documentType)
        {
            if (type == typeof(string[]))
                foreach (string path in paths)
                    Items.Add(new TxtDocumentViewModel(new Models.Document(path, type, documentType), false));
            else if (type == typeof(XDocument))
                foreach (string path in paths)
                    Items.Add(new XmlDocumentViewModel(new Models.Document(path, type, documentType), false));
            else if (type == typeof(Models.Database))
                foreach (string path in paths)
                    Items.Add(new SdfDocumentViewModel(new Models.Document(path, type, documentType), false));
            else
                throw new Exception($"Wrong type: {type.ToString()}");
        }

        public void TreeView_ContextMenuClicked(object sender, RoutedEventArgs e)
        {
            MenuItem menuItem = sender as MenuItem;
            switch (menuItem.Header)
            {
                case "Open in new tab":
                    (Parent as Conductor<IScreen>.Collection.OneActive).Items.Add(((menuItem.DataContext as DocumentTreeViewItem).ViewModel as Interfaces.ICloseableTabItem).Clone() as IScreen);
                    break;

                case "Open in new instance":
                    string paths = string.Empty;

                    if ((menuItem.DataContext as DocumentTreeViewItem).Items?.Count > 0)
                    {
                        foreach (DocumentTreeViewItem sub in (menuItem.DataContext as DocumentTreeViewItem).Items)
                            if (sub.ViewModel != null)
                                paths += $"{(sub.ViewModel as Interfaces.IStatusBarText).Path};";
                    }
                    else
                        paths = ((menuItem.DataContext as DocumentTreeViewItem).ViewModel as Interfaces.IStatusBarText).Path;

                    System.Diagnostics.Process.Start(System.Reflection.Assembly.GetExecutingAssembly().GetModules()[0].FullyQualifiedName, $"-p=\"{paths}\"");
                    break;

                case "Copy text":
                    Clipboard.SetText(((menuItem.DataContext as DocumentTreeViewItem).ViewModel as Interfaces.ICloseableTabItem).TabHeader);
                    break;

                case "Copy path":
                    Clipboard.SetText(((menuItem.DataContext as DocumentTreeViewItem).ViewModel as Interfaces.IStatusBarText).Path);
                    break;

                case "Close file":
                    DocumentTreeViewItem folder = menuItem.DataContext as DocumentTreeViewItem;
                    CloseItem(folder.ViewModel);
                    if (folder.Parent != null)
                        folder.Parent.Items.Remove(folder);
                    else
                        TreeViewPaths.Remove(folder);
                    (View as DocumentContainerView).documentTreeView.Items.Refresh();
                    break;
            }
        }

        public void TreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<Object> e)
        {
            if ((e.NewValue as DocumentTreeViewItem)?.Parent != null) // Root holds ViewModel for duplicating, but don't pull it on selection or will StackOverflow
            {
                IScreen selectedView = (e.NewValue as DocumentTreeViewItem)?.ViewModel;
                if (selectedView != null)
                {
                    (selectedView as Interfaces.IDelayedLoading).Load();
                    ActiveItem = selectedView;
                    Refresh();
                }
            }
        }

        private void SetupTreeViewPaths(string[] paths)
        {
            TreeViewPaths = new BindableCollection<DocumentTreeViewItem>();
            foreach (string path in paths)
            {
                string      fileName    = System.IO.Path.GetFileName(path);
                string[]    parts       = new string[] { path.Substring(0, path.Length - fileName.Length), fileName };

                if (parts.Any())
                {
                    DocumentTreeViewItem root = TreeViewPaths.FirstOrDefault(folder => folder.Name.Equals(parts[0]) && folder.Level.Equals(1));
                    if (root == null)
                    {
                        root = new DocumentTreeViewItem(1, parts[0]) { ViewModel = this };
                        TreeViewPaths.Add(root);
                    }

                    if (parts.Length > 1)
                    {
                        DocumentTreeViewItem parent = root;
                        int level = 2;
                        for (int i = 1; i < parts.Length; ++i)
                        {
                            DocumentTreeViewItem subItem = parent.Items.FirstOrDefault(folder => folder.Name.Equals(parts[i]) && folder.Level.Equals(level));
                            if (subItem == null)
                            {
                                subItem = new DocumentTreeViewItem(level, parts[i]) { ViewModel = Items.FirstOrDefault(vm => (vm as Interfaces.IStatusBarText).Path == path), Parent = parent };
                                parent.Items.Add(subItem);
                            }
                            parent = subItem;
                            level++;
                        }
                    }
                }
            }
        }
    }

    public class DocumentTreeViewItem
    {
        public string                       IconPath
        {
            get
            {
                if ((ViewModel as Interfaces.IDocumentType)?.DocumentType == DocumentType.UseType)
                    return "pack://application:,,,/Icons/MainMenu/help.png";

                switch (System.IO.Path.GetExtension(Name).ToLower())
                {
                    case null:
                    case "":
                        return "pack://application:,,,/Icons/Tab/folder.png";

                    case ".txt":
                        return "pack://application:,,,/Icons/MainMenu/file_text.png";

                    case ".xml":
                        return "pack://application:,,,/Icons/MainMenu/file_xml.png";

                    case ".sdf":
                        return "pack://application:,,,/Icons/MainMenu/file_database.png";

                    default:
                        return "pack://application:,,,/Icons/MainMenu/application_error.png";
                }
            }
        }

        public bool                         IsExpanded  { get; set; }
        public List<DocumentTreeViewItem>   Items       { get; set; }
        public int                          Level       { get; set; }
        public string                       Name        { get; set; }
        public DocumentTreeViewItem         Parent      { get; set; }
        public IScreen                      ViewModel   { get; set; }

        public DocumentTreeViewItem(int level, string name)
        {
            IsExpanded  = true;
            Level       = level;
            Name        = name;
            Items       = new List<DocumentTreeViewItem>();
        }
    }
}