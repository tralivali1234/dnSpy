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
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Threading;
using dnSpy.Contracts.App;
using dnSpy.Contracts.Controls;
using dnSpy.Contracts.Decompiler;
using dnSpy.Contracts.Documents.Tabs;
using dnSpy.Contracts.Images;
using dnSpy.Contracts.Settings;
using dnSpy.Controls;
using dnSpy.Culture;
using dnSpy.Documents.Tabs.Dialogs;
using dnSpy.Extension;
using dnSpy.Images;
using dnSpy.Roslyn.Shared.Text.Classification;
using dnSpy.Scripting;
using dnSpy.Settings;

namespace dnSpy.MainApp {
	sealed partial class App : Application {
		static App() {
			// Make sure we have a ref to the assembly. The file is copied to the correct location
			// but unless we have a reference to it in the code (or XAML), it could easily happen
			// that someone accidentally removes the reference. This code makes sure there'll be
			// a compilation error if that ever happens.
			var t = typeof(Images.Dummy);
			InstallExceptionHandlers();
		}

		static void InstallExceptionHandlers() {
			TaskScheduler.UnobservedTaskException += (s, e) => e.SetObserved();
			if (!Debugger.IsAttached) {
				AppDomain.CurrentDomain.UnhandledException += (s, e) => ShowException(e.ExceptionObject as Exception);
				Dispatcher.CurrentDispatcher.UnhandledException += (s, e) => {
					ShowException(e.Exception);
					e.Handled = true;
				};
			}
		}

		static void ShowException(Exception ex) {
			string msg = ex?.ToString() ?? "Unknown exception";
			MessageBox.Show(msg, "dnSpy", MessageBoxButton.OK, MessageBoxImage.Error);
		}

		[Import]
		AppWindow appWindow = null;
		[Import]
		SettingsService settingsService = null;
		[Import]
		ExtensionService extensionService = null;
		[Import]
		IDsLoaderService dsLoaderService = null;
		[Import]
		Lazy<IDocumentTabService> documentTabService = null;
		[Import]
		Lazy<IDecompilerService> decompilerService = null;
		[ImportMany]
		IEnumerable<Lazy<IAppCommandLineArgsHandler>> appCommandLineArgsHandlers = null;
		readonly List<LoadedExtension> loadedExtensions = new List<LoadedExtension>();
		CompositionContainer compositionContainer;

		readonly IAppCommandLineArgs args;

		public App(bool readSettings) {
			args = new AppCommandLineArgs();
			AppDirectories.SetSettingsFilename(args.SettingsFilename);
			if (args.SingleInstance)
				SwitchToOtherInstance();

			AddAppContextFixes();

			InitializeComponent();
			UIFixes();

			InitializeMEF(readSettings);
			compositionContainer.ComposeParts(this);
			extensionService.LoadedExtensions = loadedExtensions;
			appWindow.CommandLineArgs = args;

			Exit += App_Exit;
		}

		// These can also be put in the App.config file (semicolon-separated) but I prefer code in
		// this case.
		void AddAppContextFixes() {
			// This prevents a thin line between the tab item and its content when dpi is eg. 144.
			// It's hard to miss if you check the Options dialog box.
			AppContext.SetSwitch("Switch.MS.Internal.DoNotApplyLayoutRoundingToMarginsAndBorderThickness", true);
		}

		void InitializeMEF(bool readSettings) {
			compositionContainer = InitializeCompositionContainer();
			compositionContainer.GetExportedValue<ServiceLocator>().SetCompositionContainer(compositionContainer);
			if (readSettings) {
				var settingsService = compositionContainer.GetExportedValue<ISettingsService>();
				try {
					new XmlSettingsReader(settingsService).Read();
				}
				catch {
				}
			}

			var cultureService = compositionContainer.GetExportedValue<CultureService>();
			cultureService.Initialize(args);

			// Make sure IDpiService gets created before any MetroWindows
			compositionContainer.GetExportedValue<IDpiService>();

			// It's needed very early, and an IAutoLoaded can't be used (it gets called too late for the first 64x64 image request)
			DsImageConverter.imageService = compositionContainer.GetExportedValue<IImageService>();
		}

