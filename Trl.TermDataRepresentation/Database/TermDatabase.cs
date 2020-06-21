using System;
using Trl.IntegerMapper;
using Trl.IntegerMapper.EqualityComparerIntegerMapper;
using Trl.IntegerMapper.StringIntegerMapper;
using Trl.TermDataRepresentation.Parser.AST;

namespace Trl.TermDataRepresentation.Database
{
    /// <summary>
    /// Main storage for terms.
    /// </summary>    
    public class TermDatabase
    {
        /// <summary>
        /// Used to create human readable representation of term database content.
        /// </summary>
        private readonly IIntegerMapper<string> _stringMapper;

        /// <summary>
        /// Stores terms, mapping them to unique integers. The same term may not exist
        /// more than once.
        /// </summary>
        private readonly IIntegerMapper<Term> _termMapper;

        public TermDatabase()
        {
            _stringMapper = new StringMapper();
            _termMapper = new EqualityComparerMapper<Term>(new IntegerMapperTermEqualityComparer());
        }

        /// <summary>
        /// Reconstructs a term from an identifier, producing an AST representation of the term.
        /// </summary>
        public ITrlTerm ReadTerm(ulong termIdentifier)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Saves an AST term and returns it's symbol identifier (see <see cref="Symbol.TermIdentifier"/>).
        /// </summary>
        public ulong SaveTerm(ITrlTerm parseResult)
        {
            throw new NotImplementedException();
        }
    }
}
