using System;
using System.Collections.Generic;
using System.Linq;
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

        /// <summary>
        /// Maps integers for string labels to integers for term identifiers.
        /// </summary>
        private readonly Dictionary<ulong, HashSet<ulong>> _labelToTermMapper;

        public TermDatabase()
        {
            _stringMapper = new StringMapper();
            _termMapper = new EqualityComparerMapper<Term>(new IntegerMapperTermEqualityComparer());
            _labelToTermMapper = new Dictionary<ulong, HashSet<ulong>>();
        }

        /// <summary>
        /// Saves a statement.
        /// </summary>
        public void SaveStatement(Statement statement)
        {
            ulong termIdentifier = SaveTerm(statement.Term);
            var term = _termMapper.ReverseMap(termIdentifier);
            foreach (var identifier in statement.Label.Identifiers)
            {
                ulong labelId = _stringMapper.Map(identifier.Name);
                if (!_labelToTermMapper.TryGetValue(labelId, out HashSet<ulong> referencedTerms))
                {
                    referencedTerms = new HashSet<ulong>();
                    _labelToTermMapper.Add(labelId, referencedTerms);
                }
                term.Labels.Add(labelId);
                referencedTerms.Add(termIdentifier);
            }
        }

        /// <summary>
        /// Saves a list of statemnents.
        /// </summary>
        /// <param name="statementList"></param>
        public void SaveStatements(StatementList statementList)
        {
            foreach (var statement in statementList.Statements)
            {
                SaveStatement(statement);
            }
        }

        /// <summary>
        /// Gets the statement for the label, if it does not exist returns null.
        /// </summary>
        public StatementList ReadStatementsForLabel(string label)
        {
            if (!_stringMapper.TryGetMappedValue(label, out ulong? labelInteger))
            {
                return null;
            }

            if (!_labelToTermMapper.TryGetValue(labelInteger.Value, out HashSet<ulong> associatedTermIds)
                || !associatedTermIds.Any())
            {
                return null;
            }

            var returnStatements = new StatementList
            {
                Statements = new List<Statement>()
            };

            foreach (var termId in associatedTermIds)
            {
                var returnLabel = new Label
                {
                    Identifiers = new List<Identifier>()
                };
                var dbTerm = _termMapper.ReverseMap(termId);
                foreach (var labelId in dbTerm.Labels)
                {
                    returnLabel.Identifiers.Add(new Identifier
                    {
                        Name = _stringMapper.ReverseMap(labelId)
                    });
                }
                returnStatements.Statements.Add(new Statement
                {
                    Label = returnLabel,
                    Term = ReadTerm(termId)
                });
            }
            return returnStatements;
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
