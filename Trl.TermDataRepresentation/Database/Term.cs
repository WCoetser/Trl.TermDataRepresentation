using System.Collections.Generic;

namespace Trl.TermDataRepresentation.Database
{
    public class Term
    {
        /// <summary>
        /// The integer mapped name of the term.
        /// </summary>
        public ulong Termname { get; }
        
        /// <summary>
        /// The integer mapped arguments of the term.
        /// </summary>
        public IReadOnlyList<ulong> Arguments { get; }

        public Term(ulong termname, IReadOnlyList<ulong> arguments)
            => (Termname, Arguments) = (termname, arguments);

    }
}
