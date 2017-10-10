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

using System.Diagnostics;
using dnSpy.Contracts.AsmEditor.Compiler;
using dnSpy.Roslyn.Shared.Documentation;
using dnSpy.Roslyn.Shared.Utilities;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Emit;

namespace dnSpy.Roslyn.Shared.Compiler {
	static class Extensions {
		public static Platform ToPlatform(this TargetPlatform platform) {
			switch (platform) {
			case TargetPlatform.AnyCpu:					return Platform.AnyCpu;
			case TargetPlatform.X86:					return Platform.X86;
			case TargetPlatform.X64:					return Platform.X64;
			case TargetPlatform.Itanium:				return Platform.Itanium;
			case TargetPlatform.AnyCpu32BitPreferred:	return Platform.AnyCpu32BitPreferred;
			case TargetPlatform.Arm:					return Platform.Arm;
			default:
				Debug.Fail($"Unknown platform: {platform}");
				return Platform.AnyCpu;
			}
		}

		public static MetadataReference CreateMetadataReference(this CompilerMetadataReference mdRef, IRoslynDocumentationProviderFactory docFactory) {
			var docProvider = docFactory.TryCreate(mdRef.Filename);
			if (mdRef.IsAssemblyReference)
				return MetadataReference.CreateFromImage(ImmutableArrayUtilities<byte>.ToImmutableArray(mdRef.Data), MetadataReferenceProperties.Assembly, docProvider, mdRef.Filename);
			var moduleMetadata = ModuleMetadata.CreateFromImage(ImmutableArrayUtilities<byte>.ToImmutableArray(mdRef.Data));
			return moduleMetadata.GetReference(docProvider, mdRef.Filename);
		}

		public static DebugFileFormat ToDebugFileFormat(this DebugInformationFormat format) {
			switch (format) {
			case 0:										return DebugFileFormat.None;
			case DebugInformationFormat.Pdb:			return DebugFileFormat.Pdb;
			case DebugInformationFormat.PortablePdb:	return DebugFileFormat.PortablePdb;
			case DebugInformationFormat.Embedded:		return DebugFileFormat.Embedded;
			default:
				Debug.Fail($"Unknown debug info format: {format}");
				return DebugFileFormat.None;
			}
		}
	}
}