		CompositionContainer InitializeCompositionContainer() {
			var aggregateCatalog = new AggregateCatalog();
			aggregateCatalog.Catalogs.Add(new AssemblyCatalog(GetType().Assembly));
			// dnSpy.Contracts.DnSpy
			aggregateCatalog.Catalogs.Add(new AssemblyCatalog(typeof(MetroWindow).Assembly));
			// dnSpy.Roslyn.Shared
			aggregateCatalog.Catalogs.Add(new AssemblyCatalog(typeof(RoslynClassifier).Assembly));
			// Microsoft.VisualStudio.Text.Logic (needed for the editor option definitions)
			aggregateCatalog.Catalogs.Add(new AssemblyCatalog(typeof(Microsoft.VisualStudio.Text.Editor.ConvertTabsToSpaces).Assembly));
			// Microsoft.VisualStudio.Text.UI (needed for the editor option definitions)
			aggregateCatalog.Catalogs.Add(new AssemblyCatalog(typeof(Microsoft.VisualStudio.Text.Editor.AutoScrollEnabled).Assembly));
			// Microsoft.VisualStudio.Text.UI.Wpf (needed for the editor option definitions)
			aggregateCatalog.Catalogs.Add(new AssemblyCatalog(typeof(Microsoft.VisualStudio.Text.Editor.HighlightCurrentLineOption).Assembly));
			// dnSpy.Roslyn.EditorFeatures
			aggregateCatalog.Catalogs.Add(new AssemblyCatalog(typeof(Roslyn.EditorFeatures.Dummy).Assembly));
			// dnSpy.Roslyn.VisualBasic.EditorFeatures
			aggregateCatalog.Catalogs.Add(new AssemblyCatalog(typeof(Roslyn.VisualBasic.EditorFeatures.Dummy).Assembly));
			AddExtensionFiles(aggregateCatalog);
			var options = CompositionOptions.Default;
#if DEBUG
			options |= CompositionOptions.DisableSilentRejection;
#endif
			return new CompositionContainer(aggregateCatalog, options);
		}

		void AddExtensionFiles(AggregateCatalog aggregateCatalog) {
			var dir = Path.GetDirectoryName(GetType().Assembly.Location);
			// Load the modules in a predictable order or multicore-JIT could stop recording. See
			// "Understanding Background JIT compilation -> What can go wrong with background JIT compilation"
			// in the PerfView docs for more info.
			var files = GetExtensionFiles(dir).OrderBy(a => a, StringComparer.OrdinalIgnoreCase).ToArray();
			foreach (var file in files) {
				try {
					if (!File.Exists(file))
						continue;
					if (!CanLoadExtension(file))
						continue;
					var asm = Assembly.LoadFrom(file);
					if (!CanLoadExtension(asm)) {
						Debug.WriteLine($"Old extension detected ({file})");
						continue;
					}
					aggregateCatalog.Catalogs.Add(new AssemblyCatalog(asm));
					loadedExtensions.Add(new LoadedExtension(asm));
				}
				catch (Exception ex) {
					Debug.WriteLine($"Failed to load file '{file}', msg: {ex.Message}");
				}
			}
		}

		IEnumerable<string> GetExtensionFiles(string baseDir) {
			const string EXTENSION_SEARCH_PATTERN = "*.x.dll";
			const string EXTENSIONS_SUBDIR = "Extensions";
			foreach (var f in GetFiles(baseDir, EXTENSION_SEARCH_PATTERN))
				yield return f;
			var extensionsSubDir = Path.Combine(baseDir, EXTENSIONS_SUBDIR);
			if (Directory.Exists(extensionsSubDir)) {
				foreach (var f in GetFiles(extensionsSubDir, EXTENSION_SEARCH_PATTERN))
					yield return f;
				foreach (var d in GetDirectories(extensionsSubDir)) {
					foreach (var f in GetFiles(d, EXTENSION_SEARCH_PATTERN))
						yield return f;
				}
			}
		}

		static string[] GetFiles(string dir, string searchPattern) {
			try {
				return Directory.GetFiles(dir, searchPattern);
			}
			catch {
				return Array.Empty<string>();
			}
		}

		static string[] GetDirectories(string dir) {
			try {
				return Directory.GetDirectories(dir);
			}
			catch {
				return Array.Empty<string>();
			}
		}

