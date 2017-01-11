﻿/*
    Copyright (C) 2014-2016 de4dot@gmail.com

    This file is part of dnSpy

    dnSpy is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    dnSpy is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with dnSpy.  If not, see <http://www.gnu.org/licenses/>.
*/

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;
using System.Windows.Data;
using System.Windows.Input;
using dnSpy.Contracts.MVVM;
using dnSpy.Contracts.Settings.AppearanceCategory;
using dnSpy.Contracts.Text.Classification;
using Microsoft.VisualStudio.Text.Classification;

namespace dnSpy.Debugger.Exceptions {
	interface IExceptionsVM {
		bool CanAddException { get; }
		void AddException();

		bool CanRemoveExceptions { get; }
		void RemoveExceptions();

		bool CanRestoreDefaults { get; }
		void RestoreDefaults();

		bool CanEnableAllFilteredExceptions { get; }
		void EnableAllFilteredExceptions();

		bool CanDisableAllFilteredExceptions { get; }
		void DisableAllFilteredExceptions();

		void Initialize(ISelectedItemsProvider<ExceptionVM> selectedItemsProvider);

		bool Exists(ExceptionType type, string name);
		void AddException(ExceptionType type, string name);
		void BreakWhenThrown(ExceptionType type, string name);
	}

	[Export(typeof(IExceptionsVM))]
	sealed class ExceptionsVM : ViewModelBase, IExceptionsVM {
		public ObservableCollection<ExceptionVM> Collection => exceptionsList;
		readonly ObservableCollection<ExceptionVM> exceptionsList;

		public ICollectionView CollectionView { get; }
		public ICommand AddExceptionCommand => new RelayCommand(a => AddException(), a => CanAddException);
		public ICommand RemoveExceptionsCommand => new RelayCommand(a => RemoveExceptions(), a => CanRemoveExceptions);
		public ICommand RestoreDefaultsCommand => new RelayCommand(a => RestoreDefaults(), a => CanRestoreDefaults);

		public bool ShowOnlyEnabledExceptions {
			get { return showOnlyEnabledExceptions; }
			set {
				if (showOnlyEnabledExceptions != value) {
					showOnlyEnabledExceptions = value;
					OnPropertyChanged(nameof(ShowOnlyEnabledExceptions));
					Refilter();
				}
			}
		}
		bool showOnlyEnabledExceptions;

		public string FilterText {
			get { return filterText; }
			set {
				if (filterText != value) {
					filterText = value;
					OnPropertyChanged(nameof(FilterText));
					Refilter();
				}
			}
		}
		string filterText = string.Empty;

		void Refilter() {
			var text = (filterText ?? string.Empty).Trim().ToUpperInvariant();
			if (text == string.Empty && !ShowOnlyEnabledExceptions)
				CollectionView.Filter = null;
			else
				CollectionView.Filter = o => CalculateIsVisible((ExceptionVM)o, text);
		}

		bool CalculateIsVisible(ExceptionVM vm, string filterText) {
			Debug.Assert(filterText != null && filterText.Trim().ToUpperInvariant() == filterText);
			if (ShowOnlyEnabledExceptions && !vm.BreakOnFirstChance)
				return false;
			return string.IsNullOrEmpty(filterText) || vm.Name.ToUpperInvariant().Contains(filterText);
		}

		readonly IDebuggerSettings debuggerSettings;
		readonly IExceptionService exceptionService;
		readonly IExceptionListSettings exceptionListSettings;
		ISelectedItemsProvider<ExceptionVM> selectedItemsProvider;
		readonly IGetNewExceptionName getNewExceptionName;
		readonly ExceptionContext exceptionContext;

		[ImportingConstructor]
		ExceptionsVM(IDebuggerSettings debuggerSettings, IExceptionService exceptionService, IExceptionListSettings exceptionListSettings, IGetNewExceptionName getNewExceptionName, IClassificationFormatMapService classificationFormatMapService, ITextElementProvider textElementProvider) {
			var classificationFormatMap = classificationFormatMapService.GetClassificationFormatMap(AppearanceCategoryConstants.UIMisc);
			this.debuggerSettings = debuggerSettings;
			this.exceptionService = exceptionService;
			this.exceptionListSettings = exceptionListSettings;
			this.getNewExceptionName = getNewExceptionName;
			exceptionContext = new ExceptionContext(exceptionService, classificationFormatMap, textElementProvider) {
				SyntaxHighlight = debuggerSettings.SyntaxHighlightExceptions,
			};
			exceptionsList = new ObservableCollection<ExceptionVM>();
			CollectionView = CollectionViewSource.GetDefaultView(exceptionsList);
			debuggerSettings.PropertyChanged += DebuggerSettings_PropertyChanged;
			exceptionService.Changed += ExceptionService_Changed;
			classificationFormatMap.ClassificationFormatMappingChanged += ClassificationFormatMap_ClassificationFormatMappingChanged;
			InitializeDefaultExceptions();
		}

		void ClassificationFormatMap_ClassificationFormatMappingChanged(object sender, EventArgs e) => RefreshThemeFields();

		public void Initialize(ISelectedItemsProvider<ExceptionVM> selectedItemsProvider) =>
			this.selectedItemsProvider = selectedItemsProvider;

