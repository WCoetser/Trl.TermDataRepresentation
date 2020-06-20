using System.Linq;
using Trl.TermDataRepresentation.Database;
using Trl.TermDataRepresentation.Parser;
using Trl.TermDataRepresentation.Parser.AST;
using Xunit;

namespace Trl.TermDataRepresentation.Tests
{
    public class TermDatabaseTests
    {
        private readonly TermDatabase _termDatabase;
        private readonly TrlParser _parser;

        public TermDatabaseTests()
        {
            _termDatabase = new TermDatabase();
            _parser = new TrlParser();
        }

        private ITrlParseResult Parse(string input)
            => _parser.ParseToAst(input).Statements.StatementList.Single();

        [InlineData("123;")]
        [InlineData("_abc;")]
        [InlineData("\"Testing 123\";")]
        [Theory]
        public void ShouldAssignSameIdentifierToIdenticalTerms(string testInput)
        {
            // Act
            var testAst1 = Parse(testInput);
            var testIdentifier1 = _termDatabase.SaveTerm((ITrlTerm)testAst1);
            var testAst2 = Parse(testInput);
            var testIdentifier2 = _termDatabase.SaveTerm((ITrlTerm)testAst2);

            // Assert
            Assert.Equal(testIdentifier1, testIdentifier2);
        }

        [Fact]
        public void ShouldNotAssignSameIdentifierToStringAndNumberWithSameValue()
        {
            // Arrange
            var num = Parse("123;");
            var str = Parse("\"123\";");

            // Act
            var testIdentifier1 = _termDatabase.SaveTerm((ITrlTerm)num);
            var testIdentifier2 = _termDatabase.SaveTerm((ITrlTerm)str);

            // Assert
            Assert.NotEqual(testIdentifier1, testIdentifier2);
        }
    }
}
