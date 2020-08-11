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
            ulong termIdentifier = SaveTerm(statement.Term).TermIdentifier.Value;
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
                SymbolType.TermList => new TermList 
                { 
                    Terms = term.Arguments.Select(arg => ReadTerm(arg.TermIdentifier.Value)).ToList() 
                },
                SymbolType.NonAcTerm => new NonAcTerm
                {
                    TermName = new Identifier { Name = termName },
                    Arguments = term.Arguments.Select(arg => ReadTerm(arg.TermIdentifier.Value)).ToList(),
                    ClassMemberMappings = ReadClassMemberMappings(term.MetaData)
                },
                _ => throw new NotImplementedException()
            };
        }

        private ClassMemberMappingsList ReadClassMemberMappings(Dictionary<TermMetaData, Symbol> metaData)
        {
            if (metaData == null || !metaData.TryGetValue(TermMetaData.ClassMemberMappings, out Symbol mappings))
            {
                return null;
            }

            // List of Identifiers expected
            var mappingValues = (TermList)ReadTerm(mappings.TermIdentifier.Value);

            return new ClassMemberMappingsList
            {
                ClassMembers = mappingValues.Terms.Cast<Identifier>().ToList()
            };
        }

        /// <summary>
        /// Saves an AST term and returns a symbol uniquely identifying it.
        /// </summary>
        public Symbol SaveTerm(ITrlTerm parseResult)
        {
            Term term;
            if (parseResult is Identifier id)
            {
                ulong idName = _stringMapper.Map(id.Name);
                term = new Term(new Symbol(idName, SymbolType.Identifier), null);
            }
            else if (parseResult is StringValue str)
            {
                ulong strName = _stringMapper.Map(str.Value);
                term = new Term(new Symbol(strName, SymbolType.String), null);
            }
            else if (parseResult is NumericValue num)
            {
                ulong numName = _stringMapper.Map(num.Value);
                term = new Term(new Symbol(numName, SymbolType.Number), null);
            }
            else if (parseResult is TermList termList)
            {
                var arguments = termList.Terms.Select(t => SaveTerm(t)).ToArray();
                term = new Term(new Symbol(MapConstants.NullOrEmpty, SymbolType.TermList), arguments);
            }
            else if (parseResult is NonAcTerm nonAcTerm)
            {
                var arguments = nonAcTerm.Arguments.Select(t => SaveTerm(t)).ToArray();
                ulong numTermName = _stringMapper.Map(nonAcTerm.TermName.Name);
                term = new Term(new Symbol(numTermName, SymbolType.NonAcTerm), arguments, GetMetadata(nonAcTerm));
            }
            else
            {
                throw new NotImplementedException();
            }

            var termId = _termMapper.Map(term);
            term.Name.TermIdentifier = termId;            
            return term.Name;
        }

        private Dictionary<TermMetaData, Symbol> GetMetadata(NonAcTerm nonAcTerm)
        {
            Dictionary<TermMetaData, Symbol> metadata = new Dictionary<TermMetaData, Symbol>();

            var identifiers = nonAcTerm.ClassMemberMappings?.ClassMembers?.Cast<ITrlTerm>().ToList();
            if (identifiers == null || !identifiers.Any())
            {
                return metadata;
            }

            // Store field mappings as a list
            TermList classMappings = new TermList
            {
                Terms = identifiers
            };
            metadata.Add(TermMetaData.ClassMemberMappings, SaveTerm(classMappings));

            return metadata;
        }
    }
}
