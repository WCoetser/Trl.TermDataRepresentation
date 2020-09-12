namespace Trl.TermDataRepresentation.Database
{
    public class Substitution
    {
        /// <summary>
        /// Term to be matched.
        /// </summary>
        public ulong MatchTermIdentifier { get; set; }

        /// <summary>
        /// Terms to be substituted.
        /// </summary>
        public ulong SubstituteTermIdentifier { get; set; }

        public Substitution(Substitution cloneSource = null)
        {
            if (cloneSource != null)
            {
                MatchTermIdentifier = cloneSource.MatchTermIdentifier;
                SubstituteTermIdentifier = cloneSource.SubstituteTermIdentifier;
            }
        }
    }
}