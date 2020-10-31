using System;
using System.Collections.Generic;
using System.Linq;
using Trl.IntegerMapper;
using Trl.TermDataRepresentation.Database;
using Xunit;

namespace Trl.TermDataRepresentation.Tests
{
    public class TermEvaluatorTests
    {
        [InlineData("t1(); t2(aaa); t3();", "t1();t3();")]
        [InlineData("t1(); t2(); t3();", "t1();t3();")]
        [InlineData("t1(); t4(t2(aaa)); t3();", "t1();t3();")]
        [InlineData("t1(); t4(t2()); t3();", "t1();t3();")]
        [Theory]
        public void ShouldDeleteTermIfEvaluatorReturnsEmpty(string input, string expectedOutput)
        {
            RunTest(input, expectedOutput, "t2", SymbolType.NonAcTerm, (inputTerm, database) => Enumerable.Empty<Term>());            
        }


        [InlineData("t1(); t2(aaa); t3();", "t1();t3();")]
        [InlineData("t1(); t2(); t3();", "t1();t3();")]
        [InlineData("t1(); t4(t2(aaa)); t3();", "t1();t3();")]
        [InlineData("t1(); t4(t2()); t3();", "t1();t3();")]
        [Theory]
        public void ShouldDeleteTermIfEvaluatorReturnsNull(string input, string expectedOutput)
        {
            RunTest(input, expectedOutput, "t2", SymbolType.NonAcTerm, (inputTerm, database) => null);
        }


        [InlineData("t1(); t2(aaa); t3();", "t1();t2(bbb);t3();")]
        [InlineData("t1(); aaa; t3();", "t1();bbb;t3();")]
        [InlineData("(t1(), aaa, t3());", "(t1(),bbb,t3());")]
        [Theory]
        public void ShouldApplyTermEvaluatorToIdentifier(string input, string expectedOutput)
        {
            RunTest(input, expectedOutput, "aaa", SymbolType.Identifier, (inputTerm, database) =>
            {
                return new[] { database.Writer.StoreAtom("bbb", SymbolType.Identifier) };
            });
        }

        [InlineData("t1(); t2(\"aaa\"); t3();", "t1();t2(\"bbb\");t3();")]
        [InlineData("t1(); \"aaa\"; t3();", "t1();\"bbb\";t3();")]
        [InlineData("(t1(), \"aaa\", t3());", "(t1(),\"bbb\",t3());")]
        [Theory]
        public void ShouldApplyTermEvaluatorToString(string input, string expectedOutput)
        {
            RunTest(input, expectedOutput, "aaa", SymbolType.String, (inputTerm, database) =>
            {
                return new[] { database.Writer.StoreAtom("bbb", SymbolType.String) };
            });
        }

        [InlineData("t1(); t2(111); t3();", "t1();t2(222);t3();")]
        [InlineData("t1(); 111; t3();", "t1();222;t3();")]
        [InlineData("(t1(), 111, t3());", "(t1(),222,t3());")]
        [Theory]
        public void ShouldApplyTermEvaluatorToNumber(string input, string expectedOutput)
        {
            RunTest(input, expectedOutput, "111", SymbolType.Number, (inputTerm, database) =>
            {
                return new[] { database.Writer.StoreAtom("222", SymbolType.Number) };
            });
        }

        [InlineData("t1(); t2((1,2,3)); t3();", "t1();t2(());t3();")]
        [InlineData("t1(); (1,2,3); t3();", "t1();();t3();")]
        [Theory]
        public void ShouldApplyTermEvaluatorToList(string input, string expectedOutput)
        {
            RunTest(input, expectedOutput, null, SymbolType.TermList, (inputTerm, database) =>
            {
                return new[] { database.Writer.StoreTermList(new Term[0]) };
            });
        }

        [InlineData("t1(); root: t2(ttt(1,2,3)); t3();", "t1();root: t2(sss());t3();")]
        [InlineData("t1(); root: ttt(); t3();", "t1();root: sss();t3();")]
        [Theory]
        public void ShouldApplyTermEvaluatorToNonAcTerm(string input, string expectedOutput)
        {
            RunTest(input, expectedOutput, "ttt", SymbolType.NonAcTerm, (inputTerm, database) =>
            {
                return new[] { database.Writer.StoreNonAcTerm("sss", new Term[0], new Dictionary<TermMetaData, Term>()) };
            });
        }

        [Fact]
        public void ShouldGenerateMultipleTerms()
        {
            RunTest("val(count_to_3());", "val(1);val(2);val(3);", "count_to_3", SymbolType.NonAcTerm, (inputTerm, database) =>
            {
                return new[] 
                { 
                    database.Writer.StoreAtom("1", SymbolType.Number),
                    database.Writer.StoreAtom("2", SymbolType.Number),
                    database.Writer.StoreAtom("3", SymbolType.Number)
                };
            });
        }

        [InlineData("t1(); root: ttt(:x); t3();", "t1();root: ttt(1);t3();")]
        [InlineData("t1(); :x; t3();", "t1();1;t3();")]
        [Theory]
        public void ShouldApplyTermEvaluatorToVariable(string input, string expectedOutput)
        {
            RunTest(input, expectedOutput, ":x", SymbolType.Variable, (inputTerm, database) =>
            {
                return new[] { database.Writer.StoreAtom("1", SymbolType.Number) };
            });
        }

        [InlineData("t1(); :x; t3();", "t1();:x;t3();")]
        [Theory]
        public void ShouldResultInSameOutputWhenInputReturned(string input, string expectedOutput)
        {
            bool replacementFunctionCalled = false;
            RunTest(input, expectedOutput, "t3", SymbolType.NonAcTerm, (inputTerm, database) =>
            {
                replacementFunctionCalled = true;
                return new[] { inputTerm };
            });
            Assert.True(replacementFunctionCalled);
        }

        private static void RunTest(string input, string expectedOutput, string termname, SymbolType symbolType, TermEvaluator evaluator)
        {
            // Arrange
            var database = TestUtilities.LoadStatements(input);
            database.Writer.SetEvaluator(termname, symbolType, evaluator);

            // Act
            database.ExecuteRewriteRules();

            // Assert
            var result = database.Reader.ReadCurrentFrame().ToSourceCode();
            Assert.True(StringComparer.InvariantCulture.Equals(expectedOutput, result));
        }
    }
}
