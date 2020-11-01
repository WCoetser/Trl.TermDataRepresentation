using System;

namespace Trl.TermDataRepresentation.Database
{
    public class Substitution
    {
        /// <summary>
        /// Term to be matched.
        /// </summary>
        public Term MatchTerm { get; set; }

        /// <summary>
        /// Terms to be substituted.
        /// </summary>
        public Term SubstituteTerm { get; set; }

        public Substitution(Substitution cloneSource = null)
        {
            if (cloneSource != null)
            {
                MatchTerm = cloneSource.MatchTerm;
                SubstituteTerm = cloneSource.SubstituteTerm;
            }
        }

        public override bool Equals(object obj)
        {
            var other = obj as Substitution;
            return other != null && (other.MatchTerm, other.SubstituteTerm) == (MatchTerm , SubstituteTerm);
        }

        public override int GetHashCode() => HashCode.Combine(MatchTerm, SubstituteTerm);
    }
}