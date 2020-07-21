using System;
using System.Collections.Generic;
using System.Linq;
using Trl.Serialization;
using Xunit;

namespace Trl.Serializer.Tests
{
    public class StringSerializerTests
    {

        private readonly StringSerializer _serializer;

        public StringSerializerTests()
        {
            _serializer = new StringSerializer();
        }

        [Fact]
        public void ShouldSerializeString()
        {
            // Arrange
            var input = "abc";

            // Act
            var output = _serializer.Serialize(input);

            // Assert
            Assert.Equal("root: \"abc\";", output);
        }

        [Fact]
        public void ShouldDeserializeString()
        {
            // Arrange
            var input = "root: \"abc\";";

            // Act
            var output = _serializer.Deserialize<string>(input);

            // Assert
            Assert.Equal("abc", output);
        }

        [Fact]
        public void ShouldDeseializeNumber()
        {
            // Arrange
            var input = "root: 123;";

            // Act
            var output = _serializer.Deserialize<int>(input);

            // Assert
            Assert.Equal(123, output);
        }

        [Fact]
        public void ShouldSerializeNumber()
        {
            // Arrange
            var input = 123;

            // Act
            var output = _serializer.Serialize(input);

            // Assert
            Assert.Equal("root: 123;", output);
        }

        [Fact]
        public void ShouldSerializeList()
        {
            // Arrange
            var input = new List<int>(new[] { 1, 2, 3 });

            // Act
            var output = _serializer.Serialize(input);

            // Assert
            Assert.Equal("root: (1,2,3);", output);
        }

        [Fact]
        public void ShouldSerializeEmptyList()
        {
            // Arrange
            var input = Enumerable.Empty<int>();

            // Act
            var output = _serializer.Serialize(input);

            // Assert
            Assert.Equal("root: ();", output);
        }

        [Fact]
        public void ShouldSerializeMixedTypesList()
        {
            // Arrange
            var input = new object[] { "abc", 123, null, new[] { 0x01,0x02,0x03 } };

            // Act
            var output = _serializer.Serialize(input);

            // Assert
            Assert.Equal("root: (\"abc\",123,null,(1,2,3));", output);
        }

        [Fact]
        public void ShouldSerializeNestedList()
        {
            // Arrange
            var input = new List<double[]>();
            input.Add(new double[] { 1, 0, 0 });
            input.Add(new double[] { 0, 1, 0 });
            input.Add(new double[] { 0, 0, 1 });

            // Act
            var output = _serializer.Serialize(input);

            // Assert
            Assert.Equal("root: ((1,0,0),(0,1,0),(0,0,1));", output);
        }

        [Fact]
        public void ShouldDeserializeList()
        {
            throw new NotImplementedException();
        }

        [Fact]
        public void ShouldDeserializeNestedList()
        {
            throw new NotImplementedException();
        }

        [Fact]
        public void ShouldDeserializeArray()
        {
            throw new NotImplementedException();
        }

        [Fact]
        public void ShouldDeserializeMixedList()
        {
            throw new NotImplementedException();
        }

        [Fact]
        public void ShouldDeserializeNull()
        {
            // Arrange
            string input = "root: null;";

            // Act
            var output = _serializer.Deserialize<int?>(input);

            // Assert
            Assert.Null(output);
        }

        [Fact]
        public void ShouldSerializeNull()
        {
            // Arrange
            object input = null;

            // Act
            var output = _serializer.Serialize(input);

            // Assert
            Assert.Equal("root: null;", output);
        }
    }
}
