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
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Emit;
using dndbg.COM.CorDebug;

namespace dndbg.Engine {
	struct CorValueResult : IEquatable<CorValueResult> {
		/// <summary>
		/// The value. Only valid if <see cref="IsValid"/> is true, else it shouldn't be used
		/// </summary>
		public readonly object Value;

		/// <summary>
		/// true if <see cref="Value"/> is valid
		/// </summary>
		public readonly bool IsValid;

		public CorValueResult(object value) {
			Value = value;
			IsValid = true;
		}

		public T Write<T>(T output, CorValue value, TypeFormatterFlags flags, Func<DnEval> getEval = null) where T : ITypeOutput {
			new TypeFormatter(output, flags, getEval).Write(value, this);
			return output;
		}

		public string ToString(CorValue value, TypeFormatterFlags flags) => Write(new StringBuilderTypeOutput(), value, flags).ToString();

		public bool Equals(CorValueResult other) {
			if (IsValid != other.IsValid)
				return false;
			if (!IsValid)
				return true;
			if (ReferenceEquals(Value, other.Value))
				return true;
			if (Value == null || other.Value == null)
				return false;
			if (Value.GetType() != other.Value.GetType())
				return false;
			return Value.Equals(other.Value);
		}

		public override bool Equals(object obj) => obj is CorValueResult && Equals((CorValueResult)obj);

		public override int GetHashCode() {
			if (!IsValid)
				return 0x12345678;
			return Value == null ? 0 : Value.GetHashCode();
		}

		public override string ToString() {
			if (!IsValid)
				return "<invalid>";
			if (Value == null)
				return "null";
			return Value.ToString();
		}
	}

	struct CorValueReader {
		public static CorValueResult ReadSimpleTypeValue(CorValue value) {
			if (value == null)
				return new CorValueResult();

			if (value.IsReference && value.ElementType == CorElementType.ByRef) {
				if (value.IsNull)
					return new CorValueResult(null);
				value = value.DereferencedValue;
				if (value == null)
					return new CorValueResult();
			}
			if (value.IsReference) {
				if (value.IsNull)
					return new CorValueResult(null);
				if (value.ElementType == CorElementType.Ptr || value.ElementType == CorElementType.FnPtr) {
					if (Utils.DebuggeeIntPtrSize == 4)
						return new CorValueResult((uint)value.ReferenceAddress);
					return new CorValueResult(value.ReferenceAddress);
				}
				value = value.DereferencedValue;
				if (value == null)
					return new CorValueResult();
			}
			if (value.IsBox) {
				value = value.BoxedValue;
				if (value == null)
					return new CorValueResult();
			}
			if (value.IsReference)
				return new CorValueResult();
			if (value.IsBox)
				return new CorValueResult();
			if (value.IsArray)
				return new CorValueResult();
			if (value.IsString)
				return new CorValueResult(value.String);

			var type = value.ExactType;
			if (type == null)
				return new CorValueResult();
			var vres = GetSimpleResult(value, type.TryGetPrimitiveType(), type);
			return vres ?? new CorValueResult();
		}

		static CorValueResult? GetSimpleResult(CorValue value, CorElementType etype, CorType type) {
			var res = GetSimpleResult(value, etype);
			if (res != null)
				return res;
			if (type == null || !type.IsEnum)
				return res;
			return GetSimpleResult(value, type.EnumUnderlyingType);
		}

		static CorValueResult? GetSimpleResult(CorValue value, CorElementType etype) {
			byte[] data;
			switch (etype) {
			case CorElementType.Boolean:
				if (value.Size != 1)
					break;
				data = value.ReadGenericValue();
				if (data == null)
					break;
				return new CorValueResult(data[0] != 0);

			case CorElementType.Char:
				if (value.Size != 2)
					break;
				data = value.ReadGenericValue();
				if (data == null)
					break;
				return new CorValueResult(BitConverter.ToChar(data, 0));

			case CorElementType.I1:
				if (value.Size != 1)
					break;
				data = value.ReadGenericValue();
				if (data == null)
					break;
				return new CorValueResult((sbyte)data[0]);

			case CorElementType.U1:
				if (value.Size != 1)
					break;
				data = value.ReadGenericValue();
				if (data == null)
					break;
				return new CorValueResult(data[0]);

			case CorElementType.I2:
				if (value.Size != 2)
					break;
				data = value.ReadGenericValue();
				if (data == null)
					break;
				return new CorValueResult(BitConverter.ToInt16(data, 0));

			case CorElementType.U2:
				if (value.Size != 2)
					break;
				data = value.ReadGenericValue();
				if (data == null)
					break;
				return new CorValueResult(BitConverter.ToUInt16(data, 0));

			case CorElementType.I4:
				if (value.Size != 4)
					break;
				data = value.ReadGenericValue();
				if (data == null)
					break;
				return new CorValueResult(BitConverter.ToInt32(data, 0));

			case CorElementType.U4:
				if (value.Size != 4)
					break;
				data = value.ReadGenericValue();
				if (data == null)
					break;
				return new CorValueResult(BitConverter.ToUInt32(data, 0));

			case CorElementType.I8:
				if (value.Size != 8)
					break;
				data = value.ReadGenericValue();
				if (data == null)
					break;
				return new CorValueResult(BitConverter.ToInt64(data, 0));

			case CorElementType.U8:
				if (value.Size != 8)
					break;
				data = value.ReadGenericValue();
				if (data == null)
					break;
				return new CorValueResult(BitConverter.ToUInt64(data, 0));

			case CorElementType.R4:
				if (value.Size != 4)
					break;
				data = value.ReadGenericValue();
				if (data == null)
					break;
				return new CorValueResult(BitConverter.ToSingle(data, 0));

			case CorElementType.R8:
				if (value.Size != 8)
					break;
				data = value.ReadGenericValue();
				if (data == null)
					break;
				return new CorValueResult(BitConverter.ToDouble(data, 0));

			case CorElementType.TypedByRef:
				break;//TODO:

			case CorElementType.I:
				if (value.Size != (uint)Utils.DebuggeeIntPtrSize)
					break;
				data = value.ReadGenericValue();
				if (data == null)
					break;
				Debug.Assert(Utils.DebuggeeIntPtrSize == IntPtr.Size, "Invalid IntPtr size");
				if (Utils.DebuggeeIntPtrSize == 4)
					return new CorValueResult(new IntPtr(BitConverter.ToInt32(data, 0)));
				return new CorValueResult(new IntPtr(BitConverter.ToInt64(data, 0)));

			case CorElementType.U:
			case CorElementType.Ptr:
			case CorElementType.FnPtr:
				if (value.Size != (uint)Utils.DebuggeeIntPtrSize)
					break;
				data = value.ReadGenericValue();
				if (data == null)
					break;
				Debug.Assert(Utils.DebuggeeIntPtrSize == UIntPtr.Size, "Invalid UIntPtr size");
				if (Utils.DebuggeeIntPtrSize == 4)
					return new CorValueResult(new UIntPtr(BitConverter.ToUInt32(data, 0)));
				return new CorValueResult(new UIntPtr(BitConverter.ToUInt64(data, 0)));

			case CorElementType.Class:
			case CorElementType.ValueType:
				var res = GetDecimalResult(value);
				if (res != null)
					return res;
				res = GetDateTimeResult(value);
				if (res != null)
					return res;
				res = GetNullableResult(value);
				if (res != null)
					return res;
				break;
			}

			return null;
		}

		static CorValueResult? GetDecimalResult(CorValue value) {
			var et = value.ExactType;
			if (et == null || !et.IsSystemDecimal)
				return null;
			if (value.Size != 16)
				return null;
			var data = value.ReadGenericValue();
			if (data == null)
				return null;

			var decimalBits = new int[4];
			decimalBits[3] = BitConverter.ToInt32(data, 0);
			decimalBits[2] = BitConverter.ToInt32(data, 4);
			decimalBits[0] = BitConverter.ToInt32(data, 8);
			decimalBits[1] = BitConverter.ToInt32(data, 12);
			try {
				return new CorValueResult(new decimal(decimalBits));
			}
			catch (ArgumentException) {
			}

			return null;
		}

		static CorValueResult? GetDateTimeResult(CorValue value) {
			var et = value.ExactType;
			if (et == null || !et.IsSystemDateTime)
				return null;
			if (value.Size != 8)
				return null;
			var data = value.ReadGenericValue();
			if (data == null)
				return null;
			if (DateTime_ctor_UInt64 != null)
				return new CorValueResult(DateTime_ctor_UInt64(BitConverter.ToUInt64(data, 0)));

			return null;
		}

		static CorValueReader() {
			var ctor = typeof(DateTime).GetConstructor(BindingFlags.NonPublic | BindingFlags.Instance, null, new[] { typeof(ulong) }, null);
			Debug.Assert(ctor != null);
			if (ctor != null) {
				var dm = new DynamicMethod("DateTime_ctor_UInt64", typeof(DateTime), new[] { typeof(ulong) }, true);
				var ilg = dm.GetILGenerator();
				ilg.Emit(OpCodes.Ldarg_0);
				ilg.Emit(OpCodes.Newobj, ctor);
				ilg.Emit(OpCodes.Ret);
				DateTime_ctor_UInt64 = (Func<ulong, DateTime>)dm.CreateDelegate(typeof(Func<ulong, DateTime>));
			}
		}
		static readonly Func<ulong, DateTime> DateTime_ctor_UInt64;

		static CorValueResult? GetNullableResult(CorValue value) {
			if (!value.GetNullableValue(out var nullableValue))
				return null;
			if (nullableValue == null)
				return new CorValueResult(null);

			var valueRes = nullableValue.Value;
			if (!valueRes.IsValid)
				return null;
			return valueRes;
		}
	}
}
