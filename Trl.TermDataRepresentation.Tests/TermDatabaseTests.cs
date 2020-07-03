﻿using System.Linq;
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

        private Statement ParseStatement(string input)
            => _parser.ParseToAst(input).Statements.Statements.Single();

        [Fact]
        public void ShouldSaveAndLoadTermByLabel()
        {
            // Arrange
            var testAst1 = ParseStatement("l1,l2: 123;");
            _termDatabase.SaveStatement(testAst1);

            // Act
            var statementList = _termDatabase.ReadStatementsForLabel("l1");

            // Assert
            Assert.Single(statementList.Statements);
            Assert.Equal("﻿l1,l2: 123;", statementList.ToSourceCode());
        }

        [Fact]
        public void ShouldReturnNullIfLabelNotFound()
        {
            // Arrange
            var testAst1 = ParseStatement("l1,l2: 123;");

            // Act
            _termDatabase.SaveStatement(testAst1);
            var statementList = _termDatabase.ReadStatementsForLabel("l3");

            // Assert
            Assert.Null(statementList);
        }

        [InlineData("123;")]
        [InlineData("_abc;")]
        [InlineData("\"Testing 123\";")]
        [Theory]
        public void ShouldAssignSameIdentifierToIdenticalTerms(string testInput)
        {
            // Act
            var testAst1 = ParseStatement(testInput);
            var testIdentifier1 = _termDatabase.SaveTerm(testAst1.Term);
            var testAst2 = ParseStatement(testInput);
            var testIdentifier2 = _termDatabase.SaveTerm(testAst2.Term);

            // Assert
            Assert.Equal(testIdentifier1, testIdentifier2);
        }

        [Fact]
        public void ShouldNotAssignSameIdentifierToStringAndNumberWithSameValue()
        {
            // Arrange
            var num = ParseStatement("123;");
            var str = ParseStatement("\"123\";");

            // Act
            var testIdentifier1 = _termDatabase.SaveTerm(num.Term);
            var testIdentifier2 = _termDatabase.SaveTerm(str.Term);

            // Assert
            Assert.NotEqual(testIdentifier1, testIdentifier2);
        }

        [Fact]
        public void ShouldNotAssignSameIdentifierToStringAndIdentifierWithSameValue()
        {
            // Arrange
            var id = ParseStatement("_123;");
            var str = ParseStatement("\"_123\";");

            // Act
            var testIdentifier1 = _termDatabase.SaveTerm(id.Term);
            var testIdentifier2 = _termDatabase.SaveTerm(str.Term);

            // Assert
            Assert.NotEqual(testIdentifier1, testIdentifier2);
        }
    }
}
