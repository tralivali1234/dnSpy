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
using System.ComponentModel.Composition;
using dnlib.DotNet;
using dnSpy.Contracts.Debugger;
using dnSpy.Contracts.Debugger.DotNet.Code;
using dnSpy.Contracts.Debugger.DotNet.Metadata;
using dnSpy.Contracts.Decompiler;
using dnSpy.Contracts.Documents.Tabs;
using dnSpy.Contracts.Documents.Tabs.DocViewer;
using dnSpy.Contracts.Metadata;
using dnSpy.Debugger.DotNet.Metadata;
using dnSpy.Debugger.DotNet.UI;

namespace dnSpy.Debugger.DotNet.Code {
	[Export(typeof(DbgDotNetCodeRangeService))]
	sealed class DbgDotNetCodeRangeServiceImpl : DbgDotNetCodeRangeService {
		readonly UIDispatcher uiDispatcher;
		readonly DbgModuleIdProviderService dbgModuleIdProviderService;
		readonly DbgMetadataService dbgMetadataService;
		readonly Lazy<IDocumentTabService> documentTabService;
		readonly Lazy<DotNetReferenceNavigator> dotNetReferenceNavigator;

		[ImportingConstructor]
		DbgDotNetCodeRangeServiceImpl(UIDispatcher uiDispatcher, DbgModuleIdProviderService dbgModuleIdProviderService, DbgMetadataService dbgMetadataService, Lazy<IDocumentTabService> documentTabService, Lazy<DotNetReferenceNavigator> dotNetReferenceNavigator) {
			this.uiDispatcher = uiDispatcher;
			this.dbgModuleIdProviderService = dbgModuleIdProviderService;
			this.dbgMetadataService = dbgMetadataService;
			this.documentTabService = documentTabService;
			this.dotNetReferenceNavigator = dotNetReferenceNavigator;
		}

		void UI(Action callback) => uiDispatcher.UI(callback);

		public override void GetCodeRanges(DbgModule module, uint token, uint offset, Action<GetCodeRangeResult> callback) {
			if (module == null)
				throw new ArgumentNullException(nameof(module));
			if (callback == null)
				throw new ArgumentNullException(nameof(callback));
			UI(() => GetCodeRanges_UI(module, token, offset, callback));
		}

		void GetCodeRanges_UI(DbgModule module, uint token, uint offset, Action<GetCodeRangeResult> callback) {
			uiDispatcher.VerifyAccess();
			var ranges = TryGetCodeRanges_UI(module, token, offset);
			if (ranges != null)
				callback(new GetCodeRangeResult(ranges));
			else
				callback(new GetCodeRangeResult(false, Array.Empty<DbgCodeRange>()));
		}

		DbgCodeRange[] TryGetCodeRanges_UI(DbgModule module, uint token, uint offset) {
			uiDispatcher.VerifyAccess();
			var tab = documentTabService.Value.GetOrCreateActiveTab();
			var documentViewer = tab.TryGetDocumentViewer();
			var methodDebugService = documentViewer.GetMethodDebugService();
			var moduleId = dbgModuleIdProviderService.GetModuleId(module);
			if (moduleId == null)
				return null;

			uint refNavOffset;
			if (offset == EPILOG) {
				refNavOffset = DotNetReferenceNavigator.EPILOG;
				var mod = dbgMetadataService.TryGetMetadata(module, DbgLoadModuleOptions.AutoLoaded);
				if (mod?.ResolveToken(token) is MethodDef md && md.Body != null && md.Body.Instructions.Count > 0)
					offset = md.Body.Instructions[md.Body.Instructions.Count - 1].Offset;
				else
					return null;
			}
			else if (offset == PROLOG) {
				refNavOffset = DotNetReferenceNavigator.PROLOG;
				offset = 0;
			}
			else
				refNavOffset = offset;

			var key = new ModuleTokenId(moduleId.Value, token);
			var info = methodDebugService.TryGetMethodDebugInfo(key);
			if (info == null) {
				var md = dbgMetadataService.TryGetMetadata(module, DbgLoadModuleOptions.AutoLoaded);
				var mdMethod = md?.ResolveToken(token) as MethodDef;
				if (mdMethod == null)
					return null;

				tab.FollowReference(mdMethod);
				dotNetReferenceNavigator.Value.GoToLocation(tab, mdMethod, key, refNavOffset);
				documentViewer = tab.TryGetDocumentViewer();
				methodDebugService = documentViewer.GetMethodDebugService();
				info = methodDebugService.TryGetMethodDebugInfo(key);
				if (info == null)
					return null;
			}

			var sourceStatement = info.GetSourceStatementByCodeOffset(offset);
			uint[] ranges;
			if (sourceStatement == null)
				ranges = info.GetUnusedRanges();
			else
				ranges = info.GetRanges(sourceStatement.Value);

			if (ranges.Length == 0)
				return null;
			return CreateStepRanges(ranges);
		}

		static DbgCodeRange[] CreateStepRanges(uint[] ilSpans) {
			var stepRanges = new DbgCodeRange[ilSpans.Length / 2];
			if (stepRanges.Length == 0)
				return null;
			for (int i = 0; i < stepRanges.Length; i++)
				stepRanges[i] = new DbgCodeRange(ilSpans[i * 2], ilSpans[i * 2 + 1]);
			return stepRanges;
		}
	}
}