		void ExceptionService_Changed(object sender, ExceptionServiceEventArgs e) {
			switch (e.EventType) {
			case ExceptionServiceEventType.Restored:
				InitializeDefaultExceptions();
				break;

			case ExceptionServiceEventType.Removed:
				Remove((List<ExceptionInfo>)e.Argument);
				break;

			case ExceptionServiceEventType.Added:
				Add((ExceptionInfo)e.Argument);
				break;

			case ExceptionServiceEventType.ExceptionInfoPropertyChanged:
				break;

			default:
				Debug.Fail(string.Format("Unknown type: {0}", e.EventType));
				break;
			}
		}

		void DebuggerSettings_PropertyChanged(object sender, PropertyChangedEventArgs e) {
			if (e.PropertyName == nameof(debuggerSettings.SyntaxHighlightExceptions)) {
				exceptionContext.SyntaxHighlight = debuggerSettings.SyntaxHighlightExceptions;
				RefreshThemeFields();
			}
		}

		void InitializeDefaultExceptions() {
			Collection.Clear();
			foreach (var info in Sort(exceptionService.ExceptionInfos))
				Collection.Add(new ExceptionVM(info, exceptionContext));
		}

		static ExceptionInfo[] Sort(IEnumerable<ExceptionInfo> infos) {
			var ary = infos.ToArray();
			Array.Sort(ary, CompareExceptionInfos);
			return ary;
		}

		static int CompareExceptionInfos(ExceptionInfo a, ExceptionInfo b) {
			int res = a.ExceptionType.CompareTo(b.ExceptionType);
			if (res != 0)
				return res;
			if (a.IsOtherExceptions != b.IsOtherExceptions)
				return a.IsOtherExceptions ? -1 : 1;
			return StringComparer.CurrentCultureIgnoreCase.Compare(a.Name, b.Name);
		}

		public void RefreshThemeFields() {
			foreach (var vm in Collection)
				vm.RefreshThemeFields();
		}

		public bool CanAddException => true;

		public void AddException() {
			var name = getNewExceptionName.GetName();
			if (string.IsNullOrEmpty(name))
				return;
			name = name.Trim();
			if (string.IsNullOrEmpty(name))
				return;
			AddException(ExceptionType.DotNet, name);
		}

		public void AddException(ExceptionType type, string name) {
			if (name == null)
				return;
			exceptionService.Add(new ExceptionInfoKey(type, name));
		}

		public void BreakWhenThrown(ExceptionType type, string name) {
			var key = new ExceptionInfoKey(type, name);
			if (!exceptionService.Exists(key))
				exceptionService.Add(key);
			var vm = Collection.FirstOrDefault(a => a.ExceptionInfo.Key.Equals(key));
			Debug.Assert(vm != null);
			if (vm != null)
				vm.BreakOnFirstChance = true;
		}

		void Add(ExceptionInfo info) {
			var vm = new ExceptionVM(info, exceptionContext);
			int i;
			for (i = 0; i + 1 < Collection.Count; i++) {
				int res = CompareExceptionInfos(vm.ExceptionInfo, Collection[i].ExceptionInfo);
				if (res <= 0)
					break;
			}
			Collection.Insert(i, vm);
		}

		ExceptionVM[] GetRemovableExceptions(ExceptionVM[] items) => items.Where(a => exceptionService.CanRemove(a.ExceptionInfo)).ToArray();
		public bool CanRemoveExceptions => GetRemovableExceptions(selectedItemsProvider.SelectedItems).Length != 0;

		public void RemoveExceptions() {
			var items = GetRemovableExceptions(selectedItemsProvider.SelectedItems);
			exceptionService.RemoveExceptions(items.Select(a => a.ExceptionInfo));
		}

		void Remove(List<ExceptionInfo> infos) {
			var dict = new Dictionary<ExceptionInfo, int>(Collection.Count);
			for (int i = 0; i < Collection.Count; i++)
				dict[Collection[i].ExceptionInfo] = i;
			var ary = new int[infos.Count];
			for (int i = 0; i < infos.Count; i++)
				ary[i] = dict[infos[i]];
			Array.Sort(ary);
			for (int i = ary.Length - 1; i >= 0; i--)
				Collection.RemoveAt(ary[i]);
		}

		public bool CanRestoreDefaults => true;

		public void RestoreDefaults() {
			exceptionService.RestoreDefaults();
			FilterText = string.Empty;
			ShowOnlyEnabledExceptions = false;
		}

		public bool CanEnableAllFilteredExceptions => CollectionView.OfType<ExceptionVM>().Any(a => !a.BreakOnFirstChance);

		public void EnableAllFilteredExceptions() {
			using (exceptionListSettings.TemporarilyDisableSave()) {
				foreach (ExceptionVM vm in CollectionView)
					vm.BreakOnFirstChance = true;
			}
		}

		public bool CanDisableAllFilteredExceptions => CollectionView.OfType<ExceptionVM>().Any(a => a.BreakOnFirstChance);

		public void DisableAllFilteredExceptions() {
			using (exceptionListSettings.TemporarilyDisableSave()) {
				foreach (ExceptionVM vm in CollectionView)
					vm.BreakOnFirstChance = false;
			}
			// Don't Refilter() now since items could be hidden
		}

		public bool Exists(ExceptionType type, string name) => exceptionService.Exists(new ExceptionInfoKey(type, name));
	}
}
