using System;
using System.Collections.Generic;
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

        [Fact(Skip = "Under construction")]
        public void ShouldSerializeList()
        {
            // Arrange
            var input = new List<int>(new[] { 1, 2, 3 });

            // Act
            var output = _serializer.Serialize(input);

            // Assert
            Assert.Equal("root: (1,2,3);", output);
        }

        [Fact(Skip = "Under construction")]
        public void ShouldSerializeNestedList()
        {
            throw new NotImplementedException();
        }

        [Fact(Skip = "Under construction")]
        public void ShouldDeserializeList()
        {
            throw new NotImplementedException();
        }

        [Fact(Skip = "Under construction")]
        public void ShouldDeserializeNestedList()
        {
            throw new NotImplementedException();
        }

        [Fact(Skip = "Under construction")]
        public void ShouldDeserializeArray()
        {
            throw new NotImplementedException();
        }
    }
}
