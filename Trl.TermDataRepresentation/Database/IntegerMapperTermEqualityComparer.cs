using System;
using System.Collections.Generic;

namespace Trl.TermDataRepresentation.Database
{
    public class IntegerMapperTermEqualityComparer : IEqualityComparer<Term>
    {
        public bool Equals(Term x, Term y)
        {
            // Scenario 1: If term identifiers are available, use these instead
            // ---

            if (x.Name.TermIdentifier.HasValue && y.Name.TermIdentifier.HasValue)
            {
                return x.Name.TermIdentifier == y.Name.TermIdentifier;
            }

            // Scenario 2: At least one of the terms have not been mapped
            // ---

            // Check names
            var stringNamesMatch = x.Name.AssociatedStringValue == y.Name.AssociatedStringValue
                                && x.Name.Type == y.Name.Type;
            if (!stringNamesMatch)
            {
                return false;
            }

            // Check arguments
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
            if (!argsMatch)
            {
                return false;
            }

            // Check metadata
            var memberMappingsLengthMatch =
                (x.MetaData, y.MetaData) switch
                {
                    (null, null) => true,
                    (null, _) => false,
                    (_, null) => false,
                    (_, _) => x.MetaData.Count == y.MetaData.Count
                };
            if (!memberMappingsLengthMatch)
            {
                return false;
            }
            if (x.MetaData != null)
            {
                foreach (var pair in x.MetaData)
                {
                    TermMetaData name = pair.Key;
                    if (!y.MetaData.TryGetValue(name, out Symbol yValue)
                        || pair.Value.TermIdentifier.Value != yValue.TermIdentifier.Value)
                    {
                        return false;
                    }
                }
            }

            return true;
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
            if (x.MetaData != null)
            {
                int v = 0;
                foreach (var pair in x.MetaData)
                {
                    HashCode nestedCode = new HashCode();
                    nestedCode.Add(pair.Key);
                    nestedCode.Add(pair.Value.TermIdentifier.Value);
                    v ^= nestedCode.ToHashCode();
                }
                hash.Add(v);
            }
            return hash.ToHashCode();
        }
    }
}
