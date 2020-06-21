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
            var term = _termMapper.ReverseMap(termIdentifier);
            var termName = _stringMapper.ReverseMap(term.Name.AssociatedStringValue);
            return term.Name.Type switch
            {
                SymbolType.Identifier => new Identifier { Name = termName },
                SymbolType.String => new StringValue { Value = termName },
                SymbolType.Number => new NumericValue { Value = termName },
                _ => throw new NotImplementedException()
            };
        }

        /// <summary>
        /// Saves an AST term and returns it's symbol identifier (see <see cref="Symbol.TermIdentifier"/>).
        /// </summary>
        public ulong SaveTerm(ITrlTerm parseResult)
        {
            Term t;
            if (parseResult is Identifier id)
            {
                ulong idName = _stringMapper.Map(id.Name);
                t = new Term(new Symbol(idName, SymbolType.Identifier), null);
            }
            else if (parseResult is StringValue str)
            {
                ulong strName = _stringMapper.Map(str.Value);
                t = new Term(new Symbol(strName, SymbolType.String), null);
            }
            else if (parseResult is NumericValue num)
            {
                ulong numName = _stringMapper.Map(num.Value);
                t = new Term(new Symbol(numName, SymbolType.Number), null);
            }
            else
            {
                throw new NotImplementedException();
            }
            var termId = _termMapper.Map(t);
            t.Name.TermIdentifier = termId;
            return termId;
        }
    }
}
