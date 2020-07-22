using System;
using System.Collections.Generic;

namespace Trl.TermDataRepresentation.Database
{
    public class IntegerMapperTermEqualityComparer : IEqualityComparer<Term>
    {
        public bool Equals(Term x, Term y)
        {
            // Shortcut: If term identifiers are available, use these instead
            if (x.Name.TermIdentifier.HasValue && y.Name.TermIdentifier.HasValue)
            {
                return x.Name.TermIdentifier == y.Name.TermIdentifier;
            }

            // Scenario 2: At least one of the terms have not been mapped
            var stringNamesMatch = x.Name.AssociatedStringValue == y.Name.AssociatedStringValue
                                && x.Name.Type == y.Name.Type;
            if (!stringNamesMatch)
            {
                return false;
            }
            var argumentLengthsMatch = 
                x.Arguments switch
                {
                    null => y.Arguments == null,
                    _ => y.Arguments != null && y.Arguments.Length == x.Arguments.Length                     
                };
            if (!argumentLengthsMatch)
            {
                return false;
            }
            bool argsMatch = true;
            if (x.Arguments != null)
            {
                for (int i = 0; i < x.Arguments.Length && argsMatch; i++)
                {
                    // Term arguments must have identifiers.
                    argsMatch = x.Arguments[i].TermIdentifier.Value == y.Arguments[i].TermIdentifier.Value;
                }
            }
            return argsMatch;
        }

        public int GetHashCode(Term x)
        {
            // Term identifier may not be available for root term
            var hash = new HashCode();
            hash.Add(x.Name.AssociatedStringValue);
            if (x.Arguments != null)
            {
                // Arguments must always have term identifiers
                for (int i = 0; i < x.Arguments.Length; i++)
                {
                    hash.Add(x.Arguments[i].TermIdentifier.Value);
                }
            }
            return hash.ToHashCode();
        }
    }
}
