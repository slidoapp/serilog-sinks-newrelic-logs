using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Serilog.Events;
using Serilog.Parsing;

namespace Serilog.Sinks.NewRelic.Logs.Tests
{
    public class NewRelicLogItemTests
    {
        private static readonly DateTimeOffset Timestamp = new DateTimeOffset(2026, 10, 26, 14, 30, 15, 123, TimeSpan.FromHours(1));

        [Test]
        public async Task InformationLevelIsSentAsInfo()
        {
            // Arrange
            var logEvent = CreateEvent(LogEventLevel.Information);

            // Act
            var item = new NewRelicLogItem(logEvent, null);

            // Assert
            await Assert.That(item.Attributes["level"]).IsEqualTo("Info");
        }

        [Test]
        [Arguments(LogEventLevel.Verbose)]
        [Arguments(LogEventLevel.Debug)]
        [Arguments(LogEventLevel.Warning)]
        [Arguments(LogEventLevel.Error)]
        [Arguments(LogEventLevel.Fatal)]
        public async Task OtherLevelsKeepTheirSerilogName(LogEventLevel level)
        {
            // Arrange
            var logEvent = CreateEvent(level);

            // Act
            var item = new NewRelicLogItem(logEvent, null);

            // Assert
            await Assert.That(item.Attributes["level"]).IsEqualTo(level.ToString());
        }

        [Test]
        public async Task TimestampIsSentAsUnixMillisecondsWithTheOriginalOffset()
        {
            // Arrange
            var logEvent = CreateEvent(LogEventLevel.Information);

            // Act
            var item = new NewRelicLogItem(logEvent, null);
            var json = JsonConvert.SerializeObject(item);

            // Assert
            await Assert.That(item.Timestamp).IsEqualTo(Timestamp.ToUnixTimeMilliseconds());

            // DateTimeOffset equality ignores the offset, so EqualsExact and the serialized
            // value check that the original +01:00 offset is kept.
            var iso8601Timestamp = (DateTimeOffset)item.Attributes["iso8601Timestamp"];
            await Assert.That(Timestamp.EqualsExact(iso8601Timestamp)).IsTrue();
            await Assert.That(json).Contains("\"iso8601Timestamp\":\"2026-10-26T14:30:15.123+01:00\"");
        }

        [Test]
        public async Task MessageIsRendered()
        {
            // Arrange
            var properties = new[] { new LogEventProperty("Name", new ScalarValue("Slido")) };
            var logEvent = CreateEvent(LogEventLevel.Information, "Hello {Name}", properties: properties);

            // Act
            var item = new NewRelicLogItem(logEvent, null);

            // Assert
            await Assert.That(item.Message).IsEqualTo("Hello \"Slido\"");
            await Assert.That(item.Attributes["Name"]).IsEqualTo("Slido");
        }

        [Test]
        public async Task ExceptionAddsExceptionAndStackTrace()
        {
            // Arrange
            var exception = CreateThrownException();
            var logEvent = CreateEvent(LogEventLevel.Error, exception: exception);

            // Act
            var item = new NewRelicLogItem(logEvent, null);

            // Assert
            await Assert.That(item.Attributes["exception"]).IsEqualTo(exception.ToString());
            await Assert.That(item.Attributes["stackTrace"]).IsEqualTo(exception.StackTrace);
            await Assert.That(item.Attributes.ContainsKey("stack_trace")).IsFalse();
        }

        [Test]
        public async Task EventWithoutExceptionHasNoExceptionAttributes()
        {
            // Arrange
            var logEvent = CreateEvent(LogEventLevel.Information);

            // Act
            var item = new NewRelicLogItem(logEvent, null);

            // Assert
            await Assert.That(item.Attributes.ContainsKey("exception")).IsFalse();
            await Assert.That(item.Attributes.ContainsKey("stackTrace")).IsFalse();
        }

        [Test]
        public async Task LinkingMetadataIsUnrolledIntoAttributes()
        {
            // Arrange
            var linkingMetadata = new DictionaryValue(new[]
            {
                new KeyValuePair<ScalarValue, LogEventPropertyValue>(new ScalarValue("trace.id"), new ScalarValue("abc")),
                new KeyValuePair<ScalarValue, LogEventPropertyValue>(new ScalarValue("span.id"), new ScalarValue("def"))
            });
            var properties = new[] { new LogEventProperty("newrelic.linkingmetadata", linkingMetadata) };
            var logEvent = CreateEvent(LogEventLevel.Information, properties: properties);

            // Act
            var item = new NewRelicLogItem(logEvent, null);

            // Assert
            await Assert.That(item.Attributes["trace.id"]).IsEqualTo("abc");
            await Assert.That(item.Attributes["span.id"]).IsEqualTo("def");
            await Assert.That(item.Attributes.ContainsKey("newrelic.linkingmetadata")).IsFalse();
        }

        [Test]
        public async Task PayloadCarriesTheApplicationName()
        {
            // Arrange
            const string applicationName = "Slido for Windows";

            // Act
            var payload = new NewRelicLogPayload(applicationName);

            // Assert
            await Assert.That(payload.Common.Attributes["application"]).IsEqualTo(applicationName);
        }

        private static LogEvent CreateEvent(
            LogEventLevel level,
            string messageTemplate = "Test message",
            Exception exception = null,
            IEnumerable<LogEventProperty> properties = null)
        {
            var template = new MessageTemplateParser().Parse(messageTemplate);

            return new LogEvent(Timestamp, level, exception, template, properties ?? Enumerable.Empty<LogEventProperty>());
        }

        private static Exception CreateThrownException()
        {
            try
            {
                throw new InvalidOperationException("Test exception");
            }
            catch (InvalidOperationException ex)
            {
                return ex;
            }
        }
    }
}
