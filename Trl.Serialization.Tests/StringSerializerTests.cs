using System;
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
            throw new NotImplementedException();
        }

        [Fact]
        public void ShouldDeserializeList()
        {
            throw new NotImplementedException();
        }

    }
}
