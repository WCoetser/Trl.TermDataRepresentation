using System;
using System.Collections.Generic;
using System.Linq;

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

        /// <summary>
        /// List of variables contained by this term and subterms
        /// </summary>
        public HashSet<ulong> Variables { get; }

        public Term(Symbol name, Symbol[] arguments, HashSet<ulong> variables, Dictionary<TermMetaData, Symbol> metaData = null)
        {
            Name = name;
            Arguments = arguments;
            MetaData = metaData;
            Variables = variables;

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

        internal Term CreateCopy(Symbol[] newArguments)
        {
            var newSymbol = new Symbol(Name.AssociatedStringValue, Name.Type);
            var newMetaData = MetaData != null ? new Dictionary<TermMetaData, Symbol>(MetaData) : null;
            var variables = new HashSet<ulong>(Variables);
            var copy = new Term(newSymbol, newArguments, variables, newMetaData);
            return copy;
        }
    }
}
