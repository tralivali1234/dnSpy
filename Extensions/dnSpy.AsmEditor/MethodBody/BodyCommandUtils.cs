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
using System.Linq;
using dnlib.DotNet;
using dnSpy.Contracts.Decompiler;
using dnSpy.Contracts.Documents.Tabs.DocViewer;
using dnSpy.Contracts.Menus;
using dnSpy.Contracts.Text;

namespace dnSpy.AsmEditor.MethodBody {
	static class BodyCommandUtils {
		public static IList<MethodSourceStatement> GetStatements(IMenuItemContext context) {
			if (context == null)
				return null;
			if (context.CreatorObject.Guid != new Guid(MenuConstants.GUIDOBJ_DOCUMENTVIEWERCONTROL_GUID))
				return null;
			var uiContext = context.Find<IDocumentViewer>();
			if (uiContext == null)
				return null;
			var pos = context.Find<TextEditorPosition>();
			if (pos == null)
				return null;
			return GetStatements(uiContext, pos.Position);
		}

		public static IList<MethodSourceStatement> GetStatements(IDocumentViewer documentViewer, int textPosition) {
			if (documentViewer == null)
				return null;
			var methodDebugService = documentViewer.GetMethodDebugService();
			var methodStatements = methodDebugService.FindByTextPosition(textPosition, sameMethod: true);
			return methodStatements.Count == 0 ? null : methodStatements;
		}

		public static uint[] GetInstructionOffsets(MethodDef method, IList<MethodSourceStatement> list) {
			if (method == null)
				return null;
			var body = method.Body;
			if (body == null)
				return null;

			var foundInstrs = new HashSet<uint>();
			// The instructions' offset field is assumed to be valid
			var instrs = body.Instructions.Select(a => a.Offset).ToArray();
			foreach (var binSpan in list.Select(a => a.Statement.BinSpan)) {
				int index = Array.BinarySearch(instrs, binSpan.Start);
				if (index < 0)
					continue;
				for (int i = index; i < instrs.Length; i++) {
					uint instrOffset = instrs[i];
					if (instrOffset >= binSpan.End)
						break;

					foundInstrs.Add(instrOffset);
				}
			}

			return foundInstrs.ToArray();
		}
	}
}
