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

        internal Dictionary<(ulong, SymbolType), TermEvaluator> TermEvaluators { get; }

        internal Action<TermReplacement> TermReplacementObserver { get; set; }

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
            TermEvaluators = new Dictionary<(ulong, SymbolType), TermEvaluator>();
        }

        /// <summary>
        /// Applies rewrite rules to the terms collection.
        /// </summary>
        /// <param name="iterationLimit">The maximum number of times the rewrite rules should be applied. 
        /// Some term rewriting systems are non-terminating. In order to cater for this, a limit is imposed.</param>
        public void Rewrite(int iterationLimit = 1000)
        {
            // Keep track of new terms for next iteration of rewriting
            HashSet<Term> newTerms = new HashSet<Term>();
            // These terms could be "soft deleted" in the sense that they are no longer selectable by the serializer
            HashSet<Term> rewrittenTerms = new HashSet<Term>();
            int iterationStep = 0;

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
                        if (shouldNotUseUnification)
                        {
                            ProcessTermForSubstitutionWithoutUnification(newTerms, rewrittenTerms, substitution, currentRootTerm, iterationStep);
                        }
                        else
                        {
                            ProcessTermForSubstitutionWithUnification(newTerms, rewrittenTerms, substitution, currentRootTerm, iterationStep);
                        }

                        // Execute term evaluator
                        RunTermEvaluators(newTerms, rewrittenTerms, currentRootTerm, iterationStep);
                    }
                }

                if (TermEvaluators.Any())
                {
                    foreach (var currentRootTerm in RootTerms)
                    {
                        // Execute term evaluator
                        RunTermEvaluators(newTerms, rewrittenTerms, currentRootTerm, iterationStep);
                    }
                }

                // NB: First remove then add, in case there was a term that "came back" via rewrite rules
                RootTerms.ExceptWith(rewrittenTerms);
                RootTerms.UnionWith(newTerms);
                iterationStep++;
            }
            while (newTerms.Any() && iterationStep < iterationLimit);
        }

        /// <summary>
        /// Process a term for a substitution. In this case unification is not needed.
        /// </summary>
        /// <param name="newTerms">New terms generated during rewriting.</param>
        /// <param name="rewrittenTerms">Terms changed in the rewrite process.</param>
        /// <param name="substitution">The substitution applied.</param>
        /// <param name="currentRootTerm">The term being processed (ie. rewritten using the substitution.)</param>
        private void ProcessTermForSubstitutionWithoutUnification(HashSet<Term> newTerms, HashSet<Term> rewrittenTerms, Substitution substitution, Term currentRootTerm, int rewriteIteration)
        {
            var subsitutions = new Dictionary<Term, Term>
            {
                { substitution.MatchTerm, substitution.SubstituteTerm }
            };

            // Apply substitutions
            var newTerm = CopyAndReplaceForEquality(currentRootTerm, subsitutions);
            if (currentRootTerm != newTerm)
            {
                RecordSubstitutionResult(newTerms, rewrittenTerms, currentRootTerm, newTerm, substitution, rewriteIteration);
            }
        }

        private void RecordSubstitutionResult(HashSet<Term> newTerms, HashSet<Term> rewrittenTerms, Term currentRootTerm, Term newTerm, Substitution substitution, int rewriteIteration)
        {
            // In this case rewriting took place and the root terms must be updated
            newTerms.Add(newTerm);
            rewrittenTerms.Add(currentRootTerm);
            TermDatabase.Writer.CopyLabels(currentRootTerm, newTerm);

            // Call tracking function to notify of change
            TermReplacementObserver?.Invoke(new TermReplacement(currentRootTerm, newTerm, rewriteIteration, substitution));
        }

        /// <summary>
        /// Uses unification to solve variable in substitution head and applies solution to current root term.
        /// </summary>
        /// <param name="newTerms">New terms generated by processing substitution.</param>
        /// <param name="rewrittenTerms">Terms changed by substitution.</param>
        /// <param name="substitution">The substitution applied.</param>
        /// <param name="currentRootTerm">The current root term being processed.</param>
        private void ProcessTermForSubstitutionWithUnification(HashSet<Term> newTerms, HashSet<Term> rewrittenTerms, 
            Substitution substitution, Term currentRootTerm, int iterationCount)
        {
            UnifierCalculation unifierCalculation = new UnifierCalculation(TermDatabase);
            var substitutionHeadTerm = substitution.MatchTerm;

            // Unification needed
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
                    var replacementSub = new Dictionary<Term, Term> { { termGraphMember, tailSubstitutionValue } };
                    var newTerm = CopyAndReplaceForEquality(currentRootTerm, replacementSub);
                    if (currentRootTerm != newTerm)
                    {
                        // In this case rewriting took place and the root terms must be updated
                        RecordSubstitutionResult(newTerms, rewrittenTerms, currentRootTerm, newTerm, substitution, iterationCount);
                    }
                }
            }
        }

        /// <summary>
        /// Runs the term evaluator functions.
        /// </summary>
        /// <param name="newTerms">Collection of new terms generated in this process.</param>
        /// <param name="rewrittenTerms">Collection of terms rewritten in this process.</param>
        /// <param name="currentRootTerm">The root term that is being processed by term evaluators.</param>
        private void RunTermEvaluators(HashSet<Term> newTerms, HashSet<Term> rewrittenTerms, Term currentRootTerm, int rewriteIteration)
        {
            foreach (var termGraphMember in TermDatabase.Reader.GetAllTermsAndSubtermsForTermId(currentRootTerm))
            {
                if (TermEvaluators.TryGetValue((termGraphMember.Name.AssociatedStringValue, termGraphMember.Name.Type), out var termEvaluator)
                    && termEvaluator != null)
                {
                    var outputTerms = termEvaluator(termGraphMember, TermDatabase);
                    if (outputTerms != null &&  outputTerms.Any())
                    {
                        foreach (var outputTerm in outputTerms)
                        {
                            var sub = new Dictionary<Term, Term>
                                        {
                                            { termGraphMember,  outputTerm }
                                        };
                            var newTerm = CopyAndReplaceForEquality(currentRootTerm, sub);
                            if (currentRootTerm != newTerm)
                            {
                                // In this case rewriting took place and the root terms must be updated
                                RecordSubstitutionResult(newTerms, rewrittenTerms, currentRootTerm, newTerm, null, rewriteIteration);
                            }
                        }
                    }
                    else
                    {
                        // No output terms returned ... delete input term
                        rewrittenTerms.Add(currentRootTerm);
                        TermReplacementObserver?.Invoke(new TermReplacement(currentRootTerm, null, rewriteIteration, null));
                    }
                }
            }
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
