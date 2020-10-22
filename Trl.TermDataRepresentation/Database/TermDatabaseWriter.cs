using System;
using System.Collections.Generic;
using System.Linq;
using Trl.IntegerMapper;
using Trl.TermDataRepresentation.Parser.AST;

namespace Trl.TermDataRepresentation.Database
{
    /// <summary>
    /// Writes data to <see cref="TermDatabase"/>
    /// </summary>
    public class TermDatabaseWriter
    {
        private readonly TermDatabase _termDatabase;

        internal TermDatabaseWriter(TermDatabase termDatabase)
        {
            _termDatabase = termDatabase;
        }

        public void StoreRewriteRule(RewriteRule rule)
        {
            _termDatabase.CurrentFrame.Substitutions.Add(new Substitution
            {
                MatchTermIdentifier = StoreTerm(rule.MatchTerm).TermIdentifier.Value,
                SubstituteTermIdentifier = StoreTerm(rule.SubstituteTerm).TermIdentifier.Value
            });
        }

        /// <summary>
        /// Assigns a label to a term for easy later retrieval.
        /// </summary>
        /// <param name="termIdToLabel">ID of the term recieving the label.</param>
        /// <param name="labelToAssign">The label to assign.</param>
        public void LabelTerm(ulong termIdToLabel, string labelToAssign)
        {
            ulong labelId = _termDatabase.StringMapper.Map(labelToAssign);
            var term = _termDatabase.Reader.GetInternalTermById(termIdToLabel);
            if (!_termDatabase.LabelToTerm.TryGetValue(labelId, out HashSet<ulong> referencedTerms))
            {
                referencedTerms = new HashSet<ulong>();
                _termDatabase.LabelToTerm.Add(labelId, referencedTerms);
            }
            term.Labels.Add(labelId);
            referencedTerms.Add(term.Name.TermIdentifier.Value);
        }

        /// <summary>
        /// Saves a statement.
        /// </summary>
        public void StoreStatement(TermStatement statement)
        {
            ulong termIdentifier = StoreTerm(statement.Term).TermIdentifier.Value;
            if (statement.Label != null)
            {
                foreach (var identifier in statement.Label.Identifiers)
                {
                    LabelTerm(termIdentifier, identifier.Name);
                }
            }
            SetAsRootTerm(termIdentifier);
        }

        /// <summary>
        /// Adds the term ID to the current frame for the term database,
        /// making it a root term.
        /// </summary>
        public void SetAsRootTerm(ulong termIdentifier)
        {
            _termDatabase.CurrentFrame.RootTerms.Add(termIdentifier);
        }

        /// <summary>
        /// Saves a list of statemnents.
        /// </summary>
        /// <param name="statementList"></param>
        public void StoreStatements(StatementList statementList)
        {
            foreach (var statement in statementList.Statements)
            {
                StoreStatement(statement);
            }

            foreach (var r in statementList.RewriteRules)
            {
                StoreRewriteRule(r);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="value"></param>
        /// <param name="symbolType"></param>
        /// <returns></returns>
        public Symbol StoreAtom(string value, SymbolType symbolType)
        {
            if (symbolType != SymbolType.Number 
                && symbolType != SymbolType.Identifier
                && symbolType != SymbolType.String)
            {
                throw new Exception($"Cannot store symbol of type {symbolType} using {nameof(StoreAtom)}");
            }

            ulong numName = _termDatabase.StringMapper.Map(value);
            var term = new Term(new Symbol(numName, symbolType), null, new HashSet<ulong>());
            StoreTermAndAssignId(term);
            return term.Name;
        }

        public Symbol StoreVariable(string name)
        {
            ulong varName = _termDatabase.StringMapper.Map(name);
            var variables = new HashSet<ulong>();
            var term = new Term(new Symbol(varName, SymbolType.Variable), null, variables);
            StoreTermAndAssignId(term);
            variables.Add(term.Name.TermIdentifier.Value);
            return term.Name;
        }

        public Symbol StoreTermList(Symbol[] terms)
        {
            var variables = new HashSet<ulong>();
            foreach (var termSymbol in terms)
            {
                var t = _termDatabase.Reader.GetInternalTermById(termSymbol.TermIdentifier.Value);
                variables.UnionWith(t.Variables);
            }
            var term = new Term(new Symbol(MapConstants.NullOrEmpty, SymbolType.TermList), terms, variables);
            StoreTermAndAssignId(term);
            return term.Name;
        }

        public Symbol StoreNonAcTerm(string termName, Symbol[] arguments, Dictionary<TermMetaData, Symbol> metadata)
        {
            var variables = new HashSet<ulong>();
            foreach (var arg in arguments)
            {
                var t = _termDatabase.Reader.GetInternalTermById(arg.TermIdentifier.Value);
                variables.UnionWith(t.Variables);
            }
            ulong numTermName = _termDatabase.StringMapper.Map(termName);
            var term = new Term(new Symbol(numTermName, SymbolType.NonAcTerm), arguments, variables, metadata);
            StoreTermAndAssignId(term);
            return term.Name;
        }

        /// <summary>
        /// Saves an AST term and returns a symbol uniquely identifying it.
        /// Does no add term to set of root terms for rewriting.
        /// </summary>
        public Symbol StoreTerm(ITrlTerm parseResult)
        {
            if (parseResult is Identifier id)
            {
                return StoreAtom(id.Name, SymbolType.Identifier);
            }
            else if (parseResult is StringValue str)
            {
                return StoreAtom(str.Value, SymbolType.String);
            }
            else if (parseResult is NumericValue num)
            {
                return StoreAtom(num.Value, SymbolType.Number);
            }
            else if (parseResult is TermList termList)
            {
                var arguments = termList.Terms.Select(t => StoreTerm(t)).ToArray();
                return StoreTermList(arguments);
            }
            else if (parseResult is NonAcTerm nonAcTerm)
            {
                var arguments = nonAcTerm.Arguments.Select(t => StoreTerm(t)).ToArray();
                return StoreNonAcTerm(nonAcTerm.TermName.Name, arguments, StoreMetadata(nonAcTerm));
            }
            else if (parseResult is Variable var)
            {
                return StoreVariable(var.Name);
            }
            else
            {
                throw new NotImplementedException();
            }            
        }

        private Dictionary<TermMetaData, Symbol> StoreMetadata(NonAcTerm nonAcTerm)
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
            metadata.Add(TermMetaData.ClassMemberMappings, StoreTerm(classMappings));

            return metadata;
        }

        /// <summary>
        /// Loads a new term and assigns an ID.
        /// </summary>
        /// <param name="term">Term to save</param>
        public void StoreTermAndAssignId(Term term)
        {
            var termId = _termDatabase.TermMapper.Map(term);
            term.Name.TermIdentifier = termId;
        }

        /// <summary>
        /// Make <paramref name="toTermId"/> term retrievable with labels for <paramref name="fromTermId"/> term.
        /// </summary>
        internal void CopyLabels(ulong fromTermId, ulong toTermId)
        {
            var sourceTerm = _termDatabase.TermMapper.ReverseMap(fromTermId);
            var destinationTerm = _termDatabase.TermMapper.ReverseMap(toTermId);
            foreach (var l in sourceTerm.Labels)
            {
                destinationTerm.Labels.Add(l);
                _termDatabase.LabelToTerm[l].Add(toTermId);
            }
        }
    }
}
