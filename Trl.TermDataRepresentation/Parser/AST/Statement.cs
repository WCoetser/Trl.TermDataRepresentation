using System.Collections.Generic;
using System.IO;

namespace Trl.TermDataRepresentation.Parser.AST
{
    public class Statement : ITrlParseResult
    {
        /// <summary>
        /// Labels used to externally identify term.
        /// </summary>
        public Label Label { get; set; }

        public ITrlTerm Term { get; set; }

        public void WriteToStream(StreamWriter outputStream)
        {
            if (Label != null)
            {
                Label.WriteToStream(outputStream);
                outputStream.Write(" : ");
            }

            Term.WriteToStream(outputStream);
        }
    }
}
