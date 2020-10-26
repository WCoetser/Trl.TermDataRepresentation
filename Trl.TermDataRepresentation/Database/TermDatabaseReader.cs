using System;
using System.Collections.Generic;
using System.Linq;
using Trl.TermDataRepresentation.Parser.AST;

namespace Trl.TermDataRepresentation.Database
{
    /// <summary>
    /// Reads data from <see cref="TermDatabase"/>
    /// </summary>
    public class TermDatabaseReader
    {
        private readonly TermDatabase _termDatabase;

        internal TermDatabaseReader(TermDatabase termDatabase)
        {
            _termDatabase = termDatabase;
        }

        public List<RewriteRule> ReadAllRewriteRules()
        {
            List<RewriteRule> substitutions = new List<RewriteRule>();
            foreach (var s in _termDatabase.CurrentFrame.Substitutions)
            {
                substitutions.Add(new RewriteRule
                {
                    MatchTerm = ReadTerm(s.MatchTermIdentifier),
                    SubstituteTerm = ReadTerm(s.SubstituteTermIdentifier)
                });
            }
            return substitutions;
        }

        /// <summary>
        /// Gets all current root terms and rewrite rules that makes up the current frame.
        /// </summary>
        /// <returns></returns>
        public StatementList ReadCurrentFrame()
        {
            var returnStatements = new StatementList
            {
                Statements = new List<TermStatement>(),
                RewriteRules = new List<RewriteRule>()
            };
            foreach (var root in _termDatabase.CurrentFrame.RootTerms)
            {
                returnStatements.Statements.Add(ReadRootTermStatement(root));
            }
            foreach (var rewriteRule in ReadAllRewriteRules())
            {
                returnStatements.RewriteRules.Add(rewriteRule);
            }
            return returnStatements;
        }

        /// <summary>
        /// Gets the internal terms for a label
        /// </summary>
        /// <param name="label"></param>
        /// <returns></returns>
        public IEnumerable<Term> ReadInternalTermsForLabel(string label)
        {
            if (!_termDatabase.StringMapper.TryGetMappedValue(label, out ulong? labelInteger))
            {
                return Enumerable.Empty<Term>();
            }

            if (!_termDatabase.LabelToTerm.TryGetValue(labelInteger.Value, out HashSet<ulong> associatedTermIds)
                || !associatedTermIds.Any())
            {
                return Enumerable.Empty<Term>();
            }

            LinkedList<Term> retTerms = new LinkedList<Term>();
            foreach (var termId in associatedTermIds.Intersect(_termDatabase.CurrentFrame.RootTerms))
            {
                retTerms.AddLast(GetInternalTermById(termId));
            }
            return retTerms;
        }

        /// <summary>
        /// Gets the statement for the label, if it does not exist returns null.
        /// </summary>
        public StatementList ReadStatementsForLabel(string label)
        {
            var returnStatements = new StatementList
            {
                Statements = new List<TermStatement>()
            };

            var terms = ReadInternalTermsForLabel(label);
            if (!terms.Any())
            {
                return null;
            }

            foreach (var internalTerm in terms)
            {                
                returnStatements.Statements.Add(ReadRootTermStatement(internalTerm.Name.TermIdentifier.Value));
            }
            return returnStatements;
        }

        /// <summary>
        /// Reads the given root term and assign the label list
        /// </summary>
        public TermStatement ReadRootTermStatement(ulong termId)
        {
            var returnLabel = new Label
            {
                Identifiers = new List<Identifier>()
            };
            var dbTerm = _termDatabase.TermMapper.ReverseMap(termId);
            foreach (var labelId in dbTerm.Labels)
            {
                returnLabel.Identifiers.Add(new Identifier
                {
                    Name = _termDatabase.StringMapper.ReverseMap(labelId)
                });
            }
            return new TermStatement
            {
                Label = returnLabel,
                Term = ReadTerm(termId)
            };
        }

        /// <summary>
        /// Reconstructs a term from an identifier, producing an AST representation of the term.
        /// </summary>
        public ITrlTerm ReadTerm(ulong termIdentifier)
        {
            var term = _termDatabase.TermMapper.ReverseMap(termIdentifier);
            var termName = _termDatabase.StringMapper.ReverseMap(term.Name.AssociatedStringValue);
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
                SymbolType.Variable => new Variable
                {
                    Name = termName
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
        /// Gets a term in it's database form from the mapped integer value.
        /// </summary>
        /// <param name="termIdentifier">Identifier for the term.</param>
        /// <returns>The term.</returns>
        public Term GetInternalTermById(ulong termIdentifier)
            => _termDatabase.TermMapper.ReverseMap(termIdentifier);

        /// <summary>
        /// Gets the current frame as a collection of term identifiers.
        /// </summary>
        /// <returns></returns>
        public IReadOnlyCollection<ulong> ReadCurrentFrameRootTermIds()
        {
            return _termDatabase.CurrentFrame.RootTerms.ToList().AsReadOnly();
        }

        /// <summary>
        /// Gets all terms and subterms contained in an expression tree for the given term ID.
        /// </summary>        
        public IEnumerable<Term> GetAllTermsAndSubtermsForTermId(ulong startTermId)
        {
            var next = new Stack<ulong>();
            var retVal = new List<Term>();
            next.Push(startTermId);
            while (next.Count > 0)
            {
                var current = next.Pop();
                var term = GetInternalTermById(current);
                retVal.Add(term);
                if (term.Arguments != null)
                {
                    foreach (var arg in term.Arguments)
                    {
                        next.Push(arg.TermIdentifier.Value);
                    }
                }
            }
            return retVal;
        }
    }
}
