using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Serilog.Events;
using Serilog.Parsing;
using Xunit;

namespace Serilog.Sinks.NewRelic.Logs.Tests
{
    public class NewRelicLogItemTests
    {
        private static readonly DateTimeOffset Timestamp = new DateTimeOffset(2026, 10, 26, 14, 30, 15, 123, TimeSpan.FromHours(1));

        [Fact]
        public void InformationLevelIsSentAsInfo()
        {
            var item = new NewRelicLogItem(CreateEvent(LogEventLevel.Information), null);

            Assert.Equal("Info", item.Attributes["level"]);
        }

        [Theory]
        [InlineData(LogEventLevel.Verbose)]
        [InlineData(LogEventLevel.Debug)]
        [InlineData(LogEventLevel.Warning)]
        [InlineData(LogEventLevel.Error)]
        [InlineData(LogEventLevel.Fatal)]
        public void OtherLevelsKeepTheirSerilogName(LogEventLevel level)
        {
            var item = new NewRelicLogItem(CreateEvent(level), null);

            Assert.Equal(level.ToString(), item.Attributes["level"]);
        }

        [Fact]
        public void TimestampIsSentAsUnixMillisecondsWithTheOriginalOffset()
        {
            var item = new NewRelicLogItem(CreateEvent(LogEventLevel.Information), null);

            Assert.Equal(Timestamp.ToUnixTimeMilliseconds(), item.Timestamp);

            // DateTimeOffset equality ignores the offset, so EqualsExact and the serialized
            // value check that the original +01:00 offset is kept.
            var iso8601Timestamp = Assert.IsType<DateTimeOffset>(item.Attributes["iso8601Timestamp"]);
            Assert.True(Timestamp.EqualsExact(iso8601Timestamp), $"Expected {Timestamp:o}, got {iso8601Timestamp:o}.");

            var json = JsonConvert.SerializeObject(item);
            Assert.Contains("\"iso8601Timestamp\":\"2026-10-26T14:30:15.123+01:00\"", json);
        }

        [Fact]
        public void MessageIsRendered()
        {
            var properties = new[] { new LogEventProperty("Name", new ScalarValue("Slido")) };
            var item = new NewRelicLogItem(CreateEvent(LogEventLevel.Information, "Hello {Name}", properties: properties), null);

            Assert.Equal("Hello \"Slido\"", item.Message);
            Assert.Equal("Slido", item.Attributes["Name"]);
        }

        [Fact]
        public void ExceptionAddsExceptionAndStackTrace()
        {
            var exception = CreateThrownException();
            var item = new NewRelicLogItem(CreateEvent(LogEventLevel.Error, exception: exception), null);

            Assert.Equal(exception.ToString(), item.Attributes["exception"]);
            Assert.Equal(exception.StackTrace, item.Attributes["stackTrace"]);
            Assert.False(item.Attributes.ContainsKey("stack_trace"));
        }

        [Fact]
        public void EventWithoutExceptionHasNoExceptionAttributes()
        {
            var item = new NewRelicLogItem(CreateEvent(LogEventLevel.Information), null);

            Assert.False(item.Attributes.ContainsKey("exception"));
            Assert.False(item.Attributes.ContainsKey("stackTrace"));
        }

        [Fact]
        public void LinkingMetadataIsUnrolledIntoAttributes()
        {
            var linkingMetadata = new DictionaryValue(new[]
            {
                new KeyValuePair<ScalarValue, LogEventPropertyValue>(new ScalarValue("trace.id"), new ScalarValue("abc")),
                new KeyValuePair<ScalarValue, LogEventPropertyValue>(new ScalarValue("span.id"), new ScalarValue("def"))
            });
            var properties = new[] { new LogEventProperty("newrelic.linkingmetadata", linkingMetadata) };
            var item = new NewRelicLogItem(CreateEvent(LogEventLevel.Information, properties: properties), null);

            Assert.Equal("abc", item.Attributes["trace.id"]);
            Assert.Equal("def", item.Attributes["span.id"]);
            Assert.False(item.Attributes.ContainsKey("newrelic.linkingmetadata"));
        }

        [Fact]
        public void PayloadCarriesTheApplicationName()
        {
            var payload = new NewRelicLogPayload("Slido for Windows");

            Assert.Equal("Slido for Windows", payload.Common.Attributes["application"]);
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
