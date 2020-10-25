using System;
using System.Collections.Generic;
using System.Linq;

namespace Trl.TermDataRepresentation.Database.Unification
{
    public class UnifierCalculation
    {
        private readonly TermDatabase _termDatabase;

        public UnifierCalculation(TermDatabase termDatabase)
        {
            _termDatabase = termDatabase;
        }

        /// <summary>
        /// Calculates a unifier for the terms in the input equation.
        /// </summary>
        /// <param name="unificationProblem">Gives the two terms that have to be unified.</param>
        /// <param name="storage">Database associated with the terms in the unification problem.</param>
        /// <returns>Substitutuions for variables and a boolean indicating whether unification succeeded.</returns>
        public (IEnumerable<Substitution> substitutions, bool succeed) GetSyntacticUnifier(Equation unificationProblem)
        {
            // See Franz Baader and Wayne Snyder (2001). "Unification Theory". In John Alan Robinson and Andrei Voronkov, 
            // editors, Handbook of Automated Reasoning, volume I, pages 447–533. Elsevier Science Publishers.

            if (unificationProblem.Lhs.Name.TermIdentifier.Value == unificationProblem.Rhs.Name.TermIdentifier.Value)
            {
                return (Enumerable.Empty<Substitution>(), true);
            }

            var substitutions = new List<Substitution>();

            var currentEquations = new Queue<Equation>();
            currentEquations.Enqueue(unificationProblem);
            while (currentEquations.Any())
            {
                var next = currentEquations.Dequeue();
                Term lhs = next.Lhs;
                Term rhs = next.Rhs;

                // NB: Do not change the sequence of operations in this loop

                // "Trivial" case - LHS equals RHS
                if (lhs.Name.TermIdentifier.Value == rhs.Name.TermIdentifier.Value)
                {
                    continue;
                }

                // Symbol clash - Fail case
                if (lhs.Name.Type != SymbolType.Variable
                    && rhs.Name.Type != SymbolType.Variable
                    && (
                        lhs.Name.Type != rhs.Name.Type
                        || !NameEquals(lhs, rhs)
                        || lhs.Arguments.Length != rhs.Arguments.Length
                        )
                    )
                {
                    return (Enumerable.Empty<Substitution>(), false);
                }

                // Orient - No need to enqueue it at this point, next rules will process it
                if (lhs.Name.Type != SymbolType.Variable
                    && rhs.Name.Type == SymbolType.Variable)
                {
                    var temp = lhs;
                    next.Lhs = next.Rhs;
                    next.Rhs = temp;
                }

                // Occurs check - The case where LHS = RHS and LHS is variable already covered under "Trivial" case
                if (lhs.Name.Type == SymbolType.Variable
                    && rhs.Variables.Contains(lhs.Name.TermIdentifier.Value))
                {
                    return (Enumerable.Empty<Substitution>(), false);
                }

                // Variable elimination
                if (lhs.Name.Type == SymbolType.Variable)
                {
                    if (!CreateAndUpdateSubstitutions(next, substitutions, currentEquations))
                    {
                        // Fail: same variable mapped to different values
                        return (Enumerable.Empty<Substitution>(), false);
                    }
                }

                // Decomposition
                if (lhs.Name.Type == SymbolType.NonAcTerm
                || lhs.Name.Type == SymbolType.TermList)
                {
                    for (int i = 0; i < lhs.Arguments.Length; i++)
                    {
                        var tLhs = _termDatabase.Reader.GetInternalTermById(lhs.Arguments[i].TermIdentifier.Value);
                        var tRhs = _termDatabase.Reader.GetInternalTermById(rhs.Arguments[i].TermIdentifier.Value);
                        currentEquations.Enqueue(new Equation { Lhs = tLhs, Rhs = tRhs });
                    }
                }
            }

            return (substitutions, true);
        }

        /// <summary>
        /// Creates new substitutions and applies them to the current substitutions and equations to eliminate existing variables.
        /// </summary>        
        /// <returns>True is succeed, false if a value clash occurs (ie. the same variable mapped to different values.)</returns>
        private bool CreateAndUpdateSubstitutions(Equation next, List<Substitution> substitutions, Queue<Equation> currentEquations)
        {
            // Check if same variable would be mapped to different values
            foreach (var substitution in substitutions)
            {
                // Test for a mapping clash
                if (next.Lhs.Name.TermIdentifier == substitution.MatchTermIdentifier // same name
                    && next.Rhs.Name.TermIdentifier.Value != substitution.SubstituteTermIdentifier) // different values
                {
                    return false;
                }
            }

            // Create the new substitution
            var newSubstitution = new Substitution(_termDatabase)
            {
                MatchTermIdentifier = next.Lhs.Name.TermIdentifier.Value,
                SubstituteTermIdentifier = next.Rhs.Name.TermIdentifier.Value
            };

            // Apply new substitution to current substitutions to eleminate the variable and in the process solve it
            foreach (var substitution in substitutions)
            {
                substitution.SubstituteTermIdentifier = ApplySubstitution(newSubstitution, substitution.SubstituteTermIdentifier);
            }
            substitutions.Add(newSubstitution);

            // Remove all cases where a variable now maps to itself ... 
            // this could potentially cause a "fail" to to mappings clashes later on
            substitutions.RemoveAll(s => s.MatchTermIdentifier == s.SubstituteTermIdentifier);

            // Apply new substitution to current equations to eleminate variable and in the process "solve" it
            foreach (var eq in currentEquations)
            {
                var lhs_s = ApplySubstitution(newSubstitution, eq.Lhs.Name.TermIdentifier.Value);
                var rhs_s = ApplySubstitution(newSubstitution, eq.Rhs.Name.TermIdentifier.Value);
                eq.Lhs = _termDatabase.Reader.GetInternalTermById(lhs_s);
                eq.Rhs = _termDatabase.Reader.GetInternalTermById(rhs_s);
            }

            return true;
        }

        private ulong ApplySubstitution(Substitution newSubstitution, ulong substituteTermIdentifier)
        {
            var substitutionFrame = new Frame(_termDatabase);
            substitutionFrame.RootTerms.Add(substituteTermIdentifier);
            substitutionFrame.Substitutions.Add(newSubstitution);
            substitutionFrame.Rewrite(int.MaxValue);
            return substitutionFrame.RootTerms.Single();
        }

        /// <summary>
        /// Tests if two terms have the same name, taking field mapping metadata into account.
        /// The class member mappings (for serialization) is counted as part of the name.
        /// </summary>
        private bool NameEquals(Term lhs, Term rhs)
        {
            if (lhs.Name.AssociatedStringValue != rhs.Name.AssociatedStringValue)
            {
                return false;
            }
            // Check that the metadata fields lists matches - this is considered part of the name.
            Symbol lhsMeta = null;
            Symbol rhsMeta = null;
            _ = lhs.MetaData?.TryGetValue(TermMetaData.ClassMemberMappings, out lhsMeta);
            _ = rhs.MetaData?.TryGetValue(TermMetaData.ClassMemberMappings, out rhsMeta);
            return (lhsMeta, rhsMeta) switch
            {
                (null, null) => true,
                (null, _) => false,
                (_, null) => false,
                (_, _) => lhsMeta.TermIdentifier.Value == rhsMeta.TermIdentifier.Value
            };  
        }
    }
}