		bool CanLoadExtension(string file) {
			var xmlFile = file + ".xml";
			var config = ExtensionConfigReader.Read(xmlFile);
			return config.IsSupportedOSversion(Environment.OSVersion.Version) &&
				config.IsSupportedFrameworkVersion(Environment.Version) &&
				config.IsSupportedAppVersion(GetType().Assembly.GetName().Version);
		}

		bool CanLoadExtension(Assembly asm) {
			var ourPublicKeyToken = GetType().Assembly.GetName().GetPublicKeyToken();
			var minimumVersion = new Version(3, 0, 0, 0);
			foreach (var a in asm.GetReferencedAssemblies()) {
				if (!Equals(ourPublicKeyToken, a.GetPublicKeyToken()))
					continue;
				if (a.Version < minimumVersion)
					return false;
			}
			return true;
		}

		static bool Equals(byte[] a, byte[] b) {
			if (a == null || b == null || a.Length != b.Length)
				return false;
			for (int i = 0; i < a.Length; i++) {
				if (a[i] != b[i])
					return false;
			}
			return true;
		}

		[return: MarshalAs(UnmanagedType.Bool)]
		[DllImport("user32")]
		static extern bool SetForegroundWindow(IntPtr hWnd);
		[DllImport("user32")]
		[return: MarshalAs(UnmanagedType.Bool)]
		static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);
		[return: MarshalAs(UnmanagedType.Bool)]
		delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);
		[DllImport("user32", CharSet = CharSet.Auto, SetLastError = true)]
		static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);
		[DllImport("user32", CharSet = CharSet.Auto)]
		static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, ref COPYDATASTRUCT lParam);
		[StructLayout(LayoutKind.Sequential)]
		struct COPYDATASTRUCT {
			public IntPtr dwData;
			public int cbData;
			public IntPtr lpData;
		}
		static readonly IntPtr COPYDATASTRUCT_dwData = new IntPtr(0x11C9B152);
		static readonly IntPtr COPYDATASTRUCT_result = new IntPtr(0x615F9D6E);
		const string COPYDATASTRUCT_HEADER = "dnSpy";	// One line only

		void SwitchToOtherInstance() => EnumWindows(EnumWindowsHandler, IntPtr.Zero);

		unsafe bool EnumWindowsHandler(IntPtr hWnd, IntPtr lParam) {
			var sb = new StringBuilder(256);
			GetWindowText(hWnd, sb, sb.Capacity);
			if (sb.ToString().StartsWith("dnSpy ", StringComparison.Ordinal)) {
				var args = Environment.GetCommandLineArgs();
				args[0] = COPYDATASTRUCT_HEADER;
				var msg = string.Join(Environment.NewLine, args);
				COPYDATASTRUCT data;
				data.dwData = COPYDATASTRUCT_dwData;
				data.cbData = msg.Length * 2;
				fixed (void* pmsg = msg) {
					data.lpData = new IntPtr(pmsg);
					var res = SendMessage(hWnd, 0x4A, IntPtr.Zero, ref data);
					if (res == COPYDATASTRUCT_result) {
						if (this.args.Activate)
							SetForegroundWindow(hWnd);
						Environment.Exit(0);
					}
				}
			}

			return true;
		}

		unsafe IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled) {
			if (msg != 0x4A)
				return IntPtr.Zero;

			var data = *(COPYDATASTRUCT*)lParam;
			if (data.dwData != COPYDATASTRUCT_dwData)
				return IntPtr.Zero;

			var argsString = new string((char*)data.lpData, 0, data.cbData / 2);
			var args = argsString.Split(new[] { Environment.NewLine }, StringSplitOptions.None);
			if (args[0] != COPYDATASTRUCT_HEADER)
				return IntPtr.Zero;

			HandleAppArgs(new AppCommandLineArgs(args.Skip(1).ToArray()));
			handled = true;
			return COPYDATASTRUCT_result;
		}

		void MainWindow_SourceInitialized(object sender, EventArgs e) {
			appWindow.MainWindow.SourceInitialized -= MainWindow_SourceInitialized;

			var hwndSource = PresentationSource.FromVisual(appWindow.MainWindow) as HwndSource;
			Debug.Assert(hwndSource != null);
			if (hwndSource != null)
				hwndSource.AddHook(WndProc);
		}

		void App_Exit(object sender, ExitEventArgs e) {
			extensionService.OnAppExit();
			dsLoaderService.Save();
			try {
				new XmlSettingsWriter(settingsService).Write();
			}
			catch {
			}
			compositionContainer.Dispose();
		}

		void UIFixes() {
			// Add Ctrl+Shift+Z as a redo command. Don't know why it isn't enabled by default.
			ApplicationCommands.Redo.InputGestures.Add(new KeyGesture(Key.Z, ModifierKeys.Control | ModifierKeys.Shift));
			FixEditorContextMenuStyle();
		}

		// The text editor creates an EditorContextMenu which derives from ContextMenu. This
		// class is private in the assembly and can't be referenced from XAML. In order to style
		// this class we must get the type at runtime and add its style to the Resources.
		void FixEditorContextMenuStyle() {
			var module = typeof(ContextMenu).Module;
			var type = module.GetType("System.Windows.Documents.TextEditorContextMenu+EditorContextMenu", false, false);
			Debug.Assert(type != null);
			if (type == null)
				return;
			const string styleKey = "EditorContextMenuStyle";
			var style = Resources[styleKey];
			Debug.Assert(style != null);
			if (style == null)
				return;
			Resources.Remove(styleKey);
			Resources.Add(type, style);
		}

		protected override void OnStartup(StartupEventArgs e) {
			base.OnStartup(e);

			var win = appWindow.InitializeMainWindow();
			appWindow.MainWindow.SourceInitialized += MainWindow_SourceInitialized;
			dsLoaderService.OnAppLoaded += DsLoaderService_OnAppLoaded;
			dsLoaderService.Initialize(appWindow, win, args);
			extensionService.LoadExtensions(Resources.MergedDictionaries);
			win.Show();
		}

		void DsLoaderService_OnAppLoaded(object sender, EventArgs e) {
			dsLoaderService.OnAppLoaded -= DsLoaderService_OnAppLoaded;
			appWindow.AppLoaded = true;
			extensionService.OnAppLoaded();
			HandleAppArgs(args);
		}

		void HandleAppArgs(IAppCommandLineArgs appArgs) {
			if (appArgs.Activate && appWindow.MainWindow.WindowState == WindowState.Minimized)
				WindowUtils.SetState(appWindow.MainWindow, WindowState.Normal);

			var decompiler = GetDecompiler(appArgs.Language);
			if (decompiler != null)
				decompilerService.Value.Decompiler = decompiler;

			if (appArgs.FullScreen != null)
				appWindow.MainWindow.IsFullScreen = appArgs.FullScreen.Value;

			if (appArgs.NewTab)
				documentTabService.Value.OpenEmptyTab();

			var files = appArgs.Filenames.ToArray();
			if (files.Length > 0)
				OpenDocumentsHelper.OpenDocuments(documentTabService.Value.DocumentTreeView, appWindow.MainWindow, files, false);

			// The files were lazily added to the treeview. Make sure they've been added to the TV
			// before we process the remaining command line args.
			if (files.Length > 0)
				Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() => HandleAppArgs2(appArgs)));
			else
				HandleAppArgs2(appArgs);
		}

		void HandleAppArgs2(IAppCommandLineArgs appArgs) {
			foreach (var handler in appCommandLineArgsHandlers.OrderBy(a => a.Value.Order))
				handler.Value.OnNewArgs(appArgs);
		}

		IDecompiler GetDecompiler(string language) {
			if (string.IsNullOrEmpty(language))
				return null;

			Guid guid;
			if (Guid.TryParse(language, out guid)) {
				var lang = decompilerService.Value.Find(guid);
				if (lang != null)
					return lang;
			}

			return decompilerService.Value.AllDecompilers.FirstOrDefault(a => StringComparer.OrdinalIgnoreCase.Equals(a.UniqueNameUI, language)) ??
				decompilerService.Value.AllDecompilers.FirstOrDefault(a => StringComparer.OrdinalIgnoreCase.Equals(a.GenericNameUI, language));
		}
	}
}
