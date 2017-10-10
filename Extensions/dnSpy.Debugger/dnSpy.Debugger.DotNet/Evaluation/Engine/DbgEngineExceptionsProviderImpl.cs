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
using System.Linq;
using System.Threading;
using dnSpy.Contracts.Debugger;
using dnSpy.Contracts.Debugger.CallStack;
using dnSpy.Contracts.Debugger.DotNet.Evaluation;
using dnSpy.Contracts.Debugger.Engine.Evaluation;
using dnSpy.Contracts.Debugger.Evaluation;

namespace dnSpy.Debugger.DotNet.Evaluation.Engine {
	sealed class DbgEngineExceptionsProviderImpl : DbgEngineValueNodeProvider {
		readonly DbgDotNetEngineValueNodeFactory valueNodeFactory;

		public DbgEngineExceptionsProviderImpl(DbgDotNetEngineValueNodeFactory valueNodeFactory) =>
			this.valueNodeFactory = valueNodeFactory ?? throw new ArgumentNullException(nameof(valueNodeFactory));

		public override DbgEngineValueNode[] GetNodes(DbgEvaluationContext context, DbgStackFrame frame, DbgValueNodeEvaluationOptions options, CancellationToken cancellationToken) {
			var dispatcher = context.Runtime.GetDotNetRuntime().Dispatcher;
			if (dispatcher.CheckAccess())
				return GetNodesCore(context, frame, options, cancellationToken);
			return GetNodes(dispatcher, context, frame, options, cancellationToken);

			DbgEngineValueNode[] GetNodes(DbgDotNetDispatcher dispatcher2, DbgEvaluationContext context2, DbgStackFrame frame2, DbgValueNodeEvaluationOptions options2, CancellationToken cancellationToken2) =>
				dispatcher2.InvokeRethrow(() => GetNodesCore(context2, frame2, options2, cancellationToken2));
		}

		DbgEngineValueNode[] GetNodesCore(DbgEvaluationContext context, DbgStackFrame frame, DbgValueNodeEvaluationOptions options, CancellationToken cancellationToken) {
			var runtime = context.Runtime.GetDotNetRuntime();
			var exceptions = runtime.GetExceptions(context, frame, cancellationToken);
			if (exceptions.Length == 0)
				return Array.Empty<DbgEngineValueNode>();

			var res = new DbgEngineValueNode[exceptions.Length];
			try {
				for (int i = 0; i < res.Length; i++) {
					cancellationToken.ThrowIfCancellationRequested();
					var info = exceptions[i];
					if (info.IsStowedException)
						res[i] = valueNodeFactory.CreateStowedException(context, frame, info.Id, info.Value, options, cancellationToken);
					else
						res[i] = valueNodeFactory.CreateException(context, frame, info.Id, info.Value, options, cancellationToken);
				}
			}
			catch (Exception ex) {
				context.Runtime.Process.DbgManager.Close(res.Where(a => a != null));
				if (!ExceptionUtils.IsInternalDebuggerError(ex))
					throw;
				return valueNodeFactory.CreateInternalErrorResult(context, frame, cancellationToken);
			}
			return res;
		}
	}
}
