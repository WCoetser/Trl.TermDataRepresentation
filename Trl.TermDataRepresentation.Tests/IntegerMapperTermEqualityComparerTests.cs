using System;
using System.Collections.Generic;
using Trl.TermDataRepresentation.Database;
using Xunit;

namespace Trl.TermDataRepresentation.Tests
{
    public class IntegerMapperTermEqualityComparerTests
    {
        private readonly IntegerMapperTermEqualityComparer _comparer;

        public IntegerMapperTermEqualityComparerTests()
        {
            _comparer = new IntegerMapperTermEqualityComparer();
        }

        [Fact]
        public void ShouldRespectHashingAndEqualityForUnmappedSymbolsEquals()
        {
            // Arrange
            var term1 = new Term(new Symbol(1, SymbolType.Identifier), null, null, new Dictionary<TermMetaData, Term>());
            var term2 = new Term(new Symbol(1, SymbolType.Identifier), null, null, new Dictionary<TermMetaData, Term>());

            // Act
            var equals = _comparer.Equals(term1, term2);
            var hashCode1 = _comparer.GetHashCode(term1);
            var hashCode2 = _comparer.GetHashCode(term2);

            // Assert
            Assert.True(equals);
            Assert.Equal(hashCode1, hashCode2);
        }

        [Fact]
        public void ShouldRespectHashingAndEqualityForUnmappedSymbolsNotEquals()
        {
            // Arrange
            var term1 = new Term(new Symbol(1, SymbolType.Identifier), null, null, new Dictionary<TermMetaData, Term>());
            var term2 = new Term(new Symbol(2, SymbolType.Identifier), null, null, new Dictionary<TermMetaData, Term>());

            // Act
            var equals = _comparer.Equals(term1, term2);
            var hashCode1 = _comparer.GetHashCode(term1);
            var hashCode2 = _comparer.GetHashCode(term2);

            // Assert
            Assert.False(equals);
            Assert.NotEqual(hashCode1, hashCode2);
        }


        [Fact]
        public void ShouldRespectHashingAndEqualityForMappedSymbolsEquals()
        {
            // Arrange
            var term1 = new Term(new Symbol(1, SymbolType.Identifier), null, null, new Dictionary<TermMetaData, Term>());
            var term2 = new Term(new Symbol(1, SymbolType.Identifier), null, null, new Dictionary<TermMetaData, Term>());
            term1.Name.TermIdentifier = 1;
            term2.Name.TermIdentifier = 1;

            // Act
            var equals = _comparer.Equals(term1, term2);
            var hashCode1 = _comparer.GetHashCode(term1);
            var hashCode2 = _comparer.GetHashCode(term2);

            // Assert
            Assert.True(equals);
            Assert.Equal(hashCode1, hashCode2);
        }

        [Fact]
        public void ShouldRespectHashingAndEqualityForMappedSymbolsNotEquals()
        {
            // Arrange
            var term1 = new Term(new Symbol(1, SymbolType.Identifier), null, null, new Dictionary<TermMetaData, Term>());
            var term2 = new Term(new Symbol(2, SymbolType.Identifier), null, null, new Dictionary<TermMetaData, Term>());
            term1.Name.TermIdentifier = 1;
            term2.Name.TermIdentifier = 2;

            // Act
            var equals = _comparer.Equals(term1, term2);
            var hashCode1 = _comparer.GetHashCode(term1);
            var hashCode2 = _comparer.GetHashCode(term2);

            // Assert
            Assert.False(equals);
            Assert.NotEqual(hashCode1, hashCode2);
        }

        [Fact]
        public void ShouldRespectHashingAndEqualiyForUnmappedLists()
        {
            // Arrange
            var term1 = new Term(new Symbol(1, SymbolType.Identifier), null, null, new Dictionary<TermMetaData, Term>());
            var term2 = new Term(new Symbol(2, SymbolType.Identifier), null, null, new Dictionary<TermMetaData, Term>());
            var term3 = new Term(new Symbol(1, SymbolType.Identifier), null, null, new Dictionary<TermMetaData, Term>());
            var term4 = new Term(new Symbol(2, SymbolType.Identifier), null, null, new Dictionary<TermMetaData, Term>());
            term1.Name.TermIdentifier = 1;
            term2.Name.TermIdentifier = 2;
            term3.Name.TermIdentifier = 1;
            term4.Name.TermIdentifier = 2;
            var list1 = new Term(new Symbol(0, SymbolType.TermList), new[] { term1, term2 }, new HashSet<Term>(), new Dictionary<TermMetaData, Term>());
            var list2 = new Term(new Symbol(0, SymbolType.TermList), new[] { term3, term4 }, new HashSet<Term>(), new Dictionary<TermMetaData, Term>());

            // Act
            var equals = _comparer.Equals(list1, list2);
            var hashCode1 = _comparer.GetHashCode(list1);
            var hashCode2 = _comparer.GetHashCode(list2);

            // Assert
            Assert.True(equals);
            Assert.Equal(hashCode1, hashCode2);
        }

        [Fact]
        public void ShouldThrowExceptionIfTermArgumentsNotMapped()
        {
            // Arrange
            var termArg = new Term(new Symbol(1, SymbolType.Identifier), null, null, new Dictionary<TermMetaData, Term>());
            termArg.Name.TermIdentifier = null;

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() =>
            {
                var term1 = new Term(new Symbol(1, SymbolType.NonAcTerm), new [] { termArg }, new HashSet<Term>(), new Dictionary<TermMetaData, Term>());
            });            
        }
    }
}
