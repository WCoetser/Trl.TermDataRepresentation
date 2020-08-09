using System.Collections.Generic;
using System.Linq;
using TestObjects;
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
            // Arrange
            var input = "root: (1,2,3);";
                
            // Act
            var output = _serializer.Deserialize<List<int>>(input);

            // Assert
            Assert.True(Enumerable.SequenceEqual(new[] { 1, 2, 3 }, output));

        }

        [Fact]
        public void ShouldDeserializeNestedList()
        {
            // Arrange
            var input = "root: ((1,0,0),(0,1,0),(0,0,1));";

            // Act
            var output = _serializer.Deserialize<List<double[]>>(input);

            // Assert
            Assert.Equal(3, output.Count);
            Assert.True(Enumerable.SequenceEqual(new double[] { 1, 0, 0 }, output[0]));
            Assert.True(Enumerable.SequenceEqual(new double[] { 0, 1, 0 }, output[1]));
            Assert.True(Enumerable.SequenceEqual(new double[] { 0, 0, 1 }, output[2]));
        }

        [Fact]
        public void ShouldDeserializeArray()
        {
            // Arrange
            var input = "root: (1,2,3);";

            // Act
            var output = _serializer.Deserialize<int[]>(input);

            // Assert
            Assert.True(Enumerable.SequenceEqual(new[] { 1, 2, 3 }, output));
        }

        [Fact]
        public void ShouldDeserializeMixedList()
        {
            // Arrange
            var input = "root: (\"abc\",123,null,(1,2,3));";            

            // Act
            var output = _serializer.Deserialize<List<object>>(input);

            // Assert
            Assert.Equal(4, output.Count);
            Assert.Equal("abc", output[0]);
            Assert.Equal((decimal)123, output[1]);
            Assert.Null(output[2]);
            Assert.Equal("1", ((IList<object>)output[3])[0].ToString());
            Assert.Equal("2", ((IList<object>)output[3])[1].ToString());
            Assert.Equal("3", ((IList<object>)output[3])[2].ToString());
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

        [Fact]
        public void ShouldSerializeClassPublicProperties()
        {
            // Arrange
            var input = new PointPropertyTest { X = 100, Y = 150 };

            // Act
            var output = _serializer.Serialize(input);

            // Assert
            Assert.Equal("root: TestObjects.PointPropertyTest<StringVal,X,Y>(\"test\",100,150);", output);
        }

        [Fact]
        public void ShouldSerializeStructPublicFields()
        {
            // Arrange
            var input = new PointFieldTest { X = 100, Y = 150 };

            // Act
            var output = _serializer.Serialize(input);

            // Assert
            Assert.Equal("root: TestObjects.PointFieldTest<StringVal,X,Y>(\"test\",100,150);", output);
        }

        [Fact]
        public void ShouldSerializeClassToTerm()
        {
            // Arrange
            var address = new Address
            {
                Country = "CountryName",
                Line1 = "Line1",
                Line2 = null, // null is not in the output string
                State = "State",
                PostalCode = 1234
            };
            var contact = new ContactInfo
            {
                Address = address,
                Email = "abc@def.com",
                Name = "Test Name"
            };

            // Act
            var output = _serializer.Serialize(contact);

            // Assert
            Assert.Equal("root: TestObjects.ContactInfo<Address,Email,Name>(TestObjects.Address<Country,Line1,PostalCode,State>(\"CountryName\",\"Line1\",1234,\"State\"),\"abc@def.com\",\"Test Name\");", output);
        }
    }
}
