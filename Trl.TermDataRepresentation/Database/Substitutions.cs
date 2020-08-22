using System.Collections.Generic;

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
    }
}