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

        public TermDatabase TermDatabase { get; }

        public Substitution(TermDatabase termDatabase)
        {
            TermDatabase = termDatabase;
        }

        public Substitution(Substitution cloneSource)
        {
            if (cloneSource != null)
            {
                TermDatabase = cloneSource.TermDatabase;
                MatchTermIdentifier = cloneSource.MatchTermIdentifier;
                SubstituteTermIdentifier = cloneSource.SubstituteTermIdentifier;
            }
        }
    }
}