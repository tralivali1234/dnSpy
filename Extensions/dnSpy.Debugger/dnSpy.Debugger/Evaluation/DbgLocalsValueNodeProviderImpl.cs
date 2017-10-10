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
using dnSpy.Contracts.Debugger.CallStack;
using dnSpy.Contracts.Debugger.Engine.Evaluation;
using dnSpy.Contracts.Debugger.Evaluation;

namespace dnSpy.Debugger.Evaluation {
	sealed class DbgLocalsValueNodeProviderImpl : DbgLocalsValueNodeProvider {
		public override DbgLanguage Language { get; }

		readonly Guid runtimeKindGuid;
		readonly DbgEngineLocalsValueNodeProvider engineLocalsValueNodeProvider;

		public DbgLocalsValueNodeProviderImpl(DbgLanguage language, Guid runtimeKindGuid, DbgEngineLocalsValueNodeProvider engineLocalsValueNodeProvider) {
			Language = language ?? throw new ArgumentNullException(nameof(language));
			this.runtimeKindGuid = runtimeKindGuid;
			this.engineLocalsValueNodeProvider = engineLocalsValueNodeProvider ?? throw new ArgumentNullException(nameof(engineLocalsValueNodeProvider));
		}

		public override DbgLocalsValueNodeInfo[] GetNodes(DbgEvaluationContext context, DbgStackFrame frame, DbgValueNodeEvaluationOptions options, DbgLocalsValueNodeEvaluationOptions localsOptions, CancellationToken cancellationToken) {
			if (context == null)
				throw new ArgumentNullException(nameof(context));
			if (!(context is DbgEvaluationContextImpl))
				throw new ArgumentException();
			if (context.Language != Language)
				throw new ArgumentException();
			if (context.Runtime.RuntimeKindGuid != runtimeKindGuid)
				throw new ArgumentException();
			if (frame == null)
				throw new ArgumentNullException(nameof(frame));
			if (frame.Runtime.RuntimeKindGuid != runtimeKindGuid)
				throw new ArgumentException();
			return DbgValueNodeUtils.ToLocalsValueNodeInfoArray(Language, frame.Runtime, engineLocalsValueNodeProvider.GetNodes(context, frame, options, localsOptions, cancellationToken));
		}
	}
}
