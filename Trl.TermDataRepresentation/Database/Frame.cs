using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Trl.TermDataRepresentation.Parser;

namespace Trl.TermDataRepresentation.Database
{
    /// <summary>
    /// Represents current collection of "active" terms.
    /// The terms are stored in <see cref="Database.TermDatabase"/>
    /// Frames exist to facility rewriting.
    /// </summary>
    public class Frame
    {
        /// <summary>
        /// Root terms to be considered for rewriting.
        /// </summary>
        internal HashSet<ulong> RootTerms { get; }
      
        /// <summary>
        /// Represents the rewrite rules.
        /// </summary>
        internal HashSet<Substitution> Substitutions { get; }

        internal TermDatabase TermDatabase { get; }

        /// <summary>
        /// Creates a frame.
        /// </summary>
        /// <param name="currentTerms">Terms to be rewritten.</param>
        /// <param name="termDatabase">Storage for terms.</param>
        /// <param name="substitutions">Term substitutions.</param>
        public Frame(TermDatabase termDatabase)
        {
            RootTerms = new HashSet<ulong>();
            Substitutions = new HashSet<Substitution>(new SubstitutionEqualityComparer());
            TermDatabase = termDatabase;
        }

        /// <summary>
        /// Applies rewrite rules to the terms collection.
        /// </summary>
        /// <param name="iterationLimit">The maximum number of times the rewrite rules should be applied. 
        /// Some term rewriting systems are non-terminating. In order to cater for this, a limit is imposed.</param>
        public void Rewrite(int iterationLimit = 1000)
        {
            if (!Substitutions.Any())
            {
                return;
            }

            // Keep track of new terms for next iteration of rewriting
            HashSet<ulong> newTermIds = new HashSet<ulong>();
            // These terms could be "soft deleted" in the sense that they are no longer selectable by the serializer
            HashSet<ulong> rewrittenTerms = new HashSet<ulong>();
            int iterationCount = 0;

            do
            {
                // TODO: Optimize

                newTermIds.Clear();
                rewrittenTerms.Clear();
                foreach (var substitution in Substitutions)
                {
                    foreach (var termIdentifier in RootTerms)
                    {
                        var subsitutions = new Dictionary<ulong,ulong>
                        {
                            { substitution.MatchTermIdentifier, substitution.SubstituteTermIdentifier }
                        };
                        ulong newId = CopyAndReplaceForEquality(termIdentifier, subsitutions);
                        if (termIdentifier != newId)
                        {
                            // In this case rewriting took place and the root terms must be updated
                            newTermIds.Add(newId);
                            rewrittenTerms.Add(termIdentifier);
                            TermDatabase.Writer.CopyLabels(termIdentifier, newId);
                        }
                    }
                }

                // NB: First remove then add, in case there was a term that "came back" via rewrite rules
                RootTerms.ExceptWith(rewrittenTerms);
                RootTerms.UnionWith(newTermIds);
                iterationCount++;
            }
            while (newTermIds.Any() && iterationCount < iterationLimit);
        }

        /// <summary>
        /// Applies a substitution and returns the ID or the result term.
        /// </summary>
        /// <param name="termIdentifier">The ID of the term being recursively tested to see if it equals _matchTermIdentifier_.</param>
        /// <param name="matchAndReplaceIdentifiers">A collection of term substitutions in ID form, where the key is the matched term ID and the value is the replacement.</param>
        /// <returns>The ID of the new term, or if the reconstructed term is the same as the old term, the 
        /// ID of the old term.</returns>
        private ulong CopyAndReplaceForEquality(ulong termIdentifier, Dictionary<ulong, ulong> matchAndReplaceIdentifiers)
        {
            // Root
            if (matchAndReplaceIdentifiers.TryGetValue(termIdentifier, out var replacementTermId))
            {
                return replacementTermId;
            }

            var term = TermDatabase.Reader.GetInternalTermById(termIdentifier);

            // Arguments
            if (term.Arguments != null)
            {
                var newArguments = new Symbol[term.Arguments.Length];
                bool foundMatch = false;
                for (int i = 0; i < newArguments.Length; i++)
                {
                    var currentId = term.Arguments[i].TermIdentifier.Value;
                    var newId = CopyAndReplaceForEquality(currentId, matchAndReplaceIdentifiers);
                    if (newId == currentId)
                    {
                        // No match found
                        newArguments[i] = term.Arguments[i];
                    }
                    else
                    {
                        // Changes made, get new symbol for new argument
                        newArguments[i] = TermDatabase.Reader.GetInternalTermById(newId).Name;
                        foundMatch = true;
                    }
                }
                if (foundMatch)
                {
                    var newTerm = term.CreateCopy(newArguments);
                    TermDatabase.Writer.StoreTermAndAssignId(newTerm);
                    return newTerm.Name.TermIdentifier.Value;
                }
            }

            // Nothing matched
            return termIdentifier;
        }
    }
}
