using System;
using System.Collections.Generic;

namespace Trl.TermDataRepresentation.Database
{
    /// <summary>
    /// Represents nodes in a directed acyclic graph.
    /// All terms are unique in the sense that the same term and subterm
    /// may only exist once in the database. Therefore, terms share common sub-expressions.
    /// </summary>
    public class Term
    {
        private Lazy<HashSet<ulong>> _labels = new Lazy<HashSet<ulong>>(() => new HashSet<ulong>());

        /// <summary>
        /// Mapped integers for string labels assocated with this term.
        /// </summary>
        public HashSet<ulong> Labels => _labels.Value;

        /// <summary>
        /// Identifies what type of term this is and the name of the term is.
        /// </summary>
        public Symbol Name { get; }

        /// <summary>
        /// Term arguments. In the case where this represents a, identifier, number, or string, 
        /// this should be null.
        /// </summary>
        public Symbol[] Arguments { get; }

        /// <summary>
        /// Represents metadata about a term, for example the class member mappings.
        /// Affects equality, metadata must also be the same for terms to be equal.
        /// </summary>
        public Dictionary<TermMetaData, Symbol> MetaData { get; }

        public Term(Symbol name, Symbol[] arguments, Dictionary<TermMetaData, Symbol> metaData = null)
        {
            Name = name;
            Arguments = arguments;
            MetaData = metaData;

            // Arguments must have identifiers
            if (arguments != null)
            {
                for (int i = 0; i < arguments.Length; i++)
                {
                    if (!arguments[i].TermIdentifier.HasValue)
                    {
                        throw new InvalidOperationException();
                    }
                }
            }
        }
    }
}
