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
using System.Collections.ObjectModel;

namespace dnSpy.Contracts.Debugger {
	/// <summary>
	/// A runtime in a process
	/// </summary>
	public abstract class DbgRuntime : DbgObject {
		/// <summary>
		/// Gets the process
		/// </summary>
		public abstract DbgProcess Process { get; }

		/// <summary>
		/// Gets the process unique runtime id. There must be exactly one such id per process.
		/// </summary>
		public abstract RuntimeId Id { get; }

		/// <summary>
		/// Gets the runtime GUID, see <see cref="PredefinedDbgRuntimeGuids"/>
		/// </summary>
		public abstract Guid Guid { get; }

		/// <summary>
		/// Gets the runtime kind GUID, see <see cref="PredefinedDbgRuntimeKindGuids"/>
		/// </summary>
		public abstract Guid RuntimeKindGuid { get; }

		/// <summary>
		/// Gets the runtime name
		/// </summary>
		public abstract string Name { get; }

		/// <summary>
		/// Gets all runtime tags
		/// </summary>
		public abstract ReadOnlyCollection<string> Tags { get; }

		/// <summary>
		/// Gets the runtime object created by the debug engine
		/// </summary>
		public abstract DbgInternalRuntime InternalRuntime { get; }

		/// <summary>
		/// Gets all app domains
		/// </summary>
		public abstract DbgAppDomain[] AppDomains { get; }

		/// <summary>
		/// Raised when <see cref="AppDomains"/> is changed
		/// </summary>
		public abstract event EventHandler<DbgCollectionChangedEventArgs<DbgAppDomain>> AppDomainsChanged;

		/// <summary>
		/// Gets all modules
		/// </summary>
		public abstract DbgModule[] Modules { get; }

		/// <summary>
		/// Raised when <see cref="Modules"/> is changed
		/// </summary>
		public abstract event EventHandler<DbgCollectionChangedEventArgs<DbgModule>> ModulesChanged;

		/// <summary>
		/// Gets all threads
		/// </summary>
		public abstract DbgThread[] Threads { get; }

		/// <summary>
		/// Raised when <see cref="Threads"/> is changed
		/// </summary>
		public abstract event EventHandler<DbgCollectionChangedEventArgs<DbgThread>> ThreadsChanged;

		/// <summary>
		/// Closes <paramref name="obj"/> just before the runtime continues (or when it gets closed if it never continues)
		/// </summary>
		/// <param name="obj">Object</param>
		public abstract void CloseOnContinue(DbgObject obj);

		/// <summary>
		/// Closes <paramref name="objs"/> just before the runtime continues (or when it gets closed if it never continues)
		/// </summary>
		/// <param name="objs">Objects</param>
		public abstract void CloseOnContinue(IEnumerable<DbgObject> objs);
	}
}
