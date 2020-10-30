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
            _termDatabase.CurrentFrame.Substitutions.Add(new Substitution(_termDatabase)
            {
                MatchTerm = StoreTerm(rule.MatchTerm),
                SubstituteTerm = StoreTerm(rule.SubstituteTerm)
            });
        }

        /// <summary>
        /// Assigns a label to a term for easy later retrieval.
        /// </summary>
        /// <param name="term">Term recieving the label.</param>
        /// <param name="labelToAssign">The label to assign.</param>
        public void LabelTerm(Term term, string labelToAssign)
        {
            ulong labelId = _termDatabase.StringMapper.Map(labelToAssign);
            if (!_termDatabase.LabelToTerm.TryGetValue(labelId, out HashSet<Term> referencedTerms))
            {
                referencedTerms = new HashSet<Term>();
                _termDatabase.LabelToTerm.Add(labelId, referencedTerms);
            }
            term.Labels.Add(labelId);
            referencedTerms.Add(term);
        }

        /// <summary>
        /// Saves a statement.
        /// </summary>
        public void StoreStatement(TermStatement statement)
        {
            var term = StoreTerm(statement.Term);
            if (statement.Label != null)
            {
                foreach (var identifier in statement.Label.Identifiers)
                {
                    LabelTerm(term, identifier.Name);
                }
            }
            SetAsRootTerm(term);
        }

        /// <summary>
        /// Adds the term to the current frame for the term database,
        /// making it a root term.
        /// </summary>
        public void SetAsRootTerm(Term term)
        {
            _termDatabase.CurrentFrame.RootTerms.Add(term);
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
        public Term StoreAtom(string value, SymbolType symbolType)
        {
            if (symbolType != SymbolType.Number 
                && symbolType != SymbolType.Identifier
                && symbolType != SymbolType.String)
            {
                throw new Exception($"Cannot store symbol of type {symbolType} using {nameof(StoreAtom)}");
            }

            ulong numName = _termDatabase.StringMapper.Map(value);
            return StoreTermAndAssignId(new Symbol(numName, symbolType), null, new HashSet<Term>());
        }

        public Term StoreVariable(string name)
        {
            ulong varName = _termDatabase.StringMapper.Map(name);
            var variables = new HashSet<Term>();
            var termOut = StoreTermAndAssignId(new Symbol(varName, SymbolType.Variable), null, variables);
            variables.Add(termOut);
            return termOut;
        }

        public Term StoreTermList(Term[] terms)
        {
            return StoreTermAndAssignId(new Symbol(MapConstants.NullOrEmpty, SymbolType.TermList), terms, GetVariables(terms));
        }

        private HashSet<Term> GetVariables(Term[] arguments)
        {
            var variables = new HashSet<Term>();
            foreach (var arg in arguments)
            {
                variables.UnionWith(arg.Variables);
            }
            return variables;
        }

        public Term StoreNonAcTerm(string termName, Term[] arguments, Dictionary<TermMetaData, Term> metadata)
        {
            ulong numTermName = _termDatabase.StringMapper.Map(termName);
            return StoreTermAndAssignId(new Symbol(numTermName, SymbolType.NonAcTerm), arguments, GetVariables(arguments), metadata);
        }

        /// <summary>
        /// Saves an AST term and returns a symbol uniquely identifying it.
        /// Does no add term to set of root terms for rewriting.
        /// </summary>
        public Term StoreTerm(ITrlTerm parseResult)
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

        private Dictionary<TermMetaData, Term> StoreMetadata(NonAcTerm nonAcTerm)
        {
            var metadata = new Dictionary<TermMetaData, Term>();

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
        private Term StoreTermAndAssignId(Symbol name, Term[] arguments, HashSet<Term> variables, Dictionary<TermMetaData, Term> metaData = null)
        {
            var term = new Term(name, arguments, variables, metaData);
            var termId = _termDatabase.TermMapper.Map(term);
            // If this is an existing term, it should return an equal term with the same id
            // Use the ID to get the existing object instance
            var returnTerm = _termDatabase.TermMapper.ReverseMap(termId);
            returnTerm.Name.TermIdentifier = termId;
            return returnTerm;
        }

        public Term CreateCopy(Term source, Term[] newArguments)
        {
            var newSymbol = new Symbol(source.Name.AssociatedStringValue, source.Name.Type);
            var newMetaData = source.MetaData != null ? new Dictionary<TermMetaData, Term>(source.MetaData) : null;
            var variables = GetVariables(newArguments);
            var termOut = StoreTermAndAssignId(newSymbol, newArguments, variables, newMetaData);
            if (source.Name.Type == SymbolType.Variable)
            {
                variables.Add(termOut);
            }
            return termOut;
        }

        /// <summary>
        /// Make <paramref name="toTerm"/> term retrievable with labels for <paramref name="fromTerm"/> term.
        /// </summary>
        internal void CopyLabels(Term fromTerm, Term toTerm)
        {
            foreach (var l in fromTerm.Labels)
            {
                toTerm.Labels.Add(l);
                _termDatabase.LabelToTerm[l].Add(toTerm);
            }
        }
    }
}
