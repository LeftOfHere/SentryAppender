﻿using System;
using System.Collections.Generic;

using SharpRaven.Data;

using log4net.Appender;
using log4net.Core;

namespace SharpRaven.Log4Net
{
    public class SentryAppender : AppenderSkeleton
    {
        private static RavenClient ravenClient;
        public string DSN { get; set; }
        public string Logger { get; set; }


        protected override void Append(LoggingEvent loggingEvent)
        {
            if (ravenClient == null)
            {
                ravenClient = new RavenClient(DSN);
                ravenClient.Logger = Logger;
            }

            if (loggingEvent.ExceptionObject != null)
            {
                ravenClient.CaptureException(loggingEvent.ExceptionObject);
            }
            else
            {
                var level = Translate(loggingEvent.Level);
                var stringList = loggingEvent.MessageObject as IList<string>;

                if (stringList != null)
                {
                    foreach (var s in stringList)
                    {
                        ravenClient.CaptureMessage(s, level);
                    }
                }

                string message = loggingEvent.RenderedMessage;

                if (message != null)
                {
                    ravenClient.CaptureMessage(message, level);
                }
            }
        }


        internal static ErrorLevel Translate(Level level)
        {
            switch (level.DisplayName)
            {
                case "WARN":
                    return ErrorLevel.warning;

                case "NOTICE":
                    return ErrorLevel.info;
            }

            ErrorLevel errorLevel;

            return !Enum.TryParse(level.DisplayName, true, out errorLevel)
                       ? ErrorLevel.error
                       : errorLevel;
        }


        protected override void Append(LoggingEvent[] loggingEvents)
        {
            foreach (var loggingEvent in loggingEvents)
            {
                Append(loggingEvent);
            }
        }
    }
}