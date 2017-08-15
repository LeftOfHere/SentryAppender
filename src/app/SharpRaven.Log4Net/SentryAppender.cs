using System;
using System.Collections.Generic;
using System.Linq;

using log4net.Layout;

using SharpRaven.Data;
using SharpRaven.Log4Net.Extra;

using log4net.Appender;
using log4net.Core;

namespace SharpRaven.Log4Net
{
    public class SentryTag
    {
        public string Name { get; set; }
        public IRawLayout Layout { get; set; }
    }

    public class SentryAppender : AppenderSkeleton
    {
        private static RavenClient ravenClient;
        public string DSN { get; set; }
        public string Logger { get; set; }
        private readonly IList<SentryTag> tagLayouts = new List<SentryTag>();

        public void AddTag(SentryTag tag)
        {
            tagLayouts.Add(tag);
        }

        protected override void Append(LoggingEvent loggingEvent)
        {
            if (ravenClient == null)
            {
                ravenClient = new RavenClient(DSN)
                {
                    Logger = Logger
                };
            }

            var httpExtra = HttpExtra.GetHttpExtra();
            object extra;

            if (httpExtra != null)
            {
                extra = new
                {
                    Environment = new EnvironmentExtra(),
                    Http = httpExtra
                };
            }
            else
            {
                extra = new
                {
                    Environment = new EnvironmentExtra()
                };
            }

            var tags = tagLayouts.ToDictionary(t => t.Name, t => (t.Layout.Format(loggingEvent) ?? "").ToString());

            var exception = loggingEvent.ExceptionObject ?? loggingEvent.MessageObject as Exception;
            var level = Translate(loggingEvent.Level);

            if (exception != null)
            {
                var se = new SentryEvent(exception)
                {
                    Extra = extra,
                    Level = level,
                    Tags = tags
                };
                ravenClient.Capture(se);
            }
            else
            {
                var message = loggingEvent.RenderedMessage;

                if (message == null)
                    return;

                var se = new SentryEvent(message)
                {
                    Extra = extra,
                    Level = level,
                    Tags = tags
                };
                ravenClient.Capture(se);
            }
        }

        internal static ErrorLevel Translate(Level level)
        {
            switch (level.DisplayName)
            {
                case "WARN":
                    return ErrorLevel.Warning;
                case "NOTICE":
                    return ErrorLevel.Info;
                default:
                    ErrorLevel errorLevel;
                    return !Enum.TryParse(level.DisplayName, true, out errorLevel) ? ErrorLevel.Error : errorLevel;
            }
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