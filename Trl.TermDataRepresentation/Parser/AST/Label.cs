using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Trl.TermDataRepresentation.Parser.AST
{
    public class Label : ITrlParseResult
    {
        public List<Identifier> Identifiers { get; set; }

        public IReadOnlyList<ITrlParseResult> SubResults { get => throw new System.Exception(); }

        public void WriteToStream(StreamWriter outputStream)
        {
            // There should always be at least one identifier
            Identifiers.First().WriteToStream(outputStream);

            foreach (var id in Identifiers.Skip(1))
            {
                outputStream.Write(",");
                id.WriteToStream(outputStream);
            }
        }
    }
}
