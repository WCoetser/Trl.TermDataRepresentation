using System;
using Trl.TermDataRepresentation.Database.Mutations;
using Trl.TermDataRepresentation.Parser;
using Xunit;

namespace Trl.TermDataRepresentation.Tests
{
    public class ConvertTermsToRewriteRulesTests
    {
        private readonly ITermDatabaseMutation _mutation;

        public ConvertTermsToRewriteRulesTests()
        {
            _mutation = new ConvertCommonTermsToRewriteRules();
        }

        [InlineData("root: t(s(), s(), 123, \"abc\");", "﻿root: t(s0,s0,123,\"abc\");s0 => s();")]
        [InlineData("plant1: tree(branch(leaf1, leaf2)); plant2: tree(branch(leaf1, leaf2));", "plant1,plant2: tree(branch(leaf1,leaf2));")]
        [InlineData("root: t(s(q(1)), s(q(1)), 123, \"abc\");", "﻿root: t(s0,s0,123,\"abc\");s0 => s(q(1));")]
        [InlineData("root: r(t((1,2,3)), s((1,2,3)), (1,2,3));", "﻿root: r(t(l0),s(l0),l0);l0 => (1,2,3);")]
        [InlineData("point1: (1,2,3); point2: (1,2,3);", "﻿point1,point2: (1,2,3);")]
        [Theory]
        public void ShouldConvertTermsToRewriteRulesForDuplicates(string testInput, string expectedOutput)
        {
            // Arrange
            var database = TestUtilities.LoadStatements(testInput);

            // Act
            database.MutateDatabase(_mutation);

            // Assert
            var output = database.Reader.ReadCurrentFrame().ToSourceCode();
            Assert.True(StringComparer.InvariantCulture.Equals(expectedOutput, output));
        }

        [Fact]
        public void ShouldPreserveUnrelatedRewriteRules()
        {
            // Arrange
            var database = TestUtilities.LoadStatements("s(1) => t(1); root1: a(b(1)); root2: c(b(1));");

            // Act
            database.MutateDatabase(_mutation);

            // Assert
            var output = database.Reader.ReadCurrentFrame().ToSourceCode();
            Assert.Contains("s(1) => t(1);", output);
        }
    }
}
