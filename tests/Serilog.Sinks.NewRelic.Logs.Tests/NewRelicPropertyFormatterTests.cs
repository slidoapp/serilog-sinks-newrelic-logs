using System.Collections.Generic;
using Serilog.Events;
using Xunit;

namespace Serilog.Sinks.NewRelic.Logs.Tests
{
    public class NewRelicPropertyFormatterTests
    {
        [Fact]
        public void NumericScalarsKeepTheirType()
        {
            Assert.Equal(42, NewRelicPropertyFormatter.Simplify(new ScalarValue(42)));
            Assert.Equal(true, NewRelicPropertyFormatter.Simplify(new ScalarValue(true)));
        }

        [Fact]
        public void OtherScalarsAreConvertedToString()
        {
            var id = System.Guid.NewGuid();

            Assert.Equal(id.ToString(), NewRelicPropertyFormatter.Simplify(new ScalarValue(id)));
        }

        [Fact]
        public void NullScalarStaysNull()
        {
            Assert.Null(NewRelicPropertyFormatter.Simplify(new ScalarValue(null)));
        }

        [Fact]
        public void SequenceIsConvertedToArray()
        {
            var sequence = new SequenceValue(new[] { new ScalarValue(1), new ScalarValue(2) });

            Assert.Equal(new object[] { 1, 2 }, NewRelicPropertyFormatter.Simplify(sequence));
        }

        [Fact]
        public void StructureIsConvertedToDictionaryWithTypeTag()
        {
            var structure = new StructureValue(new[] { new LogEventProperty("Width", new ScalarValue(16)) }, "Size");

            var result = Assert.IsType<Dictionary<string, object>>(NewRelicPropertyFormatter.Simplify(structure));

            Assert.Equal(16, result["Width"]);
            Assert.Equal("Size", result["$typeTag"]);
        }
    }
}
