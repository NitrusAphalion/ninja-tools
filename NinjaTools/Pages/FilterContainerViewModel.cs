using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace NinjaTools.Pages
{
	public class DocumentDetails
	{
		public Dictionary<string, Dictionary<int, string>> Accounts
		{
			get
			{
				if (accounts != null)
					return accounts;

				accounts = new Dictionary<string, Dictionary<int, string>>();
				ISearchStrategy strategy = SearchStrategyFactory.Create("(?<=account=')[^']+|(?<=account=\")[^ \"]+", true, false, SearchMode.RegEx);

				GetSearchResult(ref accounts, strategy);
				return accounts;
			}
		}
		public Dictionary<int, string> Errors
		{
			get
			{
				if (errors != null)
					return errors;

				errors = new Dictionary<int, string>();
				ISearchStrategy strategy = SearchStrategyFactory.Create("unhandled\\sexception\\strapped|\\sERROR.+|exception|Exception|\\sError\\s.+|Chart\\srendering\\sfailed|A\\sdirect\\sX\\serror\\shas\\soccurred\\swhile\\srendering\\sthe\\schart|(?<=\\|)Unable\\sto\\sload\\sserver\\sside\\sconfig\\sfile|Multiple\\sBartypes\\swith\\sthe\\ssame\\sBarsPeriodType\\swere\\sfound", false, false, SearchMode.RegEx);

				GetSearchResult(ref errors, strategy);
				return errors;
			}
		}
		public Dictionary<string, Dictionary<int, string>> Orders
		{
			get
			{
				if (orders != null)
					return orders;

				orders = new Dictionary<string, Dictionary<int, string>>();
				ISearchStrategy strategy = SearchStrategyFactory.Create("(?<=orderId=')[^']+|(?<=orderId=\")[^ \"]+", true, false, SearchMode.RegEx);

				GetSearchResult(ref orders, strategy);
				return orders;
			}
		}
		public Dictionary<int, string> SystemInfo
		{
			get
			{
				if (systemInfo != null)
					return systemInfo;

				systemInfo = new Dictionary<int, string>();
				ISearchStrategy strategy = SearchStrategyFactory.Create("\\sInstallDir=|\\sUserDataDir=|\\sMachineID=|\\sOS=|\\sOSLanguage=|\\sOSEnvironment=|\\sProcessors=|\\sProcessorSpeed=|\\sPhysicalMemory=|\\sDisplayAdapters=\\s|\\sMonitors=|\\s\\.Net\\\\CLR\\sVersion=|\\sSqlCeVersion=|\\sApplicationTimezone=|\\sLocalTimezone=|\\sDirectXRenderingHW|\\sDirectXRenderingSW", false, false, SearchMode.RegEx);

				GetSearchResult(ref systemInfo, strategy);
				return systemInfo;
			}
		}
		public Dictionary<int, KeyValuePair<DateTime, string>> Times
		{
			get
			{
				if (times != null)
					return times;

				times = new Dictionary<int, KeyValuePair<DateTime, string>>();
				int lineNumber = 0;

				foreach (string line in document.Text.Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries))
				{
					DateTime time = DateTime.MinValue;
					DateTime.TryParseExact(line.Substring(0, 23), "yyyy-MM-dd HH:mm:ss:fff", System.Globalization.CultureInfo.CurrentCulture, System.Globalization.DateTimeStyles.None, out time);
					if (time != DateTime.MinValue)
						times.Add(lineNumber, new KeyValuePair<DateTime, string>(time, line));
					lineNumber++;
				}
				return times;
			}
		}
		public Dictionary<int, string> Warnings
		{
			get
			{
				if (warnings != null)
					return warnings;

				warnings = new Dictionary<int, string>();
				ISearchStrategy strategy = SearchStrategyFactory.Create("NinjaTrader\\shas\\sdetected\\syour\\ssystem\\sis\\sentering\\sa\\ssuspended\\sstate|NinjaTrader\\shas\\sdetected\\syour\\ssystem\\shas\\srecovered\\sfrom\\sa\\ssuspended\\sstate|Please\\supgrade\\sto\\sa\\sLifetime\\slicense\\sto\\sunlock\\s\'Order\\sFlow\\s+\'\\sfunctionality|Loss\\sof\\sDirectX\\sdevice\\sdetected|Successfully\\srecreated\\sDirectX\\sdevice", false, false, SearchMode.RegEx);

				GetSearchResult(ref warnings, strategy);
				return warnings;
			}
		}

		private Dictionary<string, Dictionary<int, string>> accounts;
		private TextDocument document;
		private Dictionary<int, string> errors;
		private Dictionary<string, Dictionary<int, string>> orders;
		private Dictionary<int, string> systemInfo;
		private Dictionary<int, KeyValuePair<DateTime, string>> times;
		private Dictionary<int, string> warnings;

		public DocumentDetails(TextDocument _document)
		{
			document = _document;
		}

		public Dictionary<int, string> GetManualResult(string text, bool regexMode, bool ignoreCaseMode)
		{
			Dictionary<int, string> returnValue = new Dictionary<int, string>();
			ISearchStrategy strategy = SearchStrategyFactory.Create(text, ignoreCaseMode, false, regexMode ? SearchMode.RegEx : SearchMode.Normal);

			GetSearchResult(ref returnValue, strategy);
			return returnValue;
		}

		private void GetSearchResult(ref Dictionary<int, string> privateObject, ISearchStrategy searchStrategy)
		{
			foreach (ISearchResult result in searchStrategy.FindAll(document, 0, document.TextLength))
			{
				ISegment segment = result as ISegment;
				DocumentLine docLine = document.GetLineByOffset(segment.Offset);
				int lineNumber = docLine.LineNumber;
				string lineText = document.GetText(docLine.Offset, docLine.Length);

				if (!privateObject.ContainsKey(lineNumber))
					privateObject.Add(lineNumber, lineText);
			}
		}

		private void GetSearchResult(ref Dictionary<string, Dictionary<int, string>> privateObject, ISearchStrategy searchStrategy)
		{
			foreach (ISearchResult result in searchStrategy.FindAll(document, 0, document.TextLength))
			{
				ISegment segment = result as ISegment;
				DocumentLine docLine = document.GetLineByOffset(segment.Offset);
				string name = document.GetText(segment);
				int lineNumber = docLine.LineNumber;
				string lineText = document.GetText(docLine.Offset, docLine.Length);

				if (!privateObject.ContainsKey(name))
				{
					Dictionary<int, string> toAdd = new Dictionary<int, string>();
					toAdd.Add(lineNumber, lineText);
					privateObject.Add(name, toAdd);
				}
				else if (!privateObject[name].ContainsKey(lineNumber))
					privateObject[name].Add(lineNumber, lineText);
			}
		}
	}

	public class FilterContainerViewModel : Conductor<IScreen>.Collection.AllActive
	{
		public DocumentDetails Details => new DocumentDetails(Editor.Document);
		public TextEditor Editor { get; set; }

		public FilterContainerViewModel(object parent, TextEditor editor)
		{
			Parent = parent;
			Editor = editor;
			Items.Add(new FilterControlViewModel(FilterRowType.None, true));
		}

		public void UpdateFilterResults()
		{
			TxtDocumentViewModel parent = Parent as TxtDocumentViewModel;
			lock (parent)
			{
				parent.FilterResults.Clear();
				Dictionary<Brush, List<Dictionary<int, string>>> groups = new Dictionary<Brush, List<Dictionary<int, string>>>();

				if (Items.All(filter => (filter as FilterControlViewModel).SelectedFilter.Type == FilterRowType.None))
				{
					parent.ShowFilterResults = false;
					return;
				}

				foreach (FilterControlViewModel filter in Items)
				{
					if (filter.Result == null)
						continue;

					if (groups.ContainsKey(filter.Result.Group))
						groups[filter.Result.Group].Add(filter.Result.Lines);
					else
					{
						List<Dictionary<int, string>> temp = new List<Dictionary<int, string>>();
						temp.Add(filter.Result.Lines);
						groups.Add(filter.Result.Group, temp);
					}
				}

				foreach (KeyValuePair<Brush, List<Dictionary<int, string>>> group in groups)
				{
					if (group.Key is DrawingBrush)
					{
						foreach (Dictionary<int, string> dict in group.Value)
						{
							foreach (KeyValuePair<int, string> kvp in dict)
							{
								Line line = new Line(kvp.Key, kvp.Value, group.Key);
								if (!parent.FilterResults.Contains(line))
									parent.FilterResults.Add(line);
								else
									parent.FilterResults.First(l => l.LineNumber == line.LineNumber).AddGroup(group.Key);
							}
						}
						continue;
					}

					int longestLength = group.Value.Max(d => d.Count);
					Dictionary<int, string> longestDictionary = group.Value.First(d => d.Count == longestLength);

					foreach (KeyValuePair<int, string> kvp in longestDictionary)
					{
						bool keyIsGood = true;
						foreach (Dictionary<int, string> compareDict in group.Value)
						{
							if (!compareDict.ContainsKey(kvp.Key))
							{
								keyIsGood = false;
								break;
							}
						}
						if (keyIsGood)
						{
							Line line = new Line(kvp.Key, kvp.Value, group.Key);
							if (!parent.FilterResults.Contains(line))
								parent.FilterResults.Add(line);
							else
								parent.FilterResults.First(l => l.LineNumber == line.LineNumber).AddGroup(group.Key);
						}
					}
				}

				// When you switch filters, the column size will not shrink by itself
				Application.Current.Dispatcher.Invoke(new Action(() =>
				{
					foreach (DataGridColumn col in (parent.View as TxtDocumentView).dataGrid.Columns)
					{
						col.Width = DataGridLength.SizeToCells;
						col.Width = DataGridLength.Auto;
					}
				}));

				/*foreach (FilterControlViewModel filter in Items)
                {
                    if (filter.Result == null)
                        return;
                    foreach (KeyValuePair<int, string> line in filter.Result.Lines)
                        (Parent as TxtDocumentViewModel).FilterResults.Add(new Line(line.Key, line.Value));
                }*/

				(Parent as TxtDocumentViewModel).ShowFilterResults = true;
			}
		}
	}

	public class Line : IEquatable<Line>
	{
		public List<Brush> Groups { get; set; }
		public int LineNumber { get; set; }
		public string LineText { get; set; }
		public Brush SelectedBrush { get; set; }

		public Line(int lineNumber, string lineText, Brush group)
		{
			LineNumber = lineNumber;
			LineText = lineText;
			Groups = new List<Brush>();

			Groups.Add(group);
			SelectedBrush = Groups[0];
		}

		public void AddGroup(Brush group)
		{
			Groups.Add(group);
		}

		public bool Equals(Line other)
		{
			return LineNumber == other.LineNumber;
		}
	}
}