using System;
using System.Collections.Generic;

namespace Trl.TermDataRepresentation.Database.Mutations
{
    /// <summary>
    /// This class exists to reduce term duplication.
    /// 
    /// Converts terms and their term arguments to rewrite rules,
    /// assigning unique identifiers to each term to preserve their identities.
    /// Adds root symbols for the new rewrite rules.
    /// Only replaces terms with more than one occurance.
    /// </summary>
    public class ConvertCommonTermsToRewriteRules : ITermDatabaseMutation
    {
        private readonly Dictionary<string, ulong> _sourceStringCountCache;
        private readonly Dictionary<ulong, Symbol> _existingSubstitutions;

        public ConvertCommonTermsToRewriteRules()
        {
            _sourceStringCountCache = new Dictionary<string, ulong>();
            _existingSubstitutions = new Dictionary<ulong, Symbol>();
        }

        public Frame CreateMutatedFrame(Frame inputFrame)
        {
            var database = inputFrame.TermDatabase;
            Frame outputFrame = new Frame(database);
            _sourceStringCountCache.Clear();
            _existingSubstitutions.Clear();
            var termsToProcess = GetTermsToProcess(inputFrame);

            // Preserve existing substitutions in case they are useful
            foreach (var rewriteRule in inputFrame.Substitutions)
            {
                outputFrame.Substitutions.Add(new Substitution(rewriteRule));
            }

            foreach (var rootTermId in inputFrame.RootTerms)
            {
                var existingRootTerm = database.Reader.GetInternalTermById(rootTermId);
                Symbol newRootTerm = ConvertToSubstitutionsAndReturnIdentifier(existingRootTerm.Name, outputFrame, termsToProcess);
                if (newRootTerm.TermIdentifier != rootTermId)
                {
                    // In this case it is new
                    database.Writer.CopyLabels(rootTermId, newRootTerm.TermIdentifier.Value);
                }
                outputFrame.RootTerms.Add(newRootTerm.TermIdentifier.Value);
            }
            return outputFrame;
        }

        /// <summary>
        /// Identifies terms with enough duplication to process.
        /// </summary>
        private Dictionary<ulong, bool> GetTermsToProcess(Frame inputFrame)
        {
            var retVal = new Dictionary<ulong, bool>();
            foreach (var root in inputFrame.RootTerms)
            {
                UpdateGetTermsToProcess(root, inputFrame, retVal);
            }
            return retVal;
        }

        private void UpdateGetTermsToProcess(ulong root, Frame inputFrame, Dictionary<ulong, bool> retVal)
        {
            var term = inputFrame.TermDatabase.Reader.GetInternalTermById(root);
            if (term.Name.Type != SymbolType.NonAcTerm
                && term.Name.Type != SymbolType.TermList)
            {
                _ = retVal.TryAdd(root, false);
                return;
            }            
            
            if (retVal.TryGetValue(root, out var alreadyFound))
            {
                retVal[root] = true;
                // Note: subtree already processed.
                return;
            }
            retVal.Add(root, false);

            foreach (var arg in term.Arguments)
            {
                var argId = arg.TermIdentifier.Value;
                UpdateGetTermsToProcess(argId, inputFrame, retVal);
            }
        }

        private Symbol ConvertToSubstitutionsAndReturnIdentifier(Symbol existingTermSymbol, Frame outputFrame, Dictionary<ulong, bool> termsToProcess)
        {
            if (existingTermSymbol.Type != SymbolType.TermList
                && existingTermSymbol.Type != SymbolType.NonAcTerm)
            {
                return existingTermSymbol;
            }

            if (_existingSubstitutions.TryGetValue(existingTermSymbol.TermIdentifier.Value, out Symbol identifier))
            {
                return identifier;
            }

            var existingTerm = outputFrame.TermDatabase.TermMapper.ReverseMap(existingTermSymbol.TermIdentifier.Value);

            // Convert Arguments First
            var replacementArguments = new List<Symbol>();
            foreach (var arg in existingTerm.Arguments)
            {
                var replacementArg = ConvertToSubstitutionsAndReturnIdentifier(arg, outputFrame, termsToProcess);
                replacementArguments.Add(replacementArg);
            }

            if (termsToProcess[existingTermSymbol.TermIdentifier.Value]) {
                // In this case we replace the term with a rewrite rule because it occurs more than once
                // Generate new identifier name or re-use existing
                var replacementName = GenerateUniqueIdentifierName(existingTermSymbol, outputFrame.TermDatabase);
                var replacementIdentifierSymbol = outputFrame.TermDatabase.Writer.StoreAtom(replacementName, SymbolType.Identifier);
                var substitutionTail = existingTerm.CreateCopy(replacementArguments.ToArray());
                outputFrame.TermDatabase.Writer.StoreTermAndAssignId(substitutionTail);

                // Create rewrite rule using replacement term aguments and new identifier as head
                outputFrame.Substitutions.Add(new Substitution
                {
                    MatchTermIdentifier = replacementIdentifierSymbol.TermIdentifier.Value,
                    SubstituteTermIdentifier = substitutionTail.Name.TermIdentifier.Value
                });
                _existingSubstitutions.Add(existingTermSymbol.TermIdentifier.Value, replacementIdentifierSymbol);
                return replacementIdentifierSymbol;
            }
            else
            {
                // In this case only the term arguments are transformed because there is only one of this term
                var replacementTerm = existingTerm.CreateCopy(replacementArguments.ToArray());
                outputFrame.TermDatabase.Writer.StoreTermAndAssignId(replacementTerm);
                return replacementTerm.Name;
            }
        }

        /// <summary>
        /// Generate a new unique name based on the source string.
        /// This name can be used as a identifier name.
        /// </summary>
        internal string GenerateUniqueIdentifierName(Symbol existingTermSymbol, TermDatabase termDatabase)
        {
            string existingName = existingTermSymbol.Type switch
            {
                SymbolType.NonAcTerm => termDatabase.StringMapper.ReverseMap(existingTermSymbol.AssociatedStringValue),
                SymbolType.TermList => "list",
                _ => throw new Exception("Unexpected term type")
            };

            ulong count = 0;
            _ = _sourceStringCountCache.TryGetValue(existingName, out count);
            string testString = $"{existingName[0]}{count}";
            while (termDatabase.StringMapper.TryGetMappedValue(testString, out _))
            {
                count++;
                testString = $"{existingName[0]}{count}";
            }
            _sourceStringCountCache.Add(existingName, count);
            return testString;
        }
    }
}
