using System;
using System.Linq;
using Trl.TermDataRepresentation.Parser;
using Xunit;

namespace Trl.TermDataRepresentation.Tests
{
    public class FrameTests
    {
        [Fact]
        public void ShouldRewriteIdentifier()
        {
            // Arrange
            var termDatabase = TestUtilities.LoadStatements("root: a; a => b; b => c;");

            // Act
            termDatabase.ExecuteRewriteRules();

            // Assert
            var result = termDatabase.Reader.ReadStatementsForLabel("root");
            Assert.True(StringComparer.InvariantCulture.Equals("root: c;", result.ToSourceCode()));
        }
        
        [Fact]
        public void ShouldRewriteString()
        {
            // Arrange
            var termDatabase = TestUtilities.LoadStatements("root: \"a\"; \"a\" => \"b\"; \"b\" => \"c\";");

            // Act
            termDatabase.ExecuteRewriteRules();

            // Assert
            var result = termDatabase.Reader.ReadStatementsForLabel("root");
            Assert.True(StringComparer.InvariantCulture.Equals("root: \"c\";", result.ToSourceCode()));
        }

        [Fact]
        public void ShouldRewriteNumber()
        {
            // Arrange
            var termDatabase = TestUtilities.LoadStatements("root: 1; 1 => 1.1; 1.1 => 1.11;");

            // Act
            termDatabase.ExecuteRewriteRules();

            // Assert
            var result = termDatabase.Reader.ReadStatementsForLabel("root");
            Assert.True(StringComparer.InvariantCulture.Equals("root: 1.11;", result.ToSourceCode()));
        }

        [Fact]
        public void ShouldRewriteList()
        {
            // Arrange
            var termDatabase = TestUtilities.LoadStatements("root: (1,2); (1,2) => (2,3); (2,3) => (3,4);");

            // Act
            termDatabase.ExecuteRewriteRules();

            // Assert
            var result = termDatabase.Reader.ReadStatementsForLabel("root");
            Assert.True(StringComparer.InvariantCulture.Equals("root: (3,4);", result.ToSourceCode()));
        }
        
        [Fact]
        public void ShouldRewriteTermArgument()
        {
            // Arrange
            var termDatabase = TestUtilities.LoadStatements("root: t(a); a => b(); b() => c();");

            // Act
            termDatabase.ExecuteRewriteRules();

            // Assert
            var result = termDatabase.Reader.ReadStatementsForLabel("root");
            Assert.True(StringComparer.InvariantCulture.Equals("root: t(c());", result.ToSourceCode()));
        }

        [Fact]
        public void ShouldRewriteListArgument()
        {
            // Arrange
            var termDatabase = TestUtilities.LoadStatements("root: (a,b()); a => b(); b() => c();");

            // Act
            termDatabase.ExecuteRewriteRules();

            // Assert
            var result = termDatabase.Reader.ReadStatementsForLabel("root");
            Assert.True(StringComparer.InvariantCulture.Equals("root: (c(),c());", result.ToSourceCode()));
        }


        [Fact]
        public void ShouldRewriteRootTerm()
        {
            // Arrange
            var termDatabase = TestUtilities.LoadStatements("root: t((a,b)); t((a,b)) => s((a,b));");

            // Act
            termDatabase.ExecuteRewriteRules();

            // Assert
            var result = termDatabase.Reader.ReadStatementsForLabel("root");
            Assert.True(StringComparer.InvariantCulture.Equals("root: s((a,b));", result.ToSourceCode()));
        }

        [Fact]
        public void ShouldCopyTermClassFieldMappingsOnRewrite()
        {
            // Arrange
            var termDatabase = TestUtilities.LoadStatements("root: t<a,b>(1,2); t<a,b>(1,2) => t<b,c>(1,2); t<b,c>(1,2) => t<c,d>(1,2);");

            // Act
            termDatabase.ExecuteRewriteRules();

            // Assert
            var result = termDatabase.Reader.ReadStatementsForLabel("root");
            Assert.True(StringComparer.InvariantCulture.Equals("root: t<c,d>(1,2);", result.ToSourceCode()));
        }

