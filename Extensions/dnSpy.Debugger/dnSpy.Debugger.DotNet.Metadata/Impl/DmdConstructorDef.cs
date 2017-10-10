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
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading;

namespace dnSpy.Debugger.DotNet.Metadata.Impl {
	abstract class DmdConstructorDef : DmdConstructorInfoBase {
		public sealed override DmdType DeclaringType { get; }
		public sealed override DmdType ReflectedType { get; }
		public sealed override int MetadataToken => (int)(0x06000000 + rid);
		public sealed override bool IsMetadataReference => false;

		public sealed override bool IsGenericMethodDefinition => GetMethodSignature().GenericParameterCount != 0;
		public sealed override bool IsGenericMethod => GetMethodSignature().GenericParameterCount != 0;

		protected uint Rid => rid;
		readonly uint rid;

		protected DmdConstructorDef(uint rid, DmdType declaringType, DmdType reflectedType) {
			this.rid = rid;
			DeclaringType = declaringType ?? throw new ArgumentNullException(nameof(declaringType));
			ReflectedType = reflectedType ?? throw new ArgumentNullException(nameof(reflectedType));
		}

		public sealed override DmdConstructorInfo Resolve(bool throwOnError) => this;

		protected abstract DmdType[] CreateGenericParameters();
		public sealed override ReadOnlyCollection<DmdType> GetGenericArguments() {
			if (__genericParameters_DONT_USE != null)
				return __genericParameters_DONT_USE;
			var res = CreateGenericParameters();
			Interlocked.CompareExchange(ref __genericParameters_DONT_USE, ReadOnlyCollectionHelpers.Create(res), null);
			return __genericParameters_DONT_USE;
		}
		volatile ReadOnlyCollection<DmdType> __genericParameters_DONT_USE;

		public sealed override ReadOnlyCollection<DmdParameterInfo> GetParameters() {
			if (__parameters_DONT_USE != null)
				return __parameters_DONT_USE;
			var info = CreateParameters();
			Debug.Assert(info.Length == GetMethodSignature().GetParameterTypes().Count);
			Interlocked.CompareExchange(ref __parameters_DONT_USE, ReadOnlyCollectionHelpers.Create(info), null);
			return __parameters_DONT_USE;
		}
		volatile ReadOnlyCollection<DmdParameterInfo> __parameters_DONT_USE;
		protected abstract DmdParameterInfo[] CreateParameters();

		public sealed override ReadOnlyCollection<DmdCustomAttributeData> GetCustomAttributesData() {
			if (__customAttributes_DONT_USE == null)
				InitializeCustomAttributes();
			return __customAttributes_DONT_USE;
		}

		void InitializeCustomAttributes() {
			if (__customAttributes_DONT_USE != null)
				return;
			var info = CreateCustomAttributes();
			var newSAs = ReadOnlyCollectionHelpers.Create(info.sas);
			var newCAs = CustomAttributesHelper.AddPseudoCustomAttributes(this, info.cas, newSAs);
			lock (LockObject) {
				if (__customAttributes_DONT_USE == null) {
					__securityAttributes_DONT_USE = newSAs;
					__customAttributes_DONT_USE = newCAs;
				}
			}
		}
		volatile ReadOnlyCollection<DmdCustomAttributeData> __customAttributes_DONT_USE;
		volatile ReadOnlyCollection<DmdCustomAttributeData> __securityAttributes_DONT_USE;

		protected abstract (DmdCustomAttributeData[] cas, DmdCustomAttributeData[] sas) CreateCustomAttributes();

		public sealed override ReadOnlyCollection<DmdCustomAttributeData> GetSecurityAttributesData() {
			if (__customAttributes_DONT_USE == null)
				InitializeCustomAttributes();
			return __securityAttributes_DONT_USE;
		}
	}
}
