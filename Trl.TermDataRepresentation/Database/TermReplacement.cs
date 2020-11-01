namespace Trl.TermDataRepresentation.Database
{
    public class TermReplacement
    {
        /// <summary>
        /// The iteration of the rewrite operation
        /// </summary>
        public int RewriteIteration { get; }

        /// <summary>
        /// The root term that has been rewritten.
        /// </summary>
        public Term OriginalRootTerm { get; }

        /// <summary>
        /// The newly generated root term.
        /// </summary>
        public Term NewRootTerm { get; }

        /// <summary>
        /// The substitution used to do the rewrite step, or null if it was a native function.
        /// </summary>
        public Substitution AppliedSubstitution { get; }

        public TermReplacement(Term original, Term replacement, int iteration, Substitution appliedSubstitution)
        {
            OriginalRootTerm = original;
            NewRootTerm = replacement;
            RewriteIteration = iteration;
            AppliedSubstitution = appliedSubstitution;
        }

        /// <summary>
        /// Indicates the method by which the source term was changed.
        /// </summary>
        public TermReplacementType ReplacementType
        {
            get
            {
                return AppliedSubstitution switch
                {
                    null => TermReplacementType.TermEvaluator,
                    _ => TermReplacementType.RewriteRule
                };
            }
        }

        /// <summary>
        /// Indicates whether this operation deleted the original source term.
        /// </summary>
        public bool IsDelete => NewRootTerm == null;
    }
}
