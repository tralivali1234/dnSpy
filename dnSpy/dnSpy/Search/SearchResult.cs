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
using System.Diagnostics;
using dnlib.DotNet;
using dnSpy.Contracts.Decompiler;
using dnSpy.Contracts.Documents;
using dnSpy.Contracts.Documents.TreeView;
using dnSpy.Contracts.Documents.TreeView.Resources;
using dnSpy.Contracts.Images;
using dnSpy.Contracts.MVVM;
using dnSpy.Contracts.Search;
using dnSpy.Contracts.Text;
using dnSpy.Contracts.Text.Classification;
using dnSpy.Contracts.TreeView;

namespace dnSpy.Search {
	class SearchResult : ViewModelBase, ISearchResult, IMDTokenNode, IComparable<ISearchResult> {
		IMDTokenProvider IMDTokenNode.Reference => Reference2;
		IMDTokenProvider Reference2 => Object as IMDTokenProvider;

		public object Reference {
			get {
				var ns = Object as string;
				if (ns != null)
					return new NamespaceRef(Document, ns);
				var node = Object as DocumentTreeNodeData;
				if (node != null)
					return node;
				return Reference2;
			}
		}

		public SearchResultContext Context { get; set; }
		public object Object { get; set; }
		public object NameObject { get; set; }
		public ImageReference ObjectImageReference { get; set; }
		public object LocationObject { get; set; }
		public ImageReference LocationImageReference { get; set; }
		public IDsDocument Document { get; set; }
		public object ObjectInfo { get; set; }

		public void RefreshUI() {
			OnPropertyChanged(nameof(NameUI));
			OnPropertyChanged(nameof(LocationUI));
			OnPropertyChanged(nameof(ToolTip));
		}

		public string ToolTip {
			get {
				var dsDocument = Document;
				if (dsDocument == null)
					return null;
				var module = dsDocument.ModuleDef;
				if (module == null)
					return dsDocument.Filename;
				if (!string.IsNullOrWhiteSpace(module.Location))
					return module.Location;
				if (!string.IsNullOrWhiteSpace(module.Name))
					return module.Name;
				if (module.Assembly != null && !string.IsNullOrWhiteSpace(module.Assembly.Name))
					return module.Assembly.Name;
				return null;
			}
		}

		public object NameUI => CreateUI(NameObject, false);
		public object LocationUI => CreateUI(LocationObject, true);

		public override string ToString() {
			var output = new StringBuilderTextColorOutput();
			CreateUI(output, NameObject, false);
			return output.ToString();
		}

		static class Cache {
			static readonly TextClassifierTextColorWriter writer = new TextClassifierTextColorWriter();
			public static TextClassifierTextColorWriter GetWriter() => writer;
			public static void FreeWriter(TextClassifierTextColorWriter writer) { writer.Clear(); }
		}

		object CreateUI(object o, bool includeNamespace) {
			var writer = Cache.GetWriter();
			try {
				CreateUI(writer, o, includeNamespace);
				var context = new TextClassifierContext(writer.Text, string.Empty, Context.SyntaxHighlight, writer.Colors);
				return Context.TextElementProvider.CreateTextElement(Context.ClassificationFormatMap, context, ContentTypes.Search, TextElementFlags.FilterOutNewLines);
			}
			finally {
				Cache.FreeWriter(writer);
			}
		}

		void CreateUI(ITextColorWriter output, object o, bool includeNamespace) {
			var ns = o as NamespaceSearchResult;
			if (ns != null) {
				output.WriteNamespace(ns.Namespace);
				return;
			}

			var td = o as TypeDef;
			if (td != null) {
				Debug.Assert(Context.Decompiler != null);
				Context.Decompiler.WriteType(output, td, includeNamespace);
				return;
			}

			var md = o as MethodDef;
			if (md != null) {
				output.Write(Context.Decompiler.MetadataTextColorProvider.GetColor(md), IdentifierEscaper.Escape(md.Name));
				return;
			}

			var fd = o as FieldDef;
			if (fd != null) {
				output.Write(Context.Decompiler.MetadataTextColorProvider.GetColor(fd), IdentifierEscaper.Escape(fd.Name));
				return;
			}

			var pd = o as PropertyDef;
			if (pd != null) {
				output.Write(Context.Decompiler.MetadataTextColorProvider.GetColor(pd), IdentifierEscaper.Escape(pd.Name));
				return;
			}

			var ed = o as EventDef;
			if (ed != null) {
				output.Write(Context.Decompiler.MetadataTextColorProvider.GetColor(ed), IdentifierEscaper.Escape(ed.Name));
				return;
			}

			var asm = o as AssemblyDef;
			if (asm != null) {
				output.Write(asm);
				return;
			}

			var mod = o as ModuleDef;
			if (mod != null) {
				output.WriteModule(mod.FullName);
				return;
			}

			var asmRef = o as AssemblyRef;
			if (asmRef != null) {
				output.Write(asmRef);
				return;
			}

			var modRef = o as ModuleRef;
			if (modRef != null) {
				output.WriteModule(modRef.FullName);
				return;
			}

			var paramDef = o as ParamDef;
			if (paramDef != null) {
				output.Write(BoxedTextColor.Parameter, IdentifierEscaper.Escape(paramDef.Name));
				return;
			}

			// non-.NET file
			var document = o as IDsDocument;
			if (document != null) {
				output.Write(BoxedTextColor.Text, document.GetShortName());
				return;
			}

			var resNode = o as ResourceNode;
			if (resNode != null) {
				output.WriteFilename(resNode.Name);
				return;
			}

			var resElNode = o as ResourceElementNode;
			if (resElNode != null) {
				output.WriteFilename(resElNode.Name);
				return;
			}

			var em = o as ErrorMessage;
			if (em != null) {
				output.Write(em.Color, em.Text);
				return;
			}

			Debug.Assert(o == null);
		}

		public static SearchResult CreateMessage(SearchResultContext context, string msg, object color, bool first) =>
			new MessageSearchResult(msg, color, first) { Context = context };

		public int CompareTo(ISearchResult other) {
			if (other == null)
				return -1;
			int o1 = GetOrder(this);
			int o2 = GetOrder(other);
			int d = o1.CompareTo(o2);
			if (d != 0)
				return d;
			var sr = other as SearchResult;
			return StringComparer.CurrentCultureIgnoreCase.Compare(GetCompareString(), sr == null ? other.ToString() : sr.GetCompareString());
		}

		static int GetOrder(ISearchResult other) {
			var mr = other as MessageSearchResult;
			return mr == null ? 0 : mr.Order;
		}

		string GetCompareString() => compareString ?? (compareString = ToString());
		string compareString = null;
	}

	sealed class ErrorMessage {
		public string Text => msg;
		readonly string msg;

		public object Color => color;
		readonly object color;

		public ErrorMessage(string msg, object color) {
			this.msg = msg;
			this.color = color;
		}
	}

	sealed class MessageSearchResult : SearchResult {
		readonly string msg;
		public int Order => order;
		readonly int order;

		public MessageSearchResult(string msg, object color, bool first) {
			this.msg = msg;
			NameObject = new ErrorMessage(msg, color);
			order = first ? int.MinValue : int.MaxValue;
		}

		public override string ToString() => msg;
	}
}
