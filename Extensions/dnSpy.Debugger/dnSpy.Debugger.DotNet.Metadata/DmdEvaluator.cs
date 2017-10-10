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
using System.Threading;

namespace dnSpy.Debugger.DotNet.Metadata {
	/// <summary>
	/// Executes methods and loads/stores fields
	/// </summary>
	public abstract class DmdEvaluator {
		/// <summary>
		/// Executes a method
		/// </summary>
		/// <param name="context">Evaluation context</param>
		/// <param name="method">Method to call</param>
		/// <param name="obj">Instance object or null if it's a constructor or a static method</param>
		/// <param name="parameters">Parameters passed to the method</param>
		/// <returns></returns>
		public abstract object Invoke(object context, DmdMethodBase method, object obj, object[] parameters);

		/// <summary>
		/// Loads a field
		/// </summary>
		/// <param name="context">Evaluation context</param>
		/// <param name="field">Field</param>
		/// <param name="obj">Instance object or null if it's a static field</param>
		/// <returns></returns>
		public abstract object LoadField(object context, DmdFieldInfo field, object obj);

		/// <summary>
		/// Stores a value in a field
		/// </summary>
		/// <param name="context">Evaluation context</param>
		/// <param name="field">Field</param>
		/// <param name="obj">Instance object or null if it's a static field</param>
		/// <param name="value">Value to store in the field</param>
		public abstract void StoreField(object context, DmdFieldInfo field, object obj, object value);

		/// <summary>
		/// Executes a method
		/// </summary>
		/// <param name="context">Evaluation context</param>
		/// <param name="method">Method to call</param>
		/// <param name="obj">Instance object or null if it's a constructor or a static method</param>
		/// <param name="parameters">Parameters passed to the method</param>
		/// <param name="callback">Notified when the method is complete</param>
		public abstract void Invoke(object context, DmdMethodBase method, object obj, object[] parameters, Action<object> callback);

		/// <summary>
		/// Loads a field
		/// </summary>
		/// <param name="context">Evaluation context</param>
		/// <param name="field">Field</param>
		/// <param name="obj">Instance object or null if it's a static field</param>
		/// <param name="callback">Notified when the method is complete</param>
		public abstract void LoadField(object context, DmdFieldInfo field, object obj, Action<object> callback);

		/// <summary>
		/// Stores a value in a field
		/// </summary>
		/// <param name="context">Evaluation context</param>
		/// <param name="field">Field</param>
		/// <param name="obj">Instance object or null if it's a static field</param>
		/// <param name="value">Value to store in the field</param>
		/// <param name="callback">Notified when the method is complete</param>
		public abstract void StoreField(object context, DmdFieldInfo field, object obj, object value, Action callback);
	}
}
