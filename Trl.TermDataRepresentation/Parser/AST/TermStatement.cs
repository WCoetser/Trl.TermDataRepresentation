using System.IO;
using System.Linq;

namespace Trl.TermDataRepresentation.Parser.AST
{
    public class TermStatement : ITrlParseResult
    {
        /// <summary>
        /// Labels used to externally identify term.
        /// </summary>
        public Label Label { get; set; }

        public ITrlTerm Term { get; set; }

        public void WriteToStream(StreamWriter outputStream)
        {
            if (Label != null && Label.Identifiers.Any())
            {
                Label.WriteToStream(outputStream);
                outputStream.Write(": ");
            }

            Term.WriteToStream(outputStream);
            outputStream.Write(";");
        }
    }
}
