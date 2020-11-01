using System;
using Trl.TermDataRepresentation.Database;
using Xunit;

namespace Trl.TermDataRepresentation.Tests
{
    public class TermReplacementObservationTests
    {
        [Fact]
        public void ShouldObserveSubstitutionsWithoutVariables()
        {
            // Arrange
            var testDatabase = TestUtilities.LoadStatements("0; 0 => inc(0);");
            TermReplacement replacement = null;
            testDatabase.Writer.SetTermReplacementObserver(r => replacement = r);

            // Act
            testDatabase.ExecuteRewriteRules(2);

            // Assert
            Assert.True(StringComparer.InvariantCulture.Equals("inc(0)", replacement.OriginalRootTerm.ToSourceCode(testDatabase)));
            Assert.True(StringComparer.InvariantCulture.Equals("inc(inc(0))", replacement.NewRootTerm.ToSourceCode(testDatabase)));
            Assert.Equal(1, replacement.RewriteIteration);
            Assert.Equal(TermReplacementType.RewriteRule, replacement.ReplacementType);
            Assert.False(replacement.IsDelete);
        }

        [Fact]
        public void ShouldObserveSubstitutionsWithVariables()
        {
            // Arrange
            var testDatabase = TestUtilities.LoadStatements("robot(red); robot(:x) => traffic_light(:x); traffic_light(:x) => robot(green);");
            TermReplacement replacement = null;
            testDatabase.Writer.SetTermReplacementObserver(r => replacement = r);

            // Act
            testDatabase.ExecuteRewriteRules(2);

            // Assert
            Assert.True(StringComparer.InvariantCulture.Equals("traffic_light(red)", replacement.OriginalRootTerm.ToSourceCode(testDatabase)));
            Assert.True(StringComparer.InvariantCulture.Equals("robot(green)", replacement.NewRootTerm.ToSourceCode(testDatabase)));
            Assert.Equal(1, replacement.RewriteIteration);
            Assert.Equal(TermReplacementType.RewriteRule, replacement.ReplacementType);
            Assert.False(replacement.IsDelete);
        }

        [Fact]
        public void ShouldObserveSubstitutionsWithNativeTermEvaluatorFunctions()
        {
            // Arrange
            var testDatabase = TestUtilities.LoadStatements("1;");
            TermReplacement replacement = null;
            testDatabase.Writer.SetTermReplacementObserver(r => replacement = r);
            testDatabase.Writer.SetEvaluator("1", SymbolType.Number, (input, database) => new[] { database.Writer.StoreAtom("2", SymbolType.Number) });
            testDatabase.Writer.SetEvaluator("2", SymbolType.Number, (input, database) => new[] { database.Writer.StoreAtom("3", SymbolType.Number) });

            // Act
            testDatabase.ExecuteRewriteRules(2);

            // Assert
            Assert.True(StringComparer.InvariantCulture.Equals("2", replacement.OriginalRootTerm.ToSourceCode(testDatabase)));
            Assert.True(StringComparer.InvariantCulture.Equals("3", replacement.NewRootTerm.ToSourceCode(testDatabase)));
            Assert.Equal(1, replacement.RewriteIteration);
            Assert.Equal(TermReplacementType.TermEvaluator, replacement.ReplacementType);
            Assert.False(replacement.IsDelete);
        }

        [Fact]
        public void ShouldObserveTermEvaluatorDelete()
        {
            // Arrange
            var testDatabase = TestUtilities.LoadStatements("1;");
            TermReplacement replacement = null;
            testDatabase.Writer.SetTermReplacementObserver(r => replacement = r);
            testDatabase.Writer.SetEvaluator("1", SymbolType.Number, (input, database) => null);

            // Act
            testDatabase.ExecuteRewriteRules();

            // Assert
            Assert.True(StringComparer.InvariantCulture.Equals("1", replacement.OriginalRootTerm.ToSourceCode(testDatabase)));
            Assert.Null(replacement.NewRootTerm);
            Assert.Equal(0, replacement.RewriteIteration);
            Assert.Equal(TermReplacementType.TermEvaluator, replacement.ReplacementType);
            Assert.True(replacement.IsDelete);
        }
    }
}
