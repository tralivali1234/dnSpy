﻿/*
    Copyright (C) 2014-2017 de4dot@gmail.com

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
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using dnSpy.Contracts.App;
using dnSpy.Contracts.Debugger;
using dnSpy.Contracts.Debugger.Breakpoints.Code;
using dnSpy.Contracts.Debugger.CallStack;
using dnSpy.Contracts.Debugger.Code;
using dnSpy.Contracts.Debugger.Steppers;
using dnSpy.Contracts.Documents;
using dnSpy.Contracts.Documents.Tabs;
using dnSpy.Debugger.Breakpoints.Code.TextEditor;
using dnSpy.Debugger.Code.TextEditor;
using dnSpy.Debugger.Dialogs.AttachToProcess;
using dnSpy.Debugger.Exceptions;
using dnSpy.Debugger.Properties;

namespace dnSpy.Debugger.DbgUI {
	[Export(typeof(Debugger))]
	[Export(typeof(IDbgManagerStartListener))]
	sealed class DebuggerImpl : Debugger, IDbgManagerStartListener {
		readonly Lazy<IMessageBoxService> messageBoxService;
		readonly Lazy<IAppWindow> appWindow;
		readonly Lazy<IDocumentTabService> documentTabService;
		readonly Lazy<DbgManager> dbgManager;
		readonly Lazy<StartDebuggingOptionsProvider> startDebuggingOptionsProvider;
		readonly Lazy<ShowAttachToProcessDialog> showAttachToProcessDialog;
		readonly Lazy<TextViewBreakpointService> textViewBreakpointService;
		readonly Lazy<DbgCodeBreakpointsService> dbgCodeBreakpointsService;
		readonly Lazy<DbgCallStackService> dbgCallStackService;
		readonly Lazy<ReferenceNavigatorService> referenceNavigatorService;
		readonly Lazy<DbgTextViewCodeLocationService> dbgTextViewCodeLocationService;
		readonly Lazy<DbgExceptionFormatterService> dbgExceptionFormatterService;
		readonly DebuggerSettings debuggerSettings;

		public override bool IsDebugging => dbgManager.Value.IsDebugging;

		[ImportingConstructor]
		DebuggerImpl(Lazy<IMessageBoxService> messageBoxService, Lazy<IAppWindow> appWindow, Lazy<IDocumentTabService> documentTabService, Lazy<DbgManager> dbgManager, Lazy<StartDebuggingOptionsProvider> startDebuggingOptionsProvider, Lazy<ShowAttachToProcessDialog> showAttachToProcessDialog, Lazy<TextViewBreakpointService> textViewBreakpointService, Lazy<DbgCodeBreakpointsService> dbgCodeBreakpointsService, Lazy<DbgCallStackService> dbgCallStackService, Lazy<ReferenceNavigatorService> referenceNavigatorService, Lazy<DbgTextViewCodeLocationService> dbgTextViewCodeLocationService, Lazy<DbgExceptionFormatterService> dbgExceptionFormatterService, DebuggerSettings debuggerSettings) {
			this.messageBoxService = messageBoxService;
			this.appWindow = appWindow;
			this.documentTabService = documentTabService;
			this.dbgManager = dbgManager;
			this.startDebuggingOptionsProvider = startDebuggingOptionsProvider;
			this.showAttachToProcessDialog = showAttachToProcessDialog;
			this.textViewBreakpointService = textViewBreakpointService;
			this.dbgCodeBreakpointsService = dbgCodeBreakpointsService;
			this.dbgCallStackService = dbgCallStackService;
			this.referenceNavigatorService = referenceNavigatorService;
			this.dbgTextViewCodeLocationService = dbgTextViewCodeLocationService;
			this.dbgExceptionFormatterService = dbgExceptionFormatterService;
			this.debuggerSettings = debuggerSettings;
		}

		public override string GetCurrentExecutableFilename() => startDebuggingOptionsProvider.Value.GetCurrentExecutableFilename();

		public override bool CanStartWithoutDebugging => GetCurrentExecutableFilename() != null;
		public override void StartWithoutDebugging() {
			var filename = GetCurrentExecutableFilename();
			if (!File.Exists(filename))
				return;
			try {
				Process.Start(filename);
			}
			catch (Exception ex) {
				messageBoxService.Value.Show(string.Format(dnSpy_Debugger_Resources.Error_StartWithoutDebuggingCouldNotStart, filename, ex.Message));
			}
		}

		public override bool CanDebugProgram => !showingDebugProgramDlgBox;
		public override void DebugProgram(bool pauseAtEntryPoint) {
			if (!CanDebugProgram)
				return;
			var breakKind = pauseAtEntryPoint ? PredefinedBreakKinds.EntryPoint : null;
			showingDebugProgramDlgBox = true;
			var options = startDebuggingOptionsProvider.Value.GetStartDebuggingOptions(breakKind);
			showingDebugProgramDlgBox = false;
			if (options == null)
				return;

			var errMsg = dbgManager.Value.Start(options);
			if (errMsg != null)
				messageBoxService.Value.Show(errMsg);
		}
		bool showingDebugProgramDlgBox;

		public override bool CanAttachProgram => true;
		public override void AttachProgram() => showAttachToProcessDialog.Value.Attach();

		bool CanExecutePauseCommand => dbgManager.Value.IsDebugging && dbgManager.Value.IsRunning != true;
		bool CanStepCommand => dbgManager.Value.CurrentThread.Current?.Process?.State == DbgProcessState.Paused;
		bool CanStepProcessCommand => CanStepCommand && !debuggerSettings.BreakAllProcesses;
		bool CanExecuteRunningCommand => dbgManager.Value.IsDebugging && dbgManager.Value.IsRunning != false;
		bool CanExecutePauseOrRunningCommand => dbgManager.Value.IsDebugging;

		public override bool CanContinue => CanExecutePauseCommand;
		public override void Continue() => dbgManager.Value.RunAll();
		public override bool CanBreakAll => CanExecuteRunningCommand;
		public override void BreakAll() => dbgManager.Value.BreakAll();
		public override bool CanStopDebugging => CanExecutePauseOrRunningCommand;
		public override void StopDebugging() => dbgManager.Value.StopDebuggingAll();

		public override bool CanDetachAll => CanExecutePauseOrRunningCommand;
		public override void DetachAll() {
			if (!dbgManager.Value.CanDetachWithoutTerminating) {
				//TODO: Show a msg box
			}
			dbgManager.Value.DetachAll();
		}

		public override bool CanTerminateAll => CanExecutePauseOrRunningCommand;
		public override void TerminateAll() => dbgManager.Value.TerminateAll();
		public override bool CanRestart => CanExecutePauseOrRunningCommand && dbgManager.Value.CanRestart;
		public override void Restart() => dbgManager.Value.Restart();

		public override bool CanShowNextStatement => CanExecutePauseCommand;
		public override void ShowNextStatement() {
			var info = GetCurrentStatementLocation();
			if (info.location != null) {
				referenceNavigatorService.Value.GoTo(info.location);
				dbgCallStackService.Value.ActiveFrameIndex = info.frameIndex;
			}
		}

		(DbgCodeLocation location, int frameIndex) GetCurrentStatementLocation() {
			var frames = dbgCallStackService.Value.Frames.Frames;
			for (int i = 0; i < frames.Count; i++) {
				var location = frames[i].Location;
				if (location != null)
					return (location, i);
			}
			return (null, -1);
		}

		public override bool CanSetNextStatement => CanExecutePauseCommand && dbgManager.Value.CurrentThread.Current != null;
		public override void SetNextStatement() {
			if (!CanSetNextStatement)
				return;
			using (var res = GetCurrentTextViewStatementLocation()) {
				if (res.Location != null)
					dbgManager.Value.CurrentThread.Current?.SetIP(res.Location);
			}
		}

		struct TextViewStatementLocationResult : IDisposable {
			readonly Lazy<DbgManager> dbgManager;
			readonly List<DbgCodeLocation> allLocations;

			public DbgCodeLocation Location { get; }

			public TextViewStatementLocationResult(Lazy<DbgManager> dbgManager, List<DbgCodeLocation> allLocations, DbgCodeLocation location) {
				this.dbgManager = dbgManager;
				this.allLocations = allLocations;
				Location = location;
			}

			public void Dispose() {
				if (allLocations != null && allLocations.Count > 0)
					dbgManager.Value.Close(allLocations);
			}
		}

		TextViewStatementLocationResult GetCurrentTextViewStatementLocation() {
			var tab = documentTabService.Value.ActiveTab;
			if (tab == null)
				return default;
			var documentViewer = tab.TryGetDocumentViewer();
			if (documentViewer == null)
				return default;
			var textView = documentViewer.TextView;

			var allLocations = new List<DbgCodeLocation>();
			foreach (var res in dbgTextViewCodeLocationService.Value.CreateLocation(tab, textView, textView.Caret.Position.VirtualBufferPosition)) {
				allLocations.AddRange(res.Locations);
				if (res.Locations.Length != 0) {
					var location = res.Locations[0];
					if (res.Locations.Length > 1) {
						var thread = dbgManager.Value.CurrentThread.Current;
						foreach (var loc in res.Locations) {
							if (thread?.CanSetIP(loc) == true) {
								location = loc;
								break;
							}
						}
					}
					return new TextViewStatementLocationResult(dbgManager, allLocations, location);
				}
			}

			return new TextViewStatementLocationResult(dbgManager, allLocations, null);
		}

		public override bool CanStepInto => CanStepCommand;
		public override void StepInto() => Step(DbgStepKind.StepInto);

		public override bool CanStepOver => CanStepCommand;
		public override void StepOver() => Step(DbgStepKind.StepOver);

		public override bool CanStepOut => CanStepCommand;
		public override void StepOut() => Step(DbgStepKind.StepOut);

		public override bool CanStepIntoCurrentProcess => CanStepProcessCommand;
		public override void StepIntoCurrentProcess() => Step(DbgStepKind.StepIntoProcess);

		public override bool CanStepOverCurrentProcess => CanStepProcessCommand;
		public override void StepOverCurrentProcess() => Step(DbgStepKind.StepOverProcess);

		public override bool CanStepOutCurrentProcess => CanStepProcessCommand;
		public override void StepOutCurrentProcess() => Step(DbgStepKind.StepOutProcess);

		void Step(DbgStepKind step) {
			var thread = dbgManager.Value.CurrentThread.Current;
			if (thread == null)
				return;
			thread.CreateStepper().Step(step, autoClose: true);
		}

		public override bool CanToggleCreateBreakpoint => textViewBreakpointService.Value.CanToggleCreateBreakpoint;
		public override void ToggleCreateBreakpoint() => textViewBreakpointService.Value.ToggleCreateBreakpoint();
		public override ToggleCreateBreakpointKind GetToggleCreateBreakpointKind() => textViewBreakpointService.Value.GetToggleCreateBreakpointKind();

		public override bool CanToggleEnableBreakpoint => textViewBreakpointService.Value.CanToggleEnableBreakpoint;
		public override void ToggleEnableBreakpoint() => textViewBreakpointService.Value.ToggleEnableBreakpoint();
		public override ToggleEnableBreakpointKind GetToggleEnableBreakpointKind() => textViewBreakpointService.Value.GetToggleEnableBreakpointKind();

		public override bool CanDeleteAllBreakpoints => dbgCodeBreakpointsService.Value.VisibleBreakpoints.Any();
		public override void DeleteAllBreakpointsAskUser() {
			var res = messageBoxService.Value.ShowIgnorableMessage(new Guid("37250D26-E844-49F4-904B-29600B90476C"), dnSpy_Debugger_Resources.AskDeleteAllBreakpoints, MsgBoxButton.Yes | MsgBoxButton.No);
			if (res != null && res != MsgBoxButton.Yes)
				return;
			dbgCodeBreakpointsService.Value.Clear();
		}

		public override bool CanEnableAllBreakpoints => dbgCodeBreakpointsService.Value.VisibleBreakpoints.Any(a => !a.IsEnabled);
		public override void EnableAllBreakpoints() => EnableAllBreakpoints(true);
		public override bool CanDisableAllBreakpoints => dbgCodeBreakpointsService.Value.VisibleBreakpoints.Any(a => a.IsEnabled);
		public override void DisableAllBreakpoints() => EnableAllBreakpoints(false);

		void EnableAllBreakpoints(bool enable) {
			dbgCodeBreakpointsService.Value.Modify(dbgCodeBreakpointsService.Value.VisibleBreakpoints.Where(a => a.IsEnabled != enable).Select(a => {
				var s = a.Settings;
				s.IsEnabled = enable;
				return new DbgCodeBreakpointAndSettings(a, s);
			}).ToArray());
		}

		public override bool CanContinueOrDegbugProgram => CanContinue || CanDebugProgram;
		public override void ContinueOrDegbugProgram() {
			if (CanContinue)
				Continue();
			else if (CanDebugProgram && !dbgManager.Value.IsDebugging)
				DebugProgram(pauseAtEntryPoint: false);
		}

		public override bool CanStepIntoOrDegbugProgram => CanStepInto || CanDebugProgram;
		public override void StepIntoOrDegbugProgram() {
			if (CanStepInto)
				StepInto();
			else if (CanDebugProgram && !dbgManager.Value.IsDebugging)
				DebugProgram(pauseAtEntryPoint: true);
		}

		public override bool CanStepOverOrDegbugProgram => CanStepOver || CanDebugProgram;
		public override void StepOverOrDegbugProgram() {
			if (CanStepOver)
				StepOver();
			else if (CanDebugProgram && !dbgManager.Value.IsDebugging)
				DebugProgram(pauseAtEntryPoint: true);
		}

		void IDbgManagerStartListener.OnStart(DbgManager dbgManager) {
			dbgManager.IsDebuggingChanged += DbgManager_IsDebuggingChanged;
			dbgManager.IsRunningChanged += DbgManager_IsRunningChanged;
			dbgManager.Message += DbgManager_Message;
		}

		void DbgManager_Message(object sender, DbgMessageEventArgs e) {
			switch (e.Kind) {
			case DbgMessageKind.SetIPComplete:
				var ep = (DbgMessageSetIPCompleteEventArgs)e;
				if (ep.Error != null)
					UI(() => ShowError_UI(ep.Error));
				break;

			case DbgMessageKind.UserMessage:
				var um = (DbgMessageUserMessageEventArgs)e;
				UI(() => ShowError_UI(um.Message));
				break;

			case DbgMessageKind.ExceptionThrown:
				var exm = (DbgMessageExceptionThrownEventArgs)e;
				if (exm.Exception.IsUnhandled) {
					exm.Pause = true;
					UI(() => ShowUnhandledException_UI(exm));
				}
				break;
			}
		}

		void UI(Action callback) {
			var dispatcher = appWindow.Value.MainWindow.Dispatcher;
			if (!dispatcher.HasShutdownStarted && !dispatcher.HasShutdownFinished)
				dispatcher.BeginInvoke(DispatcherPriority.Send, callback);
		}

		void ShowUnhandledException_UI(DbgMessageExceptionThrownEventArgs exm) {
			var sb = new StringBuilder();
			sb.AppendLine(string.Format(dnSpy_Debugger_Resources.UnhandledExceptionMessage_ProcessName_ProcessId, exm.Exception.Process.Name, exm.Exception.Process.Id));
			sb.AppendLine();
			sb.AppendLine(string.Format(dnSpy_Debugger_Resources.ExceptionName, dbgExceptionFormatterService.Value.ToString(exm.Exception.Id)));
			sb.AppendLine();
			sb.AppendLine(string.Format(dnSpy_Debugger_Resources.ExceptionMessage, exm.Exception.Message ?? dnSpy_Debugger_Resources.ExceptionMessageIsNull));
			ShowError_UI(sb.ToString());
		}

		void ShowError_UI(string error) => messageBoxService.Value.Show(error);

		void DbgManager_IsDebuggingChanged(object sender, EventArgs e) {
			var dbgManager = (DbgManager)sender;
			UI(() => {
				var newIsDebugging = dbgManager.IsDebugging;
				if (newIsDebugging == oldIsDebugging)
					return;
				oldIsDebugging = newIsDebugging;
				Application.Current.Resources["IsDebuggingKey"] = newIsDebugging;
				if (newIsDebugging) {
					appWindow.Value.StatusBar.Open();
					SetRunningStatusMessage();
					appWindow.Value.AddTitleInfo(dnSpy_Debugger_Resources.AppTitle_Debugging);
				}
				else {
					appWindow.Value.StatusBar.Close();
					appWindow.Value.RemoveTitleInfo(dnSpy_Debugger_Resources.AppTitle_Debugging);
				}
				appWindow.Value.RefreshToolBar();
			});
		}
		bool oldIsDebugging;

		void SetRunningStatusMessage() => appWindow.Value.StatusBar.Show(dnSpy_Debugger_Resources.StatusBar_Running);

		void DbgManager_IsRunningChanged(object sender, EventArgs e) {
			var dbgManager = (DbgManager)sender;
			UI(() => {
				var newIsRunning = dbgManager.IsRunning;
				if (newIsRunning == oldIsRunning)
					return;
				oldIsRunning = newIsRunning;
				CommandManager.InvalidateRequerySuggested();
			});
		}
		bool? oldIsRunning;
	}
}
