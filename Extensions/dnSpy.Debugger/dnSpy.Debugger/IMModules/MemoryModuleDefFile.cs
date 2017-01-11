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
using System.Diagnostics;
using dndbg.Engine;
using dnlib.DotNet;
using dnlib.PE;
using dnSpy.Contracts.Documents;
using dnSpy.Contracts.Metadata;
using dnSpy.Contracts.Utilities;
using dnSpy.Debugger.Memory;

namespace dnSpy.Debugger.IMModules {
	/// <summary>
	/// A class that reads the module from the debugged process' address space.
	/// </summary>
	sealed class MemoryModuleDefFile : DsDotNetDocumentBase, IModuleIdHolder {
		sealed class MyKey : IDsDocumentNameKey {
			readonly DnProcess process;
			readonly ulong address;

			public MyKey(DnProcess process, ulong address) {
				this.process = process;
				this.address = address;
			}

			public override bool Equals(object obj) {
				var o = obj as MyKey;
				return o != null && process == o.process && address == o.address;
			}

			public override int GetHashCode() => process.GetHashCode() ^ (int)address ^ (int)(address >> 32);
		}

		public ModuleId ModuleId {
			get {
				if (!isInMemory)
					return ModuleId.CreateFromFile(ModuleDef);
				return ModuleId.CreateInMemory(ModuleDef);
			}
		}

		public override IDsDocumentNameKey Key => CreateKey(Process, Address);
		public override DsDocumentInfo? SerializedDocument => null;
		public bool AutoUpdateMemory { get; }
		public DnProcess Process { get; }
		public ulong Address { get; }
		public override bool IsActive => !Process.HasExited;

		readonly SimpleProcessReader simpleProcessReader;
		readonly byte[] data;
		readonly bool isInMemory;

		MemoryModuleDefFile(SimpleProcessReader simpleProcessReader, DnProcess process, ulong address, byte[] data, bool isInMemory, ModuleDef module, bool loadSyms, bool autoUpdateMemory)
			: base(module, loadSyms) {
			this.simpleProcessReader = simpleProcessReader;
			Process = process;
			Address = address;
			this.data = data;
			this.isInMemory = isInMemory;
			AutoUpdateMemory = autoUpdateMemory;
		}

		public static IDsDocumentNameKey CreateKey(DnProcess process, ulong address) => new MyKey(process, address);

		protected override List<IDsDocument> CreateChildren() {
			var list = new List<IDsDocument>();
			if (files != null) {
				list.AddRange(files);
				files = null;
			}
			return list;
		}
		List<MemoryModuleDefFile> files;

		public bool UpdateMemory() {
			if (Process.HasExited)
				return false;
			//TODO: Only compare the smallest possible region, eg. all MD and IL bodies. Don't include writable sects.
			var newData = new byte[data.Length];
			simpleProcessReader.Read(Process.CorProcess.Handle, Address, newData, 0, data.Length);
			if (Equals(data, newData))
				return false;
			Array.Copy(newData, data, data.Length);
			return true;
		}

		static bool Equals(byte[] a, byte[] b) {
			if (a == b)
				return true;
			if (a == null || b == null)
				return false;
			if (a.Length != b.Length)
				return false;
			for (int i = 0; i < a.Length; i++) {
				if (a[i] != b[i])
					return false;
			}
			return true;
		}

		public static MemoryModuleDefFile CreateAssembly(SimpleProcessReader simpleProcessReader, List<MemoryModuleDefFile> files) {
			var manifest = files[0];
			var file = new MemoryModuleDefFile(simpleProcessReader, manifest.Process, manifest.Address, manifest.data, manifest.isInMemory, manifest.ModuleDef, false, manifest.AutoUpdateMemory);
			file.files = new List<MemoryModuleDefFile>(files);
			return file;
		}

		public static MemoryModuleDefFile Create(SimpleProcessReader simpleProcessReader, DnModule dnModule, bool loadSyms) {
			Debug.Assert(!dnModule.IsDynamic);
			Debug.Assert(dnModule.Address != 0);
			ulong address = dnModule.Address;
			var process = dnModule.Process;
			var data = new byte[dnModule.Size];
			string location = dnModule.IsInMemory ? string.Empty : dnModule.Name;

			simpleProcessReader.Read(process.CorProcess.Handle, address, data, 0, data.Length);

			var peImage = new PEImage(data, GetImageLayout(dnModule), true);
			var module = ModuleDefMD.Load(peImage);
			module.Location = location;
			bool autoUpdateMemory = false;//TODO: Init to default value
			if (GacInfo.IsGacPath(dnModule.Name))
				autoUpdateMemory = false;	// GAC files are not likely to decrypt methods in memory
			return new MemoryModuleDefFile(simpleProcessReader, process, address, data, dnModule.IsInMemory, module, loadSyms, autoUpdateMemory);
		}

		static ImageLayout GetImageLayout(DnModule module) {
			Debug.Assert(!module.IsDynamic);
			return module.IsInMemory ? ImageLayout.File : ImageLayout.Memory;
		}
	}
}
