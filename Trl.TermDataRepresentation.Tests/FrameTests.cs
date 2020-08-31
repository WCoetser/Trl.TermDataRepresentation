using System;
using System.Linq;
using Trl.TermDataRepresentation.Database;
using Trl.TermDataRepresentation.Parser;
using Xunit;

namespace Trl.TermDataRepresentation.Tests
{
    public class FrameTests
    {
        private readonly TermDatabase _termDatabase;
        private readonly TrlParser _parser;

        public FrameTests()
        {
            _termDatabase = new TermDatabase();
            _parser = new TrlParser();
        }

        public void LoadStatements(string statements)
        {
            var parseResult = _parser.ParseToAst(statements);
            if (!parseResult.Succeed)
            {
                throw new Exception(parseResult.Errors.First());
            }
            _termDatabase.Writer.StoreStatements(parseResult.Statements);
        }

        [Fact]
        public void ShouldRewriteIdentifier()
        {
            // Arrange
            LoadStatements("root: a; a => b; b => c;");

            // Act
            _termDatabase.ExecuteRewriteRules();

            // Assert
            var result = _termDatabase.Reader.ReadStatementsForLabel("root");
            Assert.True(StringComparer.InvariantCulture.Equals("root: c;", result.ToSourceCode()));
        }
        
        [Fact]
        public void ShouldRewriteString()
        {
            // Arrange
            LoadStatements("root: \"a\"; \"a\" => \"b\"; \"b\" => \"c\";");

            // Act
            _termDatabase.ExecuteRewriteRules();

            // Assert
            var result = _termDatabase.Reader.ReadStatementsForLabel("root");
            Assert.True(StringComparer.InvariantCulture.Equals("root: \"c\";", result.ToSourceCode()));
        }

        [Fact]
        public void ShouldRewriteNumber()
        {
            // Arrange
            LoadStatements("root: 1; 1 => 1.1; 1.1 => 1.11;");

            // Act
            _termDatabase.ExecuteRewriteRules();

            // Assert
            var result = _termDatabase.Reader.ReadStatementsForLabel("root");
            Assert.True(StringComparer.InvariantCulture.Equals("root: 1.11;", result.ToSourceCode()));
        }

        [Fact]
        public void ShouldRewriteList()
        {
            // Arrange
            LoadStatements("root: (1,2); (1,2) => (2,3); (2,3) => (3,4);");

            // Act
            _termDatabase.ExecuteRewriteRules();

            // Assert
            var result = _termDatabase.Reader.ReadStatementsForLabel("root");
            Assert.True(StringComparer.InvariantCulture.Equals("root: (3,4);", result.ToSourceCode()));
        }
        
        [Fact]
        public void ShouldRewriteTermArgument()
        {
            // Arrange
            LoadStatements("root: t(a); a => b(); b() => c();");

            // Act
            _termDatabase.ExecuteRewriteRules();

            // Assert
            var result = _termDatabase.Reader.ReadStatementsForLabel("root");
            Assert.True(StringComparer.InvariantCulture.Equals("root: t(c());", result.ToSourceCode()));
        }

        [Fact]
        public void ShouldRewriteListArgument()
        {
            // Arrange
            LoadStatements("root: (a,b()); a => b(); b() => c();");

            // Act
            _termDatabase.ExecuteRewriteRules();

            // Assert
            var result = _termDatabase.Reader.ReadStatementsForLabel("root");
            Assert.True(StringComparer.InvariantCulture.Equals("root: (c(),c());", result.ToSourceCode()));
        }


        [Fact]
        public void ShouldRewriteRootTerm()
        {
            // Arrange
            LoadStatements("root: t((a,b)); t((a,b)) => s((a,b));");

            // Act
            _termDatabase.ExecuteRewriteRules();

            // Assert
            var result = _termDatabase.Reader.ReadStatementsForLabel("root");
            Assert.True(StringComparer.InvariantCulture.Equals("root: s((a,b));", result.ToSourceCode()));
        }

        [Fact]
        public void ShouldCopyTermClassFieldMappingsOnRewrite()
        {
            // Arrange
            LoadStatements("root: t<a,b>(1,2); t<a,b>(1,2) => t<b,c>(1,2); t<b,c>(1,2) => t<c,d>(1,2);");

            // Act
            _termDatabase.ExecuteRewriteRules();

            // Assert
            var result = _termDatabase.Reader.ReadStatementsForLabel("root");
            Assert.True(StringComparer.InvariantCulture.Equals("root: t<c,d>(1,2);", result.ToSourceCode()));
        }

        [Fact]
        public void ShouldReUseTermsInRewrite()
        {   
            // Arrange
            LoadStatements("root: t<a,b>(1,2); t<a,b>(1,2) => t<b,c>(1,2); t<b,c>(1,2) => t<c,d>(1,2);");
            _termDatabase.ExecuteRewriteRules();
            var metrics = _termDatabase.GetDatabaseMetrics();

            // Act
            for (int i = 0; i < 100; i++)
            {
                LoadStatements("root: t<a,b>(1,2); t<a,b>(1,2) => t<b,c>(1,2); t<b,c>(1,2) => t<c,d>(1,2);");
                _termDatabase.ExecuteRewriteRules();
            }

            // Assert
            var result = _termDatabase.Reader.ReadStatementsForLabel("root");
            Assert.True(StringComparer.InvariantCulture.Equals("root: t<c,d>(1,2);", result.ToSourceCode()));
            var metricsAssert = _termDatabase.GetDatabaseMetrics();
            Assert.Equal(metrics.LabelCount, metricsAssert.LabelCount);
            Assert.Equal(metrics.RewriteRuleCount, metricsAssert.RewriteRuleCount);
            Assert.Equal(metrics.StringCount, metricsAssert.StringCount);
            Assert.Equal(metrics.TermCount, metricsAssert.TermCount);
        }

        [Fact]
        public void ShouldReturnSameResultIfNoMatch()
        {
            // Arrange
            LoadStatements("root: t((a,b)); x => y;");

            // Act
            _termDatabase.ExecuteRewriteRules();

            // Assert
            var result = _termDatabase.Reader.ReadStatementsForLabel("root");
            Assert.True(StringComparer.InvariantCulture.Equals("root: t((a,b));", result.ToSourceCode()));
        }

        [Fact]
        public void ShouldRespectIterationLimit()
        {
            // Arrange
            LoadStatements("root: x; x => t(x);");

            // Act
            _termDatabase.ExecuteRewriteRules(4);

            // Assert
            var result = _termDatabase.Reader.ReadStatementsForLabel("root");
            Assert.True(StringComparer.InvariantCulture.Equals("root: t(t(t(t(x))));", result.ToSourceCode()));
        }
    }
}