        [Fact]
        public void ShouldReUseTermsInRewrite()
        {
            // Arrange
            const string testStatements = "root: t<a,b>(1,2); t<a,b>(1,2) => t<b,c>(1,2); t<b,c>(1,2) => t<c,d>(1,2);";
            var termDatabase = TestUtilities.LoadStatements(testStatements);
            termDatabase.ExecuteRewriteRules();
            var metrics = termDatabase.GetDatabaseMetrics();

            // Act
            for (int i = 0; i < 100; i++)
            {
                var parser = new TrlParser();
                var parseResult = parser.ParseToAst(testStatements);
                if (!parseResult.Succeed)
                {
                    throw new Exception(parseResult.Errors.First());
                }
                termDatabase.Writer.StoreStatements(parseResult.Statements);
                termDatabase.ExecuteRewriteRules();
            }

            // Assert
            var result = termDatabase.Reader.ReadStatementsForLabel("root");
            Assert.True(StringComparer.InvariantCulture.Equals("root: t<c,d>(1,2);", result.ToSourceCode()));
            var metricsAssert = termDatabase.GetDatabaseMetrics();
            Assert.Equal(metrics.LabelCount, metricsAssert.LabelCount);
            Assert.Equal(metrics.RewriteRuleCount, metricsAssert.RewriteRuleCount);
            Assert.Equal(metrics.StringCount, metricsAssert.StringCount);
            Assert.Equal(metrics.TermCount, metricsAssert.TermCount);
        }

        [Fact]
        public void ShouldReturnSameResultIfNoMatch()
        {
            // Arrange
            var termDatabase = TestUtilities.LoadStatements("root: t((a,b)); x => y;");

            // Act
            termDatabase.ExecuteRewriteRules();

            // Assert
            var result = termDatabase.Reader.ReadStatementsForLabel("root");
            Assert.True(StringComparer.InvariantCulture.Equals("root: t((a,b));", result.ToSourceCode()));
        }

        [Fact]
        public void ShouldRespectIterationLimit()
        {
            // Arrange
            var termDatabase = TestUtilities.LoadStatements("root: x; x => t(x);");

            // Act
            termDatabase.ExecuteRewriteRules(4);

            // Assert
            var result = termDatabase.Reader.ReadStatementsForLabel("root");
            Assert.True(StringComparer.InvariantCulture.Equals("root: t(t(t(t(x))));", result.ToSourceCode()));
        }

        [InlineData("t(1); t(:x) => s(:x);", "s(1);t(:x) => s(:x);")]
        [InlineData("t(1,2); t(:x,:y) => s(:y,:x);", "s(2,1);t(:x,:y) => s(:y,:x);")]
        [InlineData("t(s(1)); s(:x) => t(:x);", "t(t(1));s(:x) => t(:x);")]
        [InlineData("(t(1)); t(:x) => (:x);", "((1));t(:x) => (:x);")]
        [InlineData("aaa(:x); :x => bbb;", "aaa(bbb);:x => bbb;")]
        [InlineData("root: aaa(bbb(:x)); bbb(:x) => b; bbb(:x) => c;", "root: aaa(b);root: aaa(c);bbb(:x) => b;bbb(:x) => c;")]
        [InlineData("point<x,y>(1,2); point<x,y>(:x,:y) => point<y,x>(:y, :x);", "point<y,x>(2,1);point<x,y>(:x,:y) => point<y,x>(:y,:x);")]
        [InlineData("(1,2,3); (:x,:y,:z) => (:x, :y);", "(1,2);(:x,:y,:z) => (:x,:y);")]
        [Theory]
        public void ShouldApplyUnificationToRewriteProblem(string input, string expectedOutput)
        {
            // Arrange
            var termDatabase = TestUtilities.LoadStatements(input);

            // Act
            termDatabase.ExecuteRewriteRules();

            // Assert
            var results = termDatabase.Reader.ReadCurrentFrame().ToSourceCode();
            Assert.True(StringComparer.InvariantCulture.Equals(expectedOutput, results));
        }
    }
}
