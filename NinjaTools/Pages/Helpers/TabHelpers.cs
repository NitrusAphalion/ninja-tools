using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Xml.Linq;

namespace NinjaTools.Pages.Helpers
{
	public static class TabHelpers
	{
		public static List<List<string>> CmdLineTabs { get; set; }

		public static string GetFile(string filter)
		{
			return OpenFile(filter, false)?[0];
		}

		public static string[] GetFiles(string filter)
		{
			return OpenFile(filter, true);
		}

		public static string[] GetFilesByFolder()
		{
			VistaFolderBrowserDialog openFolderDialog = new VistaFolderBrowserDialog();
			openFolderDialog.RootFolder = Environment.SpecialFolder.MyDocuments;

			if (openFolderDialog.ShowDialog().GetValueOrDefault())
				return Directory.GetFiles(openFolderDialog.SelectedPath);

			return null;
		}

		public static IScreen[] GetViewModelsByPath(string path, bool singlePathTypesSupported, bool loadNow)
		{
			string[] directories = Path.GetDirectoryName(path).Split('\\');
			string folder = directories[directories.Length - 1].ToLower();
			string filename = Path.GetFileName(path).ToLower();

			switch (Path.GetExtension(filename))
			{
				case ".txt":
					if (filename.StartsWith("log."))
						return new IScreen[] { new TxtDocumentViewModel(new Models.Document(path, typeof(string[]), DocumentType.Log), loadNow) };
					else if (filename.StartsWith("trace."))
						return new IScreen[] { new TxtDocumentViewModel(new Models.Document(path, typeof(string[]), DocumentType.Trace), loadNow) };
					else
					{
						switch (folder)
						{
							/*case "log":
								return new IScreen[] { new TxtDocumentViewModel(new Models.Document(path, typeof(string[]), DocumentType.Log), loadNow) };

							case "trace":
								return new IScreen[] { new TxtDocumentViewModel(new Models.Document(path, typeof(string[]), DocumentType.Trace), loadNow) };*/
							default:
								return new IScreen[] { new TxtDocumentViewModel(new Models.Document(path, typeof(string[]), DocumentType.UseType), loadNow) };
						}
					}
				case ".xml":
					switch (filename)
					{
						case "_workspaces.xml":
							if (singlePathTypesSupported)
							{
								XDocument _workspaces = XDocument.Load(path);
								IEnumerable<XElement> openWorkspaces = _workspaces.Descendants("OpenWorkspaces").Descendants();
								List<string> workspacePaths = new List<string>();

								foreach (XElement workspace in openWorkspaces)
								{
									string workspacePath = Path.Combine(Path.GetDirectoryName(path), $"{workspace.Value}.xml");
									if (File.Exists(workspacePath))
										workspacePaths.Add(workspacePath);
								}

								if (workspacePaths.Count > 0)
									return new IScreen[] { new DocumentContainerViewModel(workspacePaths.ToArray(), typeof(XDocument), DocumentType.Workspace) };
								else
									return null;
							}
							else
								return new IScreen[] { new XmlDocumentViewModel(new Models.Document(path, typeof(XDocument), DocumentType.Workspace), loadNow) };

						case "config.xml":
							return new IScreen[] { new XmlDocumentViewModel(new Models.Document(path, typeof(XDocument), DocumentType.Config), loadNow) };

						case "ui.xml":
							return new IScreen[] { new XmlDocumentViewModel(new Models.Document(path, typeof(XDocument), DocumentType.UI), loadNow) };

						default:
							switch (folder)
							{
								case "workspaces":
								case "recovery":
									return new IScreen[] { new XmlDocumentViewModel(new Models.Document(path, typeof(XDocument), DocumentType.Workspace), loadNow) };

								default:
									if (directories.ToList().Contains("templates"))
										return new IScreen[] { new XmlDocumentViewModel(new Models.Document(path, typeof(XDocument), DocumentType.Template), loadNow) };
									else
										return new IScreen[] { new XmlDocumentViewModel(new Models.Document(path, typeof(XDocument), DocumentType.UseType), loadNow) };
							}
					}
				case ".sdf":
					return new IScreen[] { new SdfDocumentViewModel(new Models.Document(path, typeof(Models.Database), DocumentType.Database), loadNow) };

				case ".zip":
					if (singlePathTypesSupported)
					{
						// Technically you could just extract this to memory but that kinks my design layout so not doing that
						string extractPath = Path.Combine(Path.GetTempPath(), "NinjaTools", Path.GetFileNameWithoutExtension(path));
						try
						{
							Directory.Delete(extractPath, true);
						}
						catch { }

						System.IO.Compression.ZipFile.ExtractToDirectory(path, extractPath);

						List<IScreen> toReturn = new List<IScreen>();
						string[] logs = Directory.GetFiles($"{extractPath}\\log");
						string[] traces = Directory.GetFiles($"{extractPath}\\trace");

						if (logs?.Length > 0)
							toReturn.Add(new DocumentContainerViewModel(logs, typeof(string[]), DocumentType.Log));

						if (traces?.Length > 0)
							toReturn.Add(new DocumentContainerViewModel(traces, typeof(string[]), DocumentType.Trace));

						if (toReturn.Count > 0)
							return toReturn.ToArray();
						else
							return null;
					}
					return null;

				default:
					return null;
			}
		}

		public static void LoadTab(AppViewModel vm, string[] paths)
		{
			if (paths?.Length == 0 || string.IsNullOrEmpty(paths?[0]))
				return;

			if (paths.Length == 1 && File.Exists(paths[0]))
			{
				IScreen[] viewModels = GetViewModelsByPath(paths[0], true, true);
				if (viewModels?.Length > 0)
				{
					foreach (IScreen viewModel in viewModels)
					{
						vm.Items.Add(viewModel);
						vm.ActiveItem = viewModel;
					}
				}
			}
			else
			{
				List<IScreen> viewModels = new List<IScreen>();
				foreach (string path in paths.Where(p => File.Exists(p)))
				{
					IScreen viewModel = GetViewModelsByPath(path, false, false)?[0];
					if (viewModel != null)
						viewModels.Add(viewModel);
				}

				if (viewModels.Count > 0)
				{
					DocumentContainerViewModel documentContainerViewModel = new DocumentContainerViewModel(paths, viewModels.ToArray());
					if (documentContainerViewModel != null)
						vm.ActiveItem = documentContainerViewModel;
				}
			}
		}

		private static string[] OpenFile(string filter, bool multiSelect)
		{
			OpenFileDialog openFileDialog = new OpenFileDialog();
			openFileDialog.Filter = filter;
			openFileDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
			openFileDialog.Multiselect = multiSelect;

			if (openFileDialog.ShowDialog() == DialogResult.OK)
				return openFileDialog.FileNames;

			return null;
		}
	}
}