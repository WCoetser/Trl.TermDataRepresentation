namespace Trl.TermDataRepresentation.Database
{
    /// <summary>
    /// All symbols are unique in the sense that no term may have duplicates in the term database.
    /// Common sub-terms are mapped to the same integer by the integer mappers, therefore there should 
    /// be common sub-expression sharing.
    /// </summary>
    public class Symbol
    {
        /// <summary>
        /// The integer mapped value uniquely identifying this symbol that can be used for
        /// hashing and equality purposes. Should be interpreted in the context of 
        /// <see cref="SymbolType"/> given by <see cref="Type"/>.
        /// </summary>
        public ulong? TermIdentifier { get; set; }
                
        /// <summary>
        /// This integer that is mapped to the string value for this term.
        /// Used to create human readable version of terms in the dabase.
        /// Should be used with the string integer mapper in the term database (<see cref="TermDatabase"/>)
        /// If Identifier is not assigned, this is used in equality tests.
        /// </summary>
        public ulong AssociatedStringValue { get; }

        /// <summary>
        /// Indicates what this symbol represents.
        /// </summary>
        public SymbolType Type { get; }

        public Symbol(ulong associatedStringValue, SymbolType type)
            => (AssociatedStringValue, Type) = (associatedStringValue, type);
    }
}
