﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cosmos.Logging.Core;
using Cosmos.Logging.Core.Callers;
using Cosmos.Logging.Core.Extensions;
using Cosmos.Logging.Core.Piplelines;
using Cosmos.Logging.Events;

namespace Cosmos.Logging {
    public abstract partial class LoggerBase {
        private readonly AsyncQueue<LogEvent> _asyncQueue = new AsyncQueue<LogEvent>(100);

        private void AutomaticlySubmitLoggerByPipleline() {
            LogPayloadEmitter.Emit(_logPayloadSender, AutomaticPayload.Export());
        }

        private void ManuallySubmitLoggerByPipleline() {
            LogPayloadEmitter.Emit(_logPayloadSender, ManuallyPayload.Export());
        }

        private void SubmitLogEventsManually() {
            if (!_manuallyLogEventDescriptors.Any()) return;
            foreach (var descriptor in _manuallyLogEventDescriptors) {
                (LogEventLevel level, Exception exception, string messageTemplate, LogEventSendMode sendMode, ILogCallerInfo callerInfo, AdditionalOptContext context,
                    object[] messageTemplateParameters) = descriptor;
                ParseAndInsertLogEventIntoQueueAutomaticly(level, exception, messageTemplate, callerInfo, context, messageTemplateParameters);
                var t = ParseAndInsertLogEventIntoAsyncQueueCore(level, exception, messageTemplate, LogEventSendMode.Automatic, callerInfo, context, messageTemplateParameters);
            }

            Task starter = new Task(() => { });
            Task taskPtr = new Task(() => { });
            foreach (var descriptor in _manuallyLogEventDescriptors) {
                (LogEventLevel level, Exception exception, string messageTemplate, LogEventSendMode sendMode, ILogCallerInfo callerInfo, AdditionalOptContext context,
                    object[] messageTemplateParameters) = descriptor;
                var localTask = ParseAndInsertLogEventIntoAsyncQueueCore(level, exception, messageTemplate, sendMode, callerInfo, context, messageTemplateParameters);
                taskPtr.ContinueWith(t => localTask);
            }

            starter.ContinueWith(t => taskPtr.Start())
                .ContinueWith(t => Console.WriteLine("start to DispatchFromAsyncQueue"))
                .ContinueWith(t => DispatchFromAsyncQueue())
                .ContinueWith(t => Console.WriteLine($"count={_manuallyLogEventDescriptors.Count}"))
                .ContinueWith(t => Console.WriteLine("start to _manuallyLogEventDescriptors clear"))
                .ContinueWith(t => _manuallyLogEventDescriptors.Clear())
                .ContinueWith(t => Console.WriteLine($"count={_manuallyLogEventDescriptors.Count}"))
                .ContinueWith(t => ManuallySubmitLoggerByPipleline())
                .ContinueWith(t => Console.WriteLine("all done"));

            starter.Start();
        }

        private void ParseAndInsertLogEventIntoQueueAutomaticly(LogEventLevel level, Exception exception, string messageTemplate,
            ILogCallerInfo callerInfo, AdditionalOptContext context = null, params object[] messageTemplateParameters) {
            var task = ParseAndInsertLogEventIntoAsyncQueueCore(level, exception, messageTemplate, LogEventSendMode.Automatic, callerInfo, context, messageTemplateParameters);
            task.ContinueWith(t => DispatchFromAsyncQueue());
            task.Start();
        }

        private readonly List<ManuallyLogEventDescriptor> _manuallyLogEventDescriptors = new List<ManuallyLogEventDescriptor>();

        private void ParseAndInsertLogEventIntoQueueManually(LogEventLevel level, Exception exception, string messageTemplate,
            ILogCallerInfo callerInfo, AdditionalOptContext context = null, params object[] messageTemplateParameters) {
            _manuallyLogEventDescriptors.Add(new ManuallyLogEventDescriptor(level, exception, messageTemplate, callerInfo, context, messageTemplateParameters));
        }

