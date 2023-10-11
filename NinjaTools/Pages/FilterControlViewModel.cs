using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace NinjaTools.Pages
{
	public class FilterBase
	{
		public int Level { get; set; }
		public string Name { get; set; }
		public FilterRowType Type { get; set; }

		public FilterBase(FilterRowType type)
		{
			Type = type;
			Name = Enum.GetName(typeof(FilterRowType), Type);
		}

		public FilterBase(string name)
		{
			Name = name;
		}
	}

	public class FilterControlViewModel : Screen
	{
		public List<Brush> AvailableBrushes => new List<Brush>() { transparentBrush, Brushes.Red, Brushes.HotPink, Brushes.Blue, Brushes.Cyan, Brushes.Green, Brushes.Yellow };
		public DocumentDetails Details => (Parent as FilterContainerViewModel).Details;
		public DateTime EndTime
		{
			get => endTime;
			set
			{
				endTime = value;
				NotifyOfPropertyChange(nameof(EndTime));
				Result = new FilterResult(Group);
				foreach (KeyValuePair<int, KeyValuePair<DateTime, string>> result in Details.Times.Where(d => d.Value.Key >= StartTime && d.Value.Key <= EndTime))
					Result.Lines.Add(result.Key, result.Value.Value);
				UpdateFilterResults();
			}
		}
		public BindableCollection<FilterBase> Filter1Items { get; set; } = new BindableCollection<FilterBase>();
		public BindableCollection<FilterBase> Filter2Items { get; set; } = new BindableCollection<FilterBase>();
		public Brush Group
		{
			get => group;
			set
			{
				group = value;
				if (Result != null)
					Result.Group = group;
				UpdateFilterResults();
			}
		}
		public bool IgnoreCaseMode
		{
			get => ignoreCaseMode;
			set
			{
				if (ignoreCaseMode == value || ManualText == null)
					return;
				ignoreCaseMode = value;
				Result = ManualText == string.Empty ? null : new FilterResult(Group, Details.GetManualResult(ManualText, RegexMode, ignoreCaseMode));
				UpdateFilterResults();
			}
		}
		public bool IsDefault { get; private set; }
		public bool IsNotDefault => !IsDefault;
		public string ManualText
		{
			get => manualText;
			set
			{
				if (manualText == value)
					return;

				manualText = value;
				NotifyOfPropertyChange(nameof(ManualText));
				Result = value == string.Empty ? null : new FilterResult(Group, Details.GetManualResult(manualText, RegexMode, IgnoreCaseMode));
				UpdateFilterResults();
			}
		}
		public bool RegexMode
		{
			get => regexMode;
			set
			{
				if (regexMode == value || ManualText == null)
					return;

				regexMode = value;
				Result = ManualText == string.Empty ? null : new FilterResult(Group, Details.GetManualResult(ManualText, regexMode, IgnoreCaseMode));
				UpdateFilterResults();
			}
		}
		public FilterResult Result { get; private set; }
		public FilterBase SelectedFilter
		{
			get => selectedFilter;
			set
			{
				selectedFilter = Filter1Items.First(fb => fb.Type == (value as FilterBase).Type);
				switch (selectedFilter.Type)
				{
					case FilterRowType.None:
						Result = null;
						UpdateFilterResults();
						break;

					case FilterRowType.SystemInfo:
						Result = new FilterResult(Group, Details.SystemInfo);
						UpdateFilterResults();
						break;

					case FilterRowType.Warning:
						Result = new FilterResult(Group, Details.Warnings);
						UpdateFilterResults();
						break;

					case FilterRowType.Error:
						Result = new FilterResult(Group, Details.Errors);
						UpdateFilterResults();
						break;

					case FilterRowType.Account:
						Filter2Items.Clear();
						foreach (string accountName in Details.Accounts.Keys)
							Filter2Items.Add(new FilterBase(accountName));
						if (Filter2Items.Count > 0)
							SelectedFilter2 = Filter2Items[0];
						break;

					case FilterRowType.Order:
						Filter2Items.Clear();
						foreach (string orderName in Details.Orders.Keys)
							Filter2Items.Add(new FilterBase(orderName));
						if (Filter2Items.Count > 0)
							SelectedFilter2 = Filter2Items[0];
						break;

					case FilterRowType.Time:
						StartTime = Details.Times.First().Value.Key;
						EndTime = Details.Times.Last().Value.Key;
						break;
				}
				NotifyOfPropertyChange(nameof(SelectedFilter));
			}
		}
		public FilterBase SelectedFilter2
		{
			get => selectedFilter2;
			set
			{
				if (value == null)
				{
					selectedFilter2 = null;
					NotifyOfPropertyChange(nameof(SelectedFilter2));
					return;
				}
				else
					selectedFilter2 = Filter2Items.First(fb => fb.Name == (value as FilterBase).Name);

				switch (selectedFilter.Type)
				{
					case FilterRowType.Account:
						Result = new FilterResult(Group, Details.Accounts[selectedFilter2.Name]);
						UpdateFilterResults();
						break;

					case FilterRowType.Order:
						Result = new FilterResult(Group, Details.Orders[selectedFilter2.Name]);
						UpdateFilterResults();
						break;
				}
				NotifyOfPropertyChange(nameof(SelectedFilter2));
			}
		}
		public DateTime StartTime
		{
			get => startTime;
			set
			{
				startTime = value;
				NotifyOfPropertyChange(nameof(StartTime));
				if (endTime == DateTime.MinValue)
					return;
				Result = new FilterResult(Group);
				foreach (KeyValuePair<int, KeyValuePair<DateTime, string>> result in Details.Times.Where(d => d.Value.Key >= StartTime && d.Value.Key <= EndTime))
					Result.Lines.Add(result.Key, result.Value.Value);
				UpdateFilterResults();
			}
		}

		private DateTime endTime;
		private Brush group;
		private bool ignoreCaseMode;
		private string manualText;
		private bool regexMode;
		private FilterBase selectedFilter;
		private FilterBase selectedFilter2;
		private DateTime startTime;
		private DrawingBrush transparentBrush = Application.Current.FindResource("CheckerBrush") as DrawingBrush;

		public FilterControlViewModel(FilterRowType rowType, bool isDefault)
		{
			IsDefault = isDefault;
			group = transparentBrush;

			foreach (FilterRowType t in Enum.GetValues(typeof(FilterRowType)))
				Filter1Items.Add(new FilterBase(t));

			selectedFilter = Filter1Items.First(fb => fb.Type == rowType);
		}

		public void OnAddFilter(object sender, RoutedEventArgs e)
		{
			(Parent as FilterContainerViewModel).Items.Add(new FilterControlViewModel(FilterRowType.None, false));
		}

		public void OnRemoveFilter(object sender, RoutedEventArgs e)
		{
			FilterContainerViewModel parent = Parent as FilterContainerViewModel;
			parent.Items.Remove(this);
			UpdateFilterResults(parent);
		}

		public void TextBox_PreviewKeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Enter)
			{
				ManualText = (sender as TextBox).Text;
				Keyboard.ClearFocus();
			}
		}

		private void UpdateFilterResults(FilterContainerViewModel parent = null)
		{
			Task.Run(() => (parent ?? Parent as FilterContainerViewModel).UpdateFilterResults());
		}
	}

	public class FilterResult
	{
		public Brush Group { get; set; }
		public Dictionary<int, string> Lines { get; set; }

		public FilterResult(Brush group, Dictionary<int, string> lines = null)
		{
			Group = group;
			Lines = lines ?? new Dictionary<int, string>();
		}
	}
}