﻿using System;
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
            const ulong TERM_NAME_MAPPED_VALUE = 1;
            const ulong NUMBER_MAPPED_VALUE = 2;
            var termName1 = new Symbol(TERM_NAME_MAPPED_VALUE, SymbolType.Identifier);
            var termName2 = new Symbol(TERM_NAME_MAPPED_VALUE, SymbolType.Identifier);
            var termArgs1 = new Symbol(NUMBER_MAPPED_VALUE, SymbolType.Number);
            var termArgs2 = new Symbol(NUMBER_MAPPED_VALUE, SymbolType.Number);
            termName1.TermIdentifier = null; // these must be null to simulate an unloaded term
            termName2.TermIdentifier = null; // these must be null to simulate an unloaded term
            termArgs1.TermIdentifier = 3;
            termArgs2.TermIdentifier = termArgs1.TermIdentifier;
            var term1 = new Term(termName1, new[] { termArgs1 }, new HashSet<ulong>());
            var term2 = new Term(termName2, new[] { termArgs2 }, new HashSet<ulong>());

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
            const ulong TERM_NAME_MAPPED_VALUE = 1;
            const ulong NUMBER_MAPPED_VALUE = 2;
            var termName1 = new Symbol(TERM_NAME_MAPPED_VALUE, SymbolType.Identifier);
            var termName2 = new Symbol(TERM_NAME_MAPPED_VALUE, SymbolType.Identifier);
            var termArgs1 = new Symbol(NUMBER_MAPPED_VALUE, SymbolType.Number);
            var termArgs2 = new Symbol(NUMBER_MAPPED_VALUE, SymbolType.String);
            termName1.TermIdentifier = null; // these must be null to simulate an unloaded term
            termName2.TermIdentifier = null; // these must be null to simulate an unloaded term
            termArgs1.TermIdentifier = 3; // cause of inequality is in argument non-equality
            termArgs2.TermIdentifier = 4;
            var term1 = new Term(termName1, new[] { termArgs1 }, new HashSet<ulong>());
            var term2 = new Term(termName2, new[] { termArgs2 }, new HashSet<ulong>());

            // Act
            var equals = _comparer.Equals(term1, term2);

            // Assert
            Assert.False(equals);
        }


        [Fact]
        public void ShouldRespectHashingAndEqualityForMappedSymbolsEquals()
        {
            // Arrange
            const ulong TERM_NAME_MAPPED_VALUE = 1;
            const ulong NUMBER_MAPPED_VALUE = 2;
            var termName1 = new Symbol(TERM_NAME_MAPPED_VALUE, SymbolType.Identifier);
            var termName2 = new Symbol(TERM_NAME_MAPPED_VALUE, SymbolType.Identifier);
            var termArgs1 = new Symbol(NUMBER_MAPPED_VALUE, SymbolType.Number);
            var termArgs2 = new Symbol(NUMBER_MAPPED_VALUE, SymbolType.Number);
            termName1.TermIdentifier = 4; // these must not be null to simulate a loaded term
            termName2.TermIdentifier = termName1.TermIdentifier; // these must not be null to simulate a loaded term
            termArgs1.TermIdentifier = 3;
            termArgs2.TermIdentifier = termArgs1.TermIdentifier;
            var term1 = new Term(termName1, new[] { termArgs1 }, new HashSet<ulong>());
            var term2 = new Term(termName2, new[] { termArgs2 }, new HashSet<ulong>());

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
            const ulong TERM_NAME_MAPPED_VALUE = 1;
            const ulong NUMBER_MAPPED_VALUE = 2;
            var termName1 = new Symbol(TERM_NAME_MAPPED_VALUE, SymbolType.Identifier);
            var termName2 = new Symbol(TERM_NAME_MAPPED_VALUE, SymbolType.Identifier);
            var termArgs1 = new Symbol(NUMBER_MAPPED_VALUE, SymbolType.Number);
            var termArgs2 = new Symbol(NUMBER_MAPPED_VALUE, SymbolType.String);
            termName1.TermIdentifier = 4; // these must not be null to simulate a loaded term
            termName2.TermIdentifier = 5; // these must not be null to simulate a loaded term
            termArgs1.TermIdentifier = 3;
            termArgs2.TermIdentifier = termArgs1.TermIdentifier;
            var term1 = new Term(termName1, new[] { termArgs1 }, new HashSet<ulong>());
            var term2 = new Term(termName2, new[] { termArgs2 }, new HashSet<ulong>());

            // Act
            var equals = _comparer.Equals(term1, term2);

            // Assert
            Assert.False(equals);
        }

        [Fact]
        public void ShouldRespectHashingAndEqualiyForUnmappedLists()
        {
            // Arrange
            var termName1 = new Symbol(0, SymbolType.TermList);
            var termName2 = new Symbol(0, SymbolType.TermList);
            var num1 = new Symbol(1, SymbolType.Number);
            num1.TermIdentifier = 3;
            var num2 = new Symbol(1, SymbolType.Number);
            num2.TermIdentifier = 3;
            var term1 = new Term(termName1, new[] { num1 }, new HashSet<ulong>());
            var term2 = new Term(termName2, new[] { num2 }, new HashSet<ulong>());

            // Act
            var equals = _comparer.Equals(term1, term2);

            // Assert
            Assert.True(equals);
        }

        [Fact]
        public void ShouldThrowExceptionIfTermArgumentsNotMapped()
        {
            // Arrange
            var termName1 = new Symbol(1, SymbolType.Identifier);
            var termArgs1 = new Symbol(2, SymbolType.Number);
            termArgs1.TermIdentifier = null; // this should break it

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() =>
            {
                var term1 = new Term(termName1, new[] { termArgs1 }, new HashSet<ulong>());
            });            
        }
    }
}
