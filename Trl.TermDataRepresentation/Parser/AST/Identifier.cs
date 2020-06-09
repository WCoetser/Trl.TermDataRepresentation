using System.Collections.Generic;

namespace Trl.TermDataRepresentation.Parser.AST
{
    public class Identifier : ITrlParseResult
    {
        /// <summary>
        /// This could be "." delimited for namespacing.
        /// </summary>
        public string Name { get; set; }
    }
}
