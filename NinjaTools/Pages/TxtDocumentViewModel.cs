using System;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Xml;

namespace NinjaTools.Pages
{
	public class TxtDocumentViewModel : BaseClasses.CloseableTabItem, Interfaces.ICloseableTabItem, Interfaces.IDelayedLoading, Interfaces.IStatusBarText, Interfaces.IDocumentType
	{
		public DocumentType DocumentType => model.DocumentType;
		public IHighlightingDefinition EditorSyntax
		{
			get
			{
				if (File.Exists(model.SyntaxFile))
				{
					using (Stream stream = File.OpenRead(model.SyntaxFile))
					using (XmlTextReader reader = new XmlTextReader(stream))
					{ return ICSharpCode.AvalonEdit.Highlighting.Xshd.HighlightingLoader.Load(reader, HighlightingManager.Instance); }
				}
				return null;
			}
		}
		public TextDocument EditorText => new TextDocument(model.ToEditorText());
		public FilterContainerViewModel FilterContainerViewModel => new FilterContainerViewModel(this, (View as TxtDocumentView).textEditor);
		public BindableCollection<Line> FilterResults { get; set; } = new BindableCollection<Line>();
		public bool Loaded => model.Loaded;
		public string Path => model.Path;
		public Line SelectedFilterResult
		{
			get => selectedFilterResult;
			set
			{
				selectedFilterResult = value;

				if (selectedFilterResult == null)
					return;

				TextEditor editor = (View as TxtDocumentView).textEditor;
				DocumentLine line = editor.Document.GetLineByNumber(selectedFilterResult.LineNumber);

				editor.Select(line.Offset, line.Length);
				editor.ScrollToLine(selectedFilterResult.LineNumber);
				// This code would put the text at the top but I prefer it in the middle
				/*double vertOffset = editor.TextArea.TextView.DefaultLineHeight * (selectedFilterResult.LineNumber - 2);
				editor.ScrollToVerticalOffset(vertOffset);*/
				NotifyOfPropertyChange(nameof(SelectedFilterResult));
			}
		}
		public bool ShowFilter { get; set; }
		public bool ShowFilterResults
		{
			get => showFilterResults;
			set
			{
				showFilterResults = value;
				NotifyOfPropertyChange(nameof(showFilterResults));
			}
		}
		public string StatusBarText => $"{model.Path} ({Math.Round(model.Size, 2)} KB)";
		public string TabHeader => System.IO.Path.GetFileName(model.Path);

		private Models.Document model;
		private Line selectedFilterResult;
		private bool showFilterResults;

		public TxtDocumentViewModel(Models.Document _model, bool loadNow)
		{
			model = _model;
			ShowFilter = DocumentType != DocumentType.UseType;

			if (loadNow)
				Load();
		}

		public object Clone()
		{
			return new TxtDocumentViewModel(new Models.Document(model.Path, model.FileType, model.DocumentType), Loaded);
		}

		public void DataGrid_ContextMenuClicked(object sender, RoutedEventArgs e)
		{
			string toEditorText = string.Empty;

			foreach (Line l in FilterResults)
				toEditorText += $"{l.LineText}{Environment.NewLine}";

			string tempPath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "NinjaTools", System.IO.Path.GetFileName(Path) + "_filtered.txt");

			File.WriteAllText(tempPath, toEditorText);
			// Force load on create becuase im not tracking individual temp files, aka could get overwritten before loading
			TxtDocumentViewModel vm = new TxtDocumentViewModel(new Models.Document(tempPath, model.FileType, DocumentType), true);
			if (Parent is AppViewModel parent)
				parent.Items.Add(vm);
			else
				((Parent as DocumentContainerViewModel).Parent as AppViewModel).Items.Add(vm);
		}

		public void Load()
		{
			model.Load();
		}

		public void TextEditor_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
		{
			if (Keyboard.Modifiers == ModifierKeys.Control)
			{
				if (e.Delta > 0 && Globals.Static.EditorFontSize < Globals.EditorFontMaxSize)
					Globals.Static.EditorFontSize = Math.Min(Globals.EditorFontMaxSize, Globals.Static.EditorFontSize + 1);
				else if (e.Delta <= 0 && Globals.Static.EditorFontSize > Globals.EditorFontMinSize)
					Globals.Static.EditorFontSize = Math.Max(Globals.EditorFontMinSize, Globals.Static.EditorFontSize - 1);
				e.Handled = true;
			}
		}

		protected override void OnViewLoaded()
		{
			TextEditor editor = (View as TxtDocumentView).textEditor;
			editor.Options.EnableHyperlinks = false;

			SearchPanel.Install(editor.TextArea);
			base.OnViewLoaded();
		}
	}
}