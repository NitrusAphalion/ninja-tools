using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Folding;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Search;
using Stylet;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Xml;
using System.Xml.Linq;

namespace NinjaTools.Pages
{
    public class AnalyzerResult
    {
        public string ItemName  { get; set; }
        public double ItemValue { get; set; }

        public AnalyzerResult(string itemName, double itemValue)
        {
            ItemName    = itemName;
            ItemValue   = itemValue;
        }
    }

    public class XmlDocumentViewModel : BaseClasses.CloseableTabItem, Interfaces.ICloseableTabItem, Interfaces.IDelayedLoading, Interfaces.IStatusBarText, Interfaces.IDocumentType
    {
        public BindableCollection<AnalyzerResult>   AnalyzerResults { get; set; }   = new BindableCollection<AnalyzerResult>();
        public XDocument                            Document                        => model.File as XDocument;
        public DocumentType                         DocumentType                    => model.DocumentType;
        public IHighlightingDefinition              EditorSyntax
        {
            get
            {
                if (File.Exists(model.SyntaxFile))
                {
                    using (Stream           stream = File.OpenRead(model.SyntaxFile))
                    using (XmlTextReader    reader = new XmlTextReader(stream))
                    {
                        return ICSharpCode.AvalonEdit.Highlighting.Xshd.HighlightingLoader.Load(reader, HighlightingManager.Instance);
                    }
                }
                return null;
            }
        }
        public TextDocument                         EditorText                      => new TextDocument(model.ToEditorText());
        public bool                                 Loaded                          => model.Loaded;
        public string                               Path                            => model.Path;
        public bool                                 ShowAnalyzerResults
        {
            get => showAnalyzerResults;
            set
            {
                showAnalyzerResults = value;
                NotifyOfPropertyChange(nameof(showAnalyzerResults));
            }
        }

        public string                               StatusBarText                   => $"{model.Path} ({Math.Round(model.Size, 2)} KB)";
        public string                               TabHeader                       => System.IO.Path.GetFileName(model.Path);
        public BindableCollection<XmlTreeViewItem>  TreeViewPaths   { get; set; }

        private FoldingManager      foldingManager;
        private XmlFoldingStrategy  foldingStrategy;
        private Models.Document     model               = null;
        private bool                showAnalyzerResults = false;

        public XmlDocumentViewModel(Models.Document _model, bool loadNow)
        {
            model = _model;
            ShowAnalyzerResults = DocumentType == DocumentType.Workspace;

            if (loadNow)
                Load();
        }

        public object Clone()
        {
            return new XmlDocumentViewModel(new Models.Document(model.Path, model.FileType, model.DocumentType), Loaded);
        }

        public void Load()
        {
            model.Load();
            SetupTreeViewPaths();

            if (ShowAnalyzerResults)
                RunWorkspaceAnalysis();
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

        public void TreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<Object> e)
        {
            TextEditor  editor      = (View as XmlDocumentView).textEditor;
            int         lineNumber  = ((e.NewValue as XmlTreeViewItem).Base as IXmlLineInfo).LineNumber;
            double      vertOffset  = editor.TextArea.TextView.DefaultLineHeight * (lineNumber - 2);

            editor.ScrollToVerticalOffset(vertOffset);

            DocumentLine                        line        = editor.Document.GetLineByNumber(lineNumber);
            ReadOnlyCollection<FoldingSection>  foldings    = foldingManager.GetFoldingsContaining(line.Offset);

            foreach (FoldingSection folding in foldings)
                foldingManager.RemoveFolding(folding);

            foldingStrategy.UpdateFoldings(foldingManager, editor.Document);
        }

        protected override void OnViewLoaded()
        {
            if (foldingManager == null)
            {
                TextEditor editor               = (View as XmlDocumentView).textEditor;
                editor.Options.EnableHyperlinks = false;
                foldingManager                  = FoldingManager.Install(editor.TextArea);
                foldingStrategy                 = new XmlFoldingStrategy();
                foldingStrategy.UpdateFoldings(foldingManager, editor.Document);
                SearchPanel.Install(editor.TextArea);
            }
            base.OnViewLoaded();
        }
        private void RunWorkspaceAnalysis()
        {
            AnalyzerResults.Clear();
            AnalyzerResult totalWindows         = new AnalyzerResult("Total Windows", Document.Descendants().Where(element => element.Parent?.Name == "NTWindows").Count());
            AnalyzerResult totalTabs            = new AnalyzerResult("Total Tabs", Document.Descendants().Where(element => element.Name.ToString().StartsWith("Tab-")).Count());
            AnalyzerResult totalInstruments     = new AnalyzerResult("Distinct Instruments", Document.Descendants("Instrument").Select(element => element.Value).Distinct().Count());
            AnalyzerResult totalDataSeries      = new AnalyzerResult("Distinct Data Series", Document.Descendants("BarsProperties").Distinct().Count());
            AnalyzerResult totalIndicators      = new AnalyzerResult("Total Indicators", Document.Descendants("Indicator").Count() + Document.Descendants("IndicatorInstance").Count() + Document.Descendants("SuperDomIndicatorSerialize").Count());
            AnalyzerResult totalStrategies      = new AnalyzerResult("Total Strategies", Document.Descendants("Strategies").Elements().Count());
            AnalyzerResult totalDrawingTools    = new AnalyzerResult("Total Drawing Tools", Document.Descendants("DrawingTools").Elements().Count());
            AnalyzerResult totalAlerts          = new AnalyzerResult("Total Alerts", Document.Descendants("Alert").Count());
            AnalyzerResult totalSuperDomColumns = new AnalyzerResult("Total SuperDom Columns", Document.Descendants("customColumns").Elements().Where(element => element.Name.ToString() != "columnWidth").Count());
            AnalyzerResult totalMAColumns       = new AnalyzerResult("Total MA Columns", Document.Descendants("MarketAnalyzerColumnSerialize").Count());
            AnalyzerResults.Add(totalWindows);
            AnalyzerResults.Add(totalTabs);
            AnalyzerResults.Add(totalInstruments);
            AnalyzerResults.Add(totalDataSeries);
            AnalyzerResults.Add(totalIndicators);
            AnalyzerResults.Add(totalStrategies);
            AnalyzerResults.Add(totalDrawingTools);
            AnalyzerResults.Add(totalAlerts);
            AnalyzerResults.Add(totalSuperDomColumns);
            AnalyzerResults.Add(totalMAColumns);
        }

        private void SetupTreeViewPaths()
        {
            TreeViewPaths           = new BindableCollection<XmlTreeViewItem>();
            XmlTreeViewItem root    = new XmlTreeViewItem(Document.Root, true);

            SetXmlTreeViewItemChildren(root, true);
            TreeViewPaths.Add(root);
        }

        private void SetXmlTreeViewItemChildren(XmlTreeViewItem parent, bool expand)
        {
            foreach (XElement element in parent.Base.Elements())
            {
                XmlTreeViewItem child = new XmlTreeViewItem(element, expand);
                SetXmlTreeViewItemChildren(child, false);
                parent.Items.Add(child);
            }
        }
    }

    public class XmlTreeViewItem
    {
        public XElement Base { get; set; }

        public string IconPath
        {
            get
            {
                if (Base.HasAttributes)
                    return "pack://application:,,,/Icons/TreeView/bullet_green.png";
                else if (Base.HasElements)
                    return "pack://application:,,,/Icons/TreeView/bullet_yellow.png";
                else
                    return "pack://application:,,,/Icons/TreeView/bullet_black.png";
            }
        }

        public bool                     IsExpanded  { get; set; }
        public List<XmlTreeViewItem>    Items       { get; set; }
        public string                   Name        => Base.Name.LocalName;

        public XmlTreeViewItem(XElement _base, bool isExpanded = false)
        {
            IsExpanded  = isExpanded;
            Base        = _base;
            Items       = new List<XmlTreeViewItem>();
        }
    }
}