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
using System.Globalization;
using System.Threading;
using dnSpy.Contracts.Debugger;
using dnSpy.Contracts.Debugger.CallStack;
using dnSpy.Contracts.Debugger.DotNet.Evaluation;
using dnSpy.Contracts.Debugger.DotNet.Evaluation.ValueNodes;
using dnSpy.Contracts.Debugger.DotNet.Text;
using dnSpy.Contracts.Debugger.Evaluation;
using dnSpy.Contracts.Text;
using dnSpy.Debugger.DotNet.Metadata;

namespace dnSpy.Roslyn.Shared.Debugger.ValueNodes {
	sealed class DbgDotNetValueNodeImpl : DbgDotNetValueNode {
		public override DmdType ExpectedType { get; }
		public override DmdType ActualType { get; }
		public override string ErrorMessage { get; }
		public override DbgDotNetValue Value { get; }
		public override DbgDotNetText Name { get; }
		public override string Expression { get; }
		public override string ImageName { get; }
		public override bool IsReadOnly { get; }
		public override bool CausesSideEffects { get; }
		public override bool? HasChildren => childNodeProvider?.HasChildren ?? false;

		readonly LanguageValueNodeFactory valueNodeFactory;
		readonly DbgDotNetValueNodeProvider childNodeProvider;
		readonly DbgDotNetValueNodeInfo nodeInfo;
		/*readonly*/ DbgDotNetText valueText;

		public DbgDotNetValueNodeImpl(LanguageValueNodeFactory valueNodeFactory, DbgDotNetValueNodeProvider childNodeProvider, DbgDotNetText name, DbgDotNetValueNodeInfo nodeInfo, string expression, string imageName, bool isReadOnly, bool causesSideEffects, DmdType expectedType, DmdType actualType, string errorMessage, DbgDotNetText valueText) {
			if (name.Parts == null)
				throw new ArgumentException();
			this.valueNodeFactory = valueNodeFactory ?? throw new ArgumentNullException(nameof(valueNodeFactory));
			this.childNodeProvider = childNodeProvider;
			this.nodeInfo = nodeInfo;
			Name = name;
			Value = nodeInfo?.DisplayValue;
			Expression = expression ?? throw new ArgumentNullException(nameof(expression));
			ImageName = imageName ?? throw new ArgumentNullException(nameof(imageName));
			IsReadOnly = isReadOnly;
			CausesSideEffects = causesSideEffects;
			ExpectedType = expectedType;
			ActualType = actualType;
			ErrorMessage = errorMessage;
			this.valueText = valueText;
		}

		public override bool FormatValue(DbgEvaluationContext context, DbgStackFrame frame, ITextColorWriter output, CultureInfo cultureInfo, CancellationToken cancellationToken) {
			if (valueText.Parts != null) {
				valueText.WriteTo(output);
				return true;
			}
			return false;
		}

		public override ulong GetChildCount(DbgEvaluationContext context, DbgStackFrame frame, CancellationToken cancellationToken) =>
			childNodeProvider?.GetChildCount(context, frame, cancellationToken) ?? 0;

		public override DbgDotNetValueNode[] GetChildren(DbgEvaluationContext context, DbgStackFrame frame, ulong index, int count, DbgValueNodeEvaluationOptions options, CancellationToken cancellationToken) {
			if (childNodeProvider == null)
				return Array.Empty<DbgDotNetValueNode>();
			return childNodeProvider.GetChildren(valueNodeFactory, context, frame, index, count, options, cancellationToken);
		}

		protected override void CloseCore(DbgDispatcher dispatcher) {
			Value?.Dispose();
			nodeInfo?.Dispose();
			childNodeProvider?.Dispose();
		}
	}
}
