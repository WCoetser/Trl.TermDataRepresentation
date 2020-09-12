using System;
using System.Collections.Generic;
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
                        ulong newId = CopyAndReplaceForEquality(termIdentifier, substitution.MatchTermIdentifier, substitution.SubstituteTermIdentifier);
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
        /// Note: this method only copies the parts of the term that is affercted by the definition of term
        /// equality in <see cref="IntegerMapperTermEqualityComparer"/>
        /// </summary>
        /// <returns>The ID of the new term, or if the reconstructed term is the same as the old term, the 
        /// ID of the old term.</returns>
        private ulong CopyAndReplaceForEquality(ulong termIdentifier, ulong matchTermIdentifier, ulong replacementTerm)
        {
            // Root
            if (termIdentifier == matchTermIdentifier)
            {
                return replacementTerm;
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
                    var newId = CopyAndReplaceForEquality(currentId, matchTermIdentifier, replacementTerm);
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
