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
        private readonly Lazy<HashSet<ulong>> _labels = new Lazy<HashSet<ulong>>(() => new HashSet<ulong>());

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
        public Term[] Arguments { get; }

        /// <summary>
        /// Represents metadata about a term, for example the class member mappings.
        /// Affects equality, metadata must also be the same for terms to be equal.
        /// </summary>
        public Dictionary<TermMetaData, Term> MetaData { get; }

        /// <summary>
        /// List of variables contained by this term and subterms
        /// </summary>
        public HashSet<Term> Variables { get; }

        /// <summary>
        /// Important: Use <see cref="TermDatabaseWriter"/> to create instances of this class in order to correctly maintain
        /// term identifier for unique subtrees. (See <see cref="Equals(object)"/> and <see cref="GetHashCode"/> functions).
        /// Only call this directly for unit tests.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="arguments"></param>
        /// <param name="variables"></param>
        /// <param name="metaData"></param>
        public Term(Symbol name, Term[] arguments, HashSet<Term> variables, Dictionary<TermMetaData, Term> metaData = null)
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
                    if (!arguments[i].Name.TermIdentifier.HasValue)
                    {
                        throw new InvalidOperationException();
                    }
                }
            }
        }

        public override bool Equals(object obj)
        {
            var other = obj as Term;
            return other != null && Name.TermIdentifier.Value == other.Name.TermIdentifier.Value;
        }

        public static bool operator ==(Term lhs, Term rhs)
        {
            return (lhs, rhs) switch
            {
                (null, null) => true,
                (_, null) => false,
                (null, _) => false,
                (_, _) => lhs.Equals(rhs)
            };
        }

        public static bool operator != (Term lhs, Term rhs) => !(lhs == rhs);

        public override int GetHashCode() => Name.TermIdentifier.Value.GetHashCode();
    }
}
