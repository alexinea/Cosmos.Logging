﻿using System;
using Cosmos.Logging.Core.LogFields;
using Cosmos.Logging.Events;

namespace Cosmos.Logging {
    /// <summary>
    /// Field utility
    /// </summary>
    public static class Fields {
        /// <summary>
        /// Create a level field
        /// </summary>
        /// <param name="level"></param>
        /// <returns></returns>
        public static ILogField Level(LogEventLevel level) {
            return new LogEventLevelField(level);
        }

        /// <summary>
        /// Create a message field
        /// </summary>
        /// <param name="messageTemplate"></param>
        /// <param name="append"></param>
        /// <returns></returns>
        public static ILogField Message(string messageTemplate, bool append = false) {
            return new MessageTemplateField(messageTemplate, append);
        }

        /// <summary>
        /// Create an exception field
        /// </summary>
        /// <param name="exception"></param>
        /// <returns></returns>
        public static ILogField Exception(Exception exception) {
            return new ExceptionField(exception);
        }

        /// <summary>
        /// Create an args field
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        public static ILogField Args(params object[] args) {
            return new ArgsField(args);
        }

        /// <summary>
        /// Create a tags field
        /// </summary>
        /// <param name="tags"></param>
        /// <returns></returns>
        public static ILogField Tags(params string[] tags) {
            return new TagsField(tags);
        }

        /// <summary>
        /// Create an event id field
        /// </summary>
        /// <returns></returns>
        public static ILogField EventId() {
            return new EventIdField();
        }

        /// <summary>
        /// Create an event id field
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static ILogField EventId(string name) {
            return new EventIdField(name);
        }

        /// <summary>
        /// Create an event id field
        /// </summary>
        /// <param name="id"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public static ILogField EventId(Guid id, string name) {
            return new EventIdField(id, name);
        }

        /// <summary>
        /// Create an event id field
        /// </summary>
        /// <param name="id"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public static ILogField EventId(int id, string name) {
            return new EventIdField(id, name);
        }

        /// <summary>
        /// Create an event id field
        /// </summary>
        /// <param name="id"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public static ILogField EventId(string id, string name) {
            return new EventIdField(id, name);
        }

        /// <summary>
        /// Create an event id field
        /// </summary>
        /// <param name="eventId"></param>
        /// <returns></returns>
        public static ILogField EventId(LogEventId eventId) {
            return new EventIdField(eventId);
        }
    }
}