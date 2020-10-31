using System.Collections.Generic;

namespace Trl.TermDataRepresentation.Database
{
    /// <summary>
    /// Function that evaluates a term and generates output as a collection of terms.
    /// This allows terms that generates multiple output terms with plug-in code.
    /// If it returns an empty collection the source term will be deleted.
    /// This function functions like a rewrite/substitution rule, replacing the input
    /// term with the output terms.
    /// </summary>
    /// <param name="inputTerm">The term to evaluate.</param>
    /// <param name="termDatabase">The databse associated with this term.</param>
    /// <returns>An enumerable of replacement terms.</returns>
    public delegate IEnumerable<Term> TermEvaluator(Term inputTerm, TermDatabase termDatabase);
}
