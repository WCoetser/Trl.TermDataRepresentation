using System;
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
            Assert.Single(statements.Statements);
            NumericValue str = (NumericValue)statements.Statements.Single().Term;
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
            Assert.Single(statements.Statements);
            StringValue str = (StringValue)statements.Statements.Single().Term;
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
            Assert.Single(statements.Statements);
            Identifier id = (Identifier)statements.Statements.Single().Term;
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
            Assert.Empty(statements.Statements);
        }

        [InlineData("l1 : \"Testing 123\";", new[] { "l1" })]
        [InlineData("l1.l2, l2 : \"Testing 123\";", new[] { "l1.l2", "l2" })]
        [InlineData("l1.l2, l2, l3 : \"Testing 123\";", new[] { "l1.l2", "l2", "l3" })]
        [Theory]
        public void ShouldParseWithLabel(string parseInput, string[] expectedLabels)
        {
            // Act
            var result = _parser.ParseToAst(parseInput);

            // Assert
            Assert.True(result.Succeed);
            var statement = result.Statements.Statements.Single();
            Assert.True(statement.Label.Identifiers.Select(id => id.Name).SequenceEqual(expectedLabels));

        }

        [InlineData("((1,0,0),(0,1,0),(0,0,1));", 3)]
        [InlineData("(1,2,3,4);", 4)]
        [InlineData("(\"abc\");", 1)]
        [InlineData("();", 0)]
        [Theory]
        public void ShouldParseList(string parseInput, int expectedLength)
        {
            // Act
            var result = _parser.ParseToAst(parseInput);

            // Assert
            Assert.True(result.Succeed);
            var statement = result.Statements.Statements.Single();
            Assert.IsType<TermList>(statement.Term);
            var termList = (TermList)statement.Term;
            Assert.Equal(expectedLength, termList.Terms.Count);
        }
    }
}
