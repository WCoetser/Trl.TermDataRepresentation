using System;
using System.Collections.Generic;
using System.Text;

namespace Trl.TermDataRepresentation.Database
{
    public class SubstitutionEqualityComparer : IEqualityComparer<Substitution>
    {
        public bool Equals(Substitution x, Substitution y)
            => (x.MatchTermIdentifier, x.SubstituteTermIdentifier) == (y.MatchTermIdentifier, y.SubstituteTermIdentifier);

        public int GetHashCode(Substitution obj)
            => HashCode.Combine(obj.MatchTermIdentifier, obj.SubstituteTermIdentifier);
    }
}
