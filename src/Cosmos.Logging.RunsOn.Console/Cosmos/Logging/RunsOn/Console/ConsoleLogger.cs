﻿using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Cosmos.Logging.Configurations;
using Cosmos.Logging.Core.Payloads;
using Cosmos.Logging.Events;
using Cosmos.Logging.Future;
using Cosmos.Logging.Simple;

namespace Cosmos.Logging.RunsOn.Console {
    /// <summary>
    /// Console logger
    /// </summary>
    [SuppressMessage("ReSharper", "ExplicitCallerInfoArgument")]
    public class ConsoleLogger : LoggerBase, IFutureableLogger<ConsoleFutureLogger> {

        /// <inheritdoc />
        public ConsoleLogger(Type sourceType, LogEventLevel minimumLevel, string loggerStateNamespace, Func<string, LogEventLevel, bool> filter,
            LogEventSendMode sendMode, RenderingConfiguration renderingOptions, ILogPayloadSender logPayloadSender)
            : base(sourceType, minimumLevel, loggerStateNamespace, filter, sendMode, renderingOptions, logPayloadSender) { }

        /// <inheritdoc />
        public override IFutureLogger ToFuture(
            [CallerMemberName] string memberName = null,
            [CallerFilePath] string filePath = null,
            [CallerLineNumber] int lineNumber = 0) {
            return new ConsoleFutureLogger(this, memberName, filePath, lineNumber);
        }

        /// <inheritdoc />
        public override ISimpleLogger ToSimple() {
            return new ConsoleSimpleLogger(TargetType, MinimumLevel, StateNamespace, ExposeFilter(), ExposeLogPayloadSender());
        }

#pragma warning disable 1066,4024,4025,4026
        ConsoleFutureLogger IFutureableLogger<ConsoleFutureLogger>.ToFuture(
            [CallerMemberName] string memberName = null,
            [CallerFilePath] string filePath = null,
            [CallerLineNumber] int lineNumber = 0) {
#pragma warning restore 1066,4024,4025,4026
            return new ConsoleFutureLogger(this, memberName, filePath, lineNumber);
        }
    }
}