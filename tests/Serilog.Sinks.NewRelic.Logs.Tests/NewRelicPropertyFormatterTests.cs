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
            // Arrange
            var number = new ScalarValue(42);
            var flag = new ScalarValue(true);

            // Act
            var simplifiedNumber = NewRelicPropertyFormatter.Simplify(number);
            var simplifiedFlag = NewRelicPropertyFormatter.Simplify(flag);

            // Assert
            await Assert.That(simplifiedNumber).IsEqualTo(42);
            await Assert.That((bool)simplifiedFlag).IsTrue();
        }

        [Test]
        public async Task OtherScalarsAreConvertedToString()
        {
            // Arrange
            var id = Guid.NewGuid();
            var scalar = new ScalarValue(id);

            // Act
            var result = NewRelicPropertyFormatter.Simplify(scalar);

            // Assert
            await Assert.That(result).IsEqualTo(id.ToString());
        }

        [Test]
        public async Task NullScalarStaysNull()
        {
            // Arrange
            var scalar = new ScalarValue(null);

            // Act
            var result = NewRelicPropertyFormatter.Simplify(scalar);

            // Assert
            await Assert.That(result).IsNull();
        }

        [Test]
        public async Task SequenceIsConvertedToArray()
        {
            // Arrange
            var sequence = new SequenceValue(new[] { new ScalarValue(1), new ScalarValue(2) });

            // Act
            var result = (object[])NewRelicPropertyFormatter.Simplify(sequence);

            // Assert
            await Assert.That(result).IsEquivalentTo(new object[] { 1, 2 });
        }

        [Test]
        public async Task StructureIsConvertedToDictionaryWithTypeTag()
        {
            // Arrange
            var structure = new StructureValue(new[] { new LogEventProperty("Width", new ScalarValue(16)) }, "Size");

            // Act
            var result = (Dictionary<string, object>)NewRelicPropertyFormatter.Simplify(structure);

            // Assert
            await Assert.That(result["Width"]).IsEqualTo(16);
            await Assert.That(result["$typeTag"]).IsEqualTo("Size");
        }
    }
}
