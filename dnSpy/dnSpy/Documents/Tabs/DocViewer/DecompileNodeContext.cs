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
using System.Windows.Threading;
using dnSpy.Contracts.Decompiler;
using dnSpy.Contracts.Documents.Tabs.DocViewer;
using Microsoft.VisualStudio.Utilities;

namespace dnSpy.Documents.Tabs.DocViewer {
	sealed class DecompileNodeContext : IDecompileNodeContext {
		public DecompilationContext DecompilationContext { get; }
		public IDecompiler Decompiler { get; }
		public IDecompilerOutput Output { get; }
		public IDocumentWriterService DocumentWriterService { get; }
		public string FileExtension { get; set; }
		public IContentType ContentType { get; set; }
		public string ContentTypeString { get; set; }

		readonly Dispatcher dispatcher;

		public DecompileNodeContext(DecompilationContext decompilationContext, IDecompiler decompiler, IDecompilerOutput output, IDocumentWriterService documentWriterService, Dispatcher dispatcher) {
			if (decompilationContext == null)
				throw new ArgumentNullException(nameof(decompilationContext));
			if (decompiler == null)
				throw new ArgumentNullException(nameof(decompiler));
			if (output == null)
				throw new ArgumentNullException(nameof(output));
			if (documentWriterService == null)
				throw new ArgumentNullException(nameof(documentWriterService));
			if (dispatcher == null)
				throw new ArgumentNullException(nameof(dispatcher));
			DecompilationContext = decompilationContext;
			Decompiler = decompiler;
			Output = output;
			DocumentWriterService = documentWriterService;
			this.dispatcher = dispatcher;
		}

		public T UIThread<T>(Func<T> func) {
			if (func == null)
				throw new ArgumentNullException(nameof(func));
			if (dispatcher.CheckAccess())
				return func();

			return (T)dispatcher.Invoke(DispatcherPriority.Send, func);
		}
	}
}
