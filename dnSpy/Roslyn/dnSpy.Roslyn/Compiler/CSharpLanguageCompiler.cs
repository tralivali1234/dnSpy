﻿/*
    Copyright (C) 2014-2018 de4dot@gmail.com

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
using dnlib.DotNet;
using dnSpy.Contracts.AsmEditor.Compiler;
using dnSpy.Contracts.Decompiler;
using dnSpy.Contracts.Images;
using dnSpy.Contracts.Settings.AppearanceCategory;
using dnSpy.Contracts.Text;
using dnSpy.Contracts.Text.Editor;
using dnSpy.Contracts.Text.Editor.Operations;
using dnSpy.Roslyn.Documentation;
using dnSpy.Roslyn.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace dnSpy.Roslyn.Compiler {
	[Export(typeof(ILanguageCompilerProvider))]
	sealed class CSharpLanguageCompilerProvider : RoslynLanguageCompilerProvider {
		public override ImageReference? Icon => DsImages.CSFileNode;
		public override Guid Language => DecompilerConstants.LANGUAGE_CSHARP;
		public override ILanguageCompiler Create(CompilationKind kind) => new CSharpLanguageCompiler(kind, codeEditorProvider, docFactory, roslynDocumentChangedService, textViewUndoManagerProvider);

		readonly ICodeEditorProvider codeEditorProvider;
		readonly IRoslynDocumentationProviderFactory docFactory;
		readonly IRoslynDocumentChangedService roslynDocumentChangedService;
		readonly ITextViewUndoManagerProvider textViewUndoManagerProvider;

		[ImportingConstructor]
		CSharpLanguageCompilerProvider(ICodeEditorProvider codeEditorProvider, IRoslynDocumentationProviderFactory docFactory, IRoslynDocumentChangedService roslynDocumentChangedService, ITextViewUndoManagerProvider textViewUndoManagerProvider) {
			this.codeEditorProvider = codeEditorProvider;
			this.docFactory = docFactory;
			this.roslynDocumentChangedService = roslynDocumentChangedService;
			this.textViewUndoManagerProvider = textViewUndoManagerProvider;
		}
	}

	sealed class CSharpLanguageCompiler : RoslynLanguageCompiler {
		protected override string TextViewRole => PredefinedDsTextViewRoles.RoslynCSharpCodeEditor;
		protected override string ContentType => ContentTypes.CSharpRoslyn;
		protected override string LanguageName => LanguageNames.CSharp;
		protected override CompilationOptions CompilationOptions => new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, allowUnsafe: true);
		protected override ParseOptions ParseOptions => new CSharpParseOptions(languageVersion: LanguageVersion.Latest);
		public override string FileExtension => ".cs";
		protected override string AppearanceCategory => AppearanceCategoryConstants.TextEditor;

		public CSharpLanguageCompiler(CompilationKind kind, ICodeEditorProvider codeEditorProvider, IRoslynDocumentationProviderFactory docFactory, IRoslynDocumentChangedService roslynDocumentChangedService, ITextViewUndoManagerProvider textViewUndoManagerProvider)
			: base(kind, codeEditorProvider, docFactory, roslynDocumentChangedService, textViewUndoManagerProvider) {
		}

		public override IEnumerable<string> GetRequiredAssemblyReferences(ModuleDef editedModule) => Array.Empty<string>();

		protected override string GetHelpUri(Diagnostic diagnostic) {
			string id = diagnostic.Id.ToLowerInvariant();
			// See https://github.com/dotnet/docs/tree/master/docs/csharp/misc
			const string URL = "https://docs.microsoft.com/dotnet/csharp/misc/";
			return URL + id;
		}
	}
}
