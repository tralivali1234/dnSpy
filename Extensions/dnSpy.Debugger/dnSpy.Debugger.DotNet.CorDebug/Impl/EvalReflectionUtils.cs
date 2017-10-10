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

using dndbg.Engine;

namespace dnSpy.Debugger.DotNet.CorDebug.Impl {
	static class EvalReflectionUtils {
		public static bool ReadExceptionMessage(CorValue thisRef, out string result) {
			result = null;
			if (thisRef == null)
				return false;
			return ReadValue(thisRef, "_message", out result);
		}

		public static bool ReadValue<T>(CorValue thisRef, string fieldName, out T value) {
			value = default;
			if (thisRef == null)
				return false;

			var val = thisRef.GetFieldValue(fieldName);
			if (val == null)
				return false;

			var dval = val.Value;
			if (!dval.IsValid)
				return false;
			if (!(dval.Value is T || Equals(default(T), dval.Value)))
				return false;

			value = (T)dval.Value;
			return true;
		}
	}
}
