using System;
using System.Collections.Generic;
using System.Linq;
using Trl.TermDataRepresentation.Database.Unification;

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
        internal HashSet<Term> RootTerms { get; }
      
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
            RootTerms = new HashSet<Term>();
            Substitutions = new HashSet<Substitution>();
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
            HashSet<Term> newTerms = new HashSet<Term>();
            // These terms could be "soft deleted" in the sense that they are no longer selectable by the serializer
            HashSet<Term> rewrittenTerms = new HashSet<Term>();
            int iterationCount = 0;

            do
            {
                // TODO: Optimize

                newTerms.Clear();
                rewrittenTerms.Clear();
                foreach (var substitution in Substitutions)
                {
                    foreach (var currentRootTerm in RootTerms)
                    {
                        // Use unification to generate variable substitutions if needed
                        var substitutionHeadTerm = substitution.MatchTerm;
                        var shouldNotUseUnification = !substitutionHeadTerm.Variables.Any() || substitutionHeadTerm.Name.Type == SymbolType.Variable;
                        Dictionary<Term, Term> subsitutions = null;
                        if (shouldNotUseUnification)
                        {
                            // Case 1: Unification not needed
                            subsitutions = new Dictionary<Term, Term>
                            {
                                { substitution.MatchTerm, substitution.SubstituteTerm }
                            };

                            // Apply substitutions
                            var newTerm = CopyAndReplaceForEquality(currentRootTerm, subsitutions);
                            if (currentRootTerm != newTerm)
                            {
                                // In this case rewriting took place and the root terms must be updated
                                newTerms.Add(newTerm);
                                rewrittenTerms.Add(currentRootTerm);
                                TermDatabase.Writer.CopyLabels(currentRootTerm, newTerm);
                            }
                        }
                        else
                        {
                            UnifierCalculation unifierCalculation = new UnifierCalculation(TermDatabase);

                            // Case 2: Unification needed
                            // - Test every subtree of the current term against the substitution for unification
                            // - Substitute the substitution tail using the unifier
                            foreach (var termGraphMember in TermDatabase.Reader.GetAllTermsAndSubtermsForTermId(currentRootTerm))
                            {
                                var unificationResult = unifierCalculation.GetSyntacticUnifier(new Equation { Lhs = substitutionHeadTerm, Rhs = termGraphMember });
                                if (unificationResult.succeed)
                                {
                                    // Generate replacement term using unifier
                                    // - Substitutions can only be from a variable to a term
                                    var substitutions = unificationResult.substitutions.ToDictionary(s => s.MatchTerm, s => s.SubstituteTerm);
                                    var tailSubstitutionValue = CopyAndReplaceForEquality(substitution.SubstituteTerm, substitutions);
                                    // Replace subterm of current term with calculated replacement
                                    var replacementSub = new Dictionary<Term, Term> { {  termGraphMember, tailSubstitutionValue } };
                                    var newId = CopyAndReplaceForEquality(currentRootTerm, replacementSub);
                                    if (currentRootTerm != newId)
                                    {
                                        // In this case rewriting took place and the root terms must be updated
                                        newTerms.Add(newId);
                                        rewrittenTerms.Add(currentRootTerm);
                                        TermDatabase.Writer.CopyLabels(currentRootTerm, newId);
                                    }
                                }
                            }
                        }
                    }
                }

                // NB: First remove then add, in case there was a term that "came back" via rewrite rules
                RootTerms.ExceptWith(rewrittenTerms);
                RootTerms.UnionWith(newTerms);
                iterationCount++;
            }
            while (newTerms.Any() && iterationCount < iterationLimit);
        }

        /// <summary>
        /// Applies a substitution and returns the ID or the result term.
        /// </summary>
        /// <param name="term">The term being recursively tested to see if it equals _matchTermIdentifier_.</param>
        /// <param name="matchAndReplaceTerms">A collection of term substitutions, where the key is the term being substituted.
        /// This is a dictionary because it needs to cater for unification 
        /// scenarios were a collectrion of variable mappings is passed in. It should only contain one enrty for cases where unification 
        /// is not needed.</param>
        /// <returns>The ID of the new term, or if the reconstructed term is the same as the old term, the 
        /// ID of the old term.</returns>
        private Term CopyAndReplaceForEquality(Term term, Dictionary<Term, Term> matchAndReplaceTerms)
        {
            // Root
            if (matchAndReplaceTerms.TryGetValue(term, out var replacementTermId))
            {
                return replacementTermId;
            }

            // Arguments
            if (term.Arguments != null)
            {
                var newArguments = new Term[term.Arguments.Length];
                bool foundMatch = false;
                for (int i = 0; i < newArguments.Length; i++)
                {
                    var currentId = term.Arguments[i].Name.TermIdentifier.Value;
                    var newTerm = CopyAndReplaceForEquality(term.Arguments[i], matchAndReplaceTerms);
                    if (newTerm.Name.TermIdentifier.Value == currentId)
                    {
                        // No match found
                        newArguments[i] = term.Arguments[i];
                    }
                    else
                    {
                        // Changes made, get new symbol for new argument
                        newArguments[i] = newTerm;
                        foundMatch = true;
                    }
                }
                if (foundMatch)
                {
                    return TermDatabase.Writer.CreateCopy(term, newArguments);
                }
            }

            // Nothing matched
            return term;
        }       

    }
}
