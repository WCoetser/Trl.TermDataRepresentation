using System;
using System.Collections.Generic;
using System.Linq;

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
        private readonly Dictionary<Term, Term> _existingSubstitutions;

        public ConvertCommonTermsToRewriteRules()
        {
            _sourceStringCountCache = new Dictionary<string, ulong>();
            _existingSubstitutions = new Dictionary<Term, Term>();
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

            foreach (var rootTerm in inputFrame.RootTerms)
            {
                var newRootTerm = ConvertToSubstitutionsAndReturnIdentifier(rootTerm, outputFrame, termsToProcess);
                if (newRootTerm != rootTerm)
                {
                    // In this case it is new
                    database.Writer.CopyLabels(rootTerm, newRootTerm);
                }
                outputFrame.RootTerms.Add(newRootTerm);
            }
            return outputFrame;
        }

        /// <summary>
        /// Identifies terms with enough duplication to process.
        /// </summary>
        private Dictionary<Term, bool> GetTermsToProcess(Frame inputFrame)
        {
            var retVal = new Dictionary<Term, bool>();
            foreach (var root in inputFrame.RootTerms)
            {
                UpdateGetTermsToProcess(root, inputFrame, retVal);
            }
            return retVal;
        }

        private void UpdateGetTermsToProcess(Term root, Frame inputFrame, Dictionary<Term, bool> retVal)
        {
            if (root.Name.Type != SymbolType.NonAcTerm
                    && root.Name.Type != SymbolType.TermList)
            {
                _ = retVal.TryAdd(root, false);
                return;
            }            
            
            if (retVal.ContainsKey(root)
                && !root.Variables.Any())
            {
                retVal[root] = true;
                // Note: subtree already processed.
                return;
            }
            _ = retVal.TryAdd(root, false);

            foreach (var arg in root.Arguments)
            {
                UpdateGetTermsToProcess(arg, inputFrame, retVal);
            }
        }

        private Term ConvertToSubstitutionsAndReturnIdentifier(Term existingTerm, Frame outputFrame, Dictionary<Term, bool> termsToProcess)
        {
            if (existingTerm.Name.Type != SymbolType.TermList
                && existingTerm.Name.Type != SymbolType.NonAcTerm)
            {
                return existingTerm;
            }

            if (_existingSubstitutions.TryGetValue(existingTerm, out Term identifier))
            {
                return identifier;
            }

            // Convert Arguments First
            var replacementArguments = new List<Term>();
            foreach (var arg in existingTerm.Arguments)
            {
                var replacementArg = ConvertToSubstitutionsAndReturnIdentifier(arg, outputFrame, termsToProcess);
                replacementArguments.Add(replacementArg);
            }

            if (termsToProcess[existingTerm]) {
                // In this case we replace the term with a rewrite rule because it occurs more than once
                // Generate new identifier name or re-use existing
                var replacementName = GenerateUniqueIdentifierName(existingTerm, outputFrame.TermDatabase);
                var replacementIdentifier = outputFrame.TermDatabase.Writer.StoreAtom(replacementName, SymbolType.Identifier);
                var substitutionTail = outputFrame.TermDatabase.Writer.CreateCopy(existingTerm, replacementArguments.ToArray());

                // Create rewrite rule using replacement term aguments and new identifier as head
                outputFrame.Substitutions.Add(new Substitution
                {
                    MatchTerm = replacementIdentifier,
                    SubstituteTerm = substitutionTail
                });
                _existingSubstitutions.Add(existingTerm, replacementIdentifier);
                return replacementIdentifier;
            }
            else
            {
                // In this case only the term arguments are transformed because there is only one of this term
                return  outputFrame.TermDatabase.Writer.CreateCopy(existingTerm, replacementArguments.ToArray());
            }
        }

        /// <summary>
        /// Generate a new unique name based on the source string.
        /// This name can be used as a identifier name.
        /// </summary>
        internal string GenerateUniqueIdentifierName(Term existingTerm, TermDatabase termDatabase)
        {
            string existingName = existingTerm.Name.Type switch
            {
                SymbolType.NonAcTerm => termDatabase.StringMapper.ReverseMap(existingTerm.Name.AssociatedStringValue),
                SymbolType.TermList => "list",
                _ => throw new Exception("Unexpected term type")
            };

            _ = _sourceStringCountCache.TryGetValue(existingName, out ulong count);
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
