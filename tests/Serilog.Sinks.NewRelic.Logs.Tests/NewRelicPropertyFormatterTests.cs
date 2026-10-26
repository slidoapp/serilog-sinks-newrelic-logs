using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Serilog.Events;

namespace Serilog.Sinks.NewRelic.Logs.Tests
{
    public class NewRelicPropertyFormatterTests
    {
        [Test]
        public async Task NumericScalarsKeepTheirType()
        {
            await Assert.That(NewRelicPropertyFormatter.Simplify(new ScalarValue(42))).IsEqualTo(42);
            await Assert.That((bool)NewRelicPropertyFormatter.Simplify(new ScalarValue(true))).IsTrue();
        }

        [Test]
        public async Task OtherScalarsAreConvertedToString()
        {
            var id = Guid.NewGuid();

            await Assert.That(NewRelicPropertyFormatter.Simplify(new ScalarValue(id))).IsEqualTo(id.ToString());
        }

        [Test]
        public async Task NullScalarStaysNull()
        {
            await Assert.That(NewRelicPropertyFormatter.Simplify(new ScalarValue(null))).IsNull();
        }

        [Test]
        public async Task SequenceIsConvertedToArray()
        {
            var sequence = new SequenceValue(new[] { new ScalarValue(1), new ScalarValue(2) });

            var result = (object[])NewRelicPropertyFormatter.Simplify(sequence);

            await Assert.That(result).IsEquivalentTo(new object[] { 1, 2 });
        }

        [Test]
        public async Task StructureIsConvertedToDictionaryWithTypeTag()
        {
            var structure = new StructureValue(new[] { new LogEventProperty("Width", new ScalarValue(16)) }, "Size");

            var result = (Dictionary<string, object>)NewRelicPropertyFormatter.Simplify(structure);

            await Assert.That(result["Width"]).IsEqualTo(16);
            await Assert.That(result["$typeTag"]).IsEqualTo("Size");
        }
    }
}
