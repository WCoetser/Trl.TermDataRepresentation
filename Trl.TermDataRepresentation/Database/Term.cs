using System;

namespace Trl.TermDataRepresentation.Database
{
    /// <summary>
    /// Represents nodes in a directed acyclic graph.
    /// All terms are unique in the sense that the same term and subterm
    /// may only exist once in the database. Therefore, terms share common sub-expressions.
    /// </summary>
    public class Term
    {
        /// <summary>
        /// Identifies what type of term this is and the name of the term is.
        /// </summary>
        public Symbol Name { get; }

        /// <summary>
        /// Term arguments. In the case where this represents a, identifier, number, or string, 
        /// this should be null.
        /// </summary>
        public Symbol[] Arguments { get; }

        public Term(Symbol name, Symbol[] arguments)
        {
            Name = name;
            Arguments = arguments;
        }
    }
}