        private Task ParseAndInsertLogEventIntoAsyncQueueCore(LogEventLevel level, Exception exception, string messageTemplate, LogEventSendMode sendMode,
            ILogCallerInfo callerInfo, AdditionalOptContext context = null, params object[] messageTemplateParameters) {
            var task = new Task(async () => {
                var writer = await _asyncQueue.AcquireWriteAsync(1, CancellationToken.None);
                writer.Visit(
                    succeeded => {
                        _messageParameterProcessor.Process(messageTemplate, __as(messageTemplateParameters),
                            out var parsedTemplate, out var namedMessageProperties, out var positionalMessageProperties);

                        var logEvent = new LogEvent(StateNamespace, DateTimeOffset.Now, level, parsedTemplate, exception,
                            sendMode, callerInfo, namedMessageProperties, positionalMessageProperties, context);

                        if (succeeded.ItemCount >= 1) {
                            _asyncQueue.ReleaseWrite(logEvent);
                        } else {
                            _asyncQueue.ReleaseWrite();
                        }

                        return logEvent;
                    },
                    cancelled => {
                        InternalLogger.WriteLine("When insert log event(0) into async queue, task has been cancelled.");
                        return null;
                    },
                    faulted => {
                        InternalLogger.WriteLine($@"Thrown an exception when insert log event(0) into async queue:{Environment.NewLine}{faulted.Exception.ToUwrapedString()}",
                            faulted.Exception);
                        return null;
                    });
            });

            return task;

            object[] __as(object[] __paramObjs) {
                if (__paramObjs != null && __paramObjs.GetType() != typeof(object[]))
                    return new object[] {__paramObjs};
                return __paramObjs;
            }
        }

        private Task InsertLogEventIntoAsyncQueue(LogEvent logEvent) {
            if (logEvent == null) throw new ArgumentNullException(nameof(logEvent));
            var task = new Task(async () => {
                var writer = await _asyncQueue.AcquireWriteAsync(1, CancellationToken.None);
                writer.Visit(
                    succeeded => {
                        if (succeeded.ItemCount >= 1) {
                            _asyncQueue.ReleaseWrite(_submitCommandLogger);
                        } else {
                            _asyncQueue.ReleaseWrite();
                        }

                        return _submitCommandLogger;
                    },
                    cancelled => {
                        InternalLogger.WriteLine("When insert log event(1) into async queue, task has been cancelled.");
                        return null;
                    },
                    faulted => {
                        InternalLogger.WriteLine($@"Thrown an exception when insert log event(1) into async queue:{Environment.NewLine}{faulted.Exception.ToUwrapedString()}",
                            faulted.Exception);
                        return null;
                    });
            });
            return task;
        }

        private void DispatchLogEventWithoutAsyncQueue(LogEventLevel level, Exception exception, string messageTemplate, LogEventSendMode sendMode, ILogCallerInfo callerInfo,
            AdditionalOptContext context = null, params object[] messageTemplateParameters) {
            _messageParameterProcessor.Process(messageTemplate, __as(messageTemplateParameters),
                out var parsedTemplate, out var namedMessageProperties, out var positionalMessageProperties);

            var logEvent = new LogEvent(StateNamespace, DateTimeOffset.Now, level, parsedTemplate, exception,
                sendMode, callerInfo, namedMessageProperties, positionalMessageProperties, context);

            Dispatch(logEvent);

            object[] __as(object[] __paramObjs) {
                if (__paramObjs != null && __paramObjs.GetType() != typeof(object[]))
                    return new object[] {__paramObjs};
                return __paramObjs;
            }
        }

        private void DispatchFromAsyncQueue() {
            var readerTask = new Task(async () => {
                bool more = true;
                while (more) {
                    var result = await _asyncQueue.AcquireReadAsync(1, CancellationToken.None);
                    result.Visit(
                        succeeded => {
                            LogEvent logEvent = null;
                            if (succeeded is AcquireReadSucceeded<LogEvent> acquireReader) {
                                logEvent = acquireReader.Items.First();
                                Dispatch(logEvent);
                            }

                            if (succeeded.ItemCount < 1) {
                                more = false;
                            }

                            _asyncQueue.ReleaseRead(succeeded.ItemCount);

                            return logEvent;
                        },
                        cancelled => {
                            InternalLogger.WriteLine("Dispatching task has been cancelled.");
                            return null;
                        },
                        faulted => {
                            InternalLogger.WriteLine($@"Thrown an exception when dispatch log event from async queue:{Environment.NewLine}{faulted.Exception.ToUwrapedString()}",
                                faulted.Exception);
                            return null;
                        });
                }
            });

            readerTask.Start();
        }

        // ReSharper disable once InconsistentNaming
        private static readonly EventSpeakAsSubmitCommand _submitCommandLogger = new EventSpeakAsSubmitCommand();

        private class EventSpeakAsSubmitCommand : LogEvent { }
    }
}