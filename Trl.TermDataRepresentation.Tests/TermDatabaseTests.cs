﻿using System;
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

        private TermStatement ParseStatement(string input)
        {
            var parseResult = _parser.ParseToAst(input);
            if (!parseResult.Succeed)
            {
                throw new Exception(parseResult.Errors.First());
            }
            return _parser.ParseToAst(input).Statements.Statements.Single();
        }

        [Fact]
        public void ShouldSaveAndLoadTermByLabel()
        {
            // Arrange
            var testAst1 = ParseStatement("l1,l2: 123;");
            _termDatabase.Writer.StoreStatement(testAst1);

            // Act
            var statementList = _termDatabase.Reader.ReadStatementsForLabel("l1");

            // Assert
            Assert.Single(statementList.Statements);
            Assert.Equal("l1,l2: 123;", statementList.ToSourceCode());
        }

        [Fact]
        public void ShouldReturnNullIfLabelNotFound()
        {
            // Arrange
            var testAst1 = ParseStatement("l1,l2: 123;");

            // Act
            _termDatabase.Writer.StoreStatement(testAst1);
            var statementList = _termDatabase.Reader.ReadStatementsForLabel("l3");

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
            var testIdentifier1 = _termDatabase.Writer.StoreTerm(testAst1.Term).TermIdentifier.Value;
            var testAst2 = ParseStatement(testInput);
            var testIdentifier2 = _termDatabase.Writer.StoreTerm(testAst2.Term).TermIdentifier.Value;

            // Assert
            Assert.Equal(testIdentifier1, testIdentifier2);
        }

        [Fact]
        public void ShouldNotAssignSameIdentifierToStringAndNumberWithSameTermIdentifier()
        {
            // Arrange
            var num = ParseStatement("123;");
            var str = ParseStatement("\"123\";");

            // Act
            var testIdentifier1 = _termDatabase.Writer.StoreTerm(num.Term).TermIdentifier.Value;
            var testIdentifier2 = _termDatabase.Writer.StoreTerm(str.Term).TermIdentifier.Value;

            // Assert
            Assert.NotEqual(testIdentifier1, testIdentifier2);
        }

        [Fact]
        public void ShouldNotAssignSameIdentifierToStringAndIdentifierWithSameTermIdentifier()
        {
            // Arrange
            var id = ParseStatement("_123;");
            var str = ParseStatement("\"_123\";");

            // Act
            var testIdentifier1 = _termDatabase.Writer.StoreTerm(id.Term).TermIdentifier.Value;
            var testIdentifier2 = _termDatabase.Writer.StoreTerm(str.Term).TermIdentifier.Value;

            // Assert
            Assert.NotEqual(testIdentifier1, testIdentifier2);
        }

        [InlineData("((), (1,2,3), (\"abc\"));")]
        [InlineData("(\"a\", \"b\", \"c\");")]
        [InlineData("(1,2,3);")]
        [InlineData("();")]
        [Theory]
        public void ShouldAssignEqualListsToSameTermIdentifier(string testList)
        {
            // Arrange
            var lhs = ParseStatement(testList);
            var rhs = ParseStatement(testList);

            // Act
            var testIdentifier1 = _termDatabase.Writer.StoreTerm(lhs.Term).TermIdentifier.Value;
            var testIdentifier2 = _termDatabase.Writer.StoreTerm(rhs.Term).TermIdentifier.Value;

            // Assert
            Assert.Equal(testIdentifier1, testIdentifier2);
        }

        [InlineData("a(a(),b(),c());")]
        [InlineData("a(a(a));")]
        [InlineData("a(a);")]
        [InlineData("a();")]
        [Theory]
        public void ShouldAssignNonAcTermsToSameTermIdentifier(string testTerm)
        {
            // Arrange
            var lhs = ParseStatement(testTerm);
            var rhs = ParseStatement(testTerm);

            // Act
            var testIdentifier1 = _termDatabase.Writer.StoreTerm(lhs.Term).TermIdentifier.Value;
            var testIdentifier2 = _termDatabase.Writer.StoreTerm(rhs.Term).TermIdentifier.Value;

            // Assert
            Assert.Equal(testIdentifier1, testIdentifier2);
        }


        [InlineData("vertex<x,y,z>(1,2,3);", "vertex<c1,c2,c3>(1,2,3);", false)]
        [InlineData("vertex<x,y,z>(1,2,3);", "vertex<x,y,z>(1,2,3);", true)]
        [InlineData("vertex<x,y,z>(1,2,3);", "vertex(1,2,3);", false)]
        [InlineData("vertex(1,2,3);", "vertex<x,y,z>(1,2,3);", false)]
        [Theory]
        public void ShouldNotIgnoreFieldMappingsForEquality(string lhsTerm, string rhsTerm, bool expectedEquals)
        {
            var lhs = ParseStatement(lhsTerm);
            var rhs = ParseStatement(rhsTerm);

            // Act
            var testIdentifier1 = _termDatabase.Writer.StoreTerm(lhs.Term).TermIdentifier.Value;
            var testIdentifier2 = _termDatabase.Writer.StoreTerm(rhs.Term).TermIdentifier.Value;

            // Assert
            Assert.Equal(expectedEquals, testIdentifier1 == testIdentifier2);
        }

        [Fact]
        public void ShouldMapDifferentIdentifiersToTermAndIdWithSameName()
        {
            // Arrange
            var lhs = ParseStatement("a;");
            var rhs = ParseStatement("a();");

            // Act
            var testIdentifier1 = _termDatabase.Writer.StoreTerm(lhs.Term).TermIdentifier.Value;
            var testIdentifier2 = _termDatabase.Writer.StoreTerm(rhs.Term).TermIdentifier.Value;

            // Assert
            Assert.NotEqual(testIdentifier1, testIdentifier2);
        }

        [Fact]
        public void ShouldLoadAndApplyRewriteRules()
        {
            // Arrange
            var parseResult = _parser.ParseToAst("root: a(1); a(1) => a(2); a(2) => a(3);");
            if (!parseResult.Succeed)
            {
                throw new Exception(parseResult.Errors.First());
            }

            // Act
            _termDatabase.Writer.StoreStatements(parseResult.Statements);
            _termDatabase.ExecuteRewriteRules();

            // Assert
            var s = _termDatabase.Reader.ReadStatementsForLabel("root");
            Assert.NotNull(s);
            Assert.Single(s.Statements);
            Assert.True(StringComparer.InvariantCulture.Equals("root: a(3);", s.Statements.Single().ToSourceCode()));
        }

        [Fact]
        public void ShouldLoadVariable()
        {
            // Arrange
            string input = "root: :xyz;";
            var parseResult = _parser.ParseToAst(input);
            if (!parseResult.Succeed)
            {
                throw new Exception(parseResult.Errors.First());
            }

            // Act
            _termDatabase.Writer.StoreStatements(parseResult.Statements);

            // Assert
            var output = _termDatabase.Reader.ReadCurrentFrame().ToSourceCode();
            Assert.True(StringComparer.InvariantCulture.Equals(input, output));
        }
    }
}
