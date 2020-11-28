using System.Linq;
using Trl.TermDataRepresentation.Database.Unification;
using Xunit;

namespace Trl.TermDataRepresentation.Tests
{
    public class UnifierCalculationTests
    {
        [InlineData("lhs: t(1); rhs: s(1);")]
        [InlineData("lhs: t(s(:x), :x); rhs: s(s(1), 2);")]
        [InlineData("lhs: point<x,y>(1,2); rhs: point<y,z>(1,2);")]
        [InlineData("lhs: (1,2,3); rhs: (4,5,6);")]
        [InlineData("lhs: :x; rhs: t(s(:x));")]
        [Theory]
        public void ShouldNotUnify(string testCode)
        {
            // Arrange
            var database = TestUtilities.LoadStatements(testCode);
            var unification = new UnifierCalculation(database);
            var lhs = database.Reader.ReadInternalTermsForLabel("lhs").Single();
            var rhs = database.Reader.ReadInternalTermsForLabel("rhs").Single();

            // Act
            var eq = new Equation { Lhs = lhs, Rhs = rhs };
            var (substitutions, succeed) = unification.GetSyntacticUnifier(eq);

            // Assert
            Assert.False(succeed);
            Assert.Empty(substitutions);
        }

        [InlineData("lhs: t(:x, :z); rhs: t(:z, 2);", new[] { ":x => 2;", ":z => 2;" })]
        [InlineData("lhs: t(:x, 2); rhs: t(1, 2);", new[] { ":x => 1;" })]
        [InlineData("lhs: t(:x, :y); rhs: t(:y, 2);", new[] { ":x => 2;", ":y => 2;" })]
        [InlineData("lhs: t(1, Pi, \"aaa\"); rhs: t(1, Pi, \"aaa\");", new string[0])]
        [InlineData("lhs: point<x,y>(1,2); rhs: point<x,y>(1,2);", new string[0])]
        [InlineData("lhs: (:x, 2); rhs: (1, 2);", new[] { ":x => 1;" })]
        [InlineData("lhs: (:x, :y); rhs: (:y, 2);", new[] { ":x => 2;", ":y => 2;" })]
        [InlineData("lhs: (t(:a, :b)); rhs: (t(:b, :a));", new[] { ":a => :b;" })]
        [InlineData("lhs: 1.2; rhs: 1.2;", new string[0])]
        [InlineData("lhs: Pi; rhs: Pi;", new string[0])]
        [InlineData("lhs: \"a\"; rhs: \"a\";", new string[0])]
        [InlineData("lhs: :x; rhs: :x;", new string[0])]
        [InlineData("lhs: :x; rhs: 123;", new[] { ":x => 123;" })]
        [Theory]
        public void ShouldUnify(string testCode, string[] substitutions)
        {
            // Arrange
            var database = TestUtilities.LoadStatements(testCode);
            var unification = new UnifierCalculation(database);
            var lhs = database.Reader.ReadInternalTermsForLabel("lhs").Single();
            var rhs = database.Reader.ReadInternalTermsForLabel("rhs").Single();

            // Act
            var eq = new Equation { Lhs = lhs, Rhs = rhs };
            var unifier = unification.GetSyntacticUnifier(eq);

            // Assert
            Assert.True(unifier.succeed);
            var unifierStrings = unifier.substitutions.Select(u => u.ToSourceCode(database));
            Assert.True(TestUtilities.ContainsTheSameValues(substitutions, unifierStrings));
        }
    }
}
