using System.Linq;
using Trl.TermDataRepresentation.Parser;
using Trl.TermDataRepresentation.Parser.AST;
using Xunit;

namespace Trl.TermDataRepresentation.Tests
{
    public class TrlParserTests
    {
        private readonly TrlParser _parser;

        public TrlParserTests()
        {
            _parser = new TrlParser();
        }

        [InlineData("123")]
        [InlineData("-123")]
        [InlineData("+123")]
        [InlineData("0.123")]
        [Theory]
        public void ShouldParseNumber(string inputString)
        {
            // Act
            var result = _parser.ParseToAst($"{inputString};");

            // Assert
            var statements = result.Statements;
            Assert.True(result.Succeed);
            Assert.Single(statements.StatementList);
            NumericValue str = (NumericValue)statements.StatementList.Single();
            Assert.Equal(inputString, str.Value);
        }

        [InlineData("\"Testing 123\"")]
        [InlineData("\"\"")] // empty string
        [InlineData("\"Testing \\\" 123\"")] // Test escaping the double quote character
        [Theory]
        public void ShouldParseString(string inputString)
        {
            // Act
            var result = _parser.ParseToAst($"{inputString};");

            // Assert
            var statements = result.Statements;
            Assert.True(result.Succeed);
            Assert.Single(statements.StatementList);
            StringValue str = (StringValue)statements.StatementList.Single();
            Assert.Equal(inputString[1..^1], str.Value);
        }

        [InlineData("_test")]
        [InlineData("part1.part2")]
        [InlineData("part1.part2.part3")]
        [InlineData("part1.part2_.pa_rt3")]
        [Theory]
        public void ShouldParseIdentifier(string inputString)
        {
            // Act
            var result = _parser.ParseToAst($"{inputString};");

            // Assert
            var statements = result.Statements;
            Assert.True(result.Succeed);
            Assert.Single(statements.StatementList);
            Identifier id = (Identifier)statements.StatementList.Single();
            Assert.Equal(inputString, id.Name);
        }

        [Fact]
        public void ShouldNotParseWhitespaceOnlyString()
        {
            // Act
            var result = _parser.ParseToAst("\t \n ");

            // Assert
            Assert.False(result.Succeed);
        }

        [Fact]
        public void ShouldParseSemicolonSequence()
        {
            // Act
            var result = _parser.ParseToAst(";;;");

            // Assert
            Assert.True(result.Succeed);
            var statements = result.Statements;
            Assert.Empty(statements.StatementList);
        }
    }
}
