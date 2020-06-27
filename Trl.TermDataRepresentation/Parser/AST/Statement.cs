using System.Collections.Generic;

namespace Trl.TermDataRepresentation.Parser.AST
{
    public class Statement : ITrlParseResult
    {
        /// <summary>
        /// Labels used to externally identify term.
        /// </summary>
        public Label Label { get; set; }

        public ITrlTerm Term { get; set; }
    }
}
