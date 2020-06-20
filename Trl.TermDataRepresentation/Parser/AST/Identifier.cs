using System.Collections.Generic;
using System.IO;

namespace Trl.TermDataRepresentation.Parser.AST
{
    public class Identifier : ITrlParseResult, ITrlTerm
    {
        /// <summary>
        /// This could be "." delimited for namespacing.
        /// </summary>
        public string Name { get; set; }

        public void WriteToStream(StreamWriter outputStream)
        {
            outputStream.Write(Name);
        }
    }
}
