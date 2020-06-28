using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Trl.TermDataRepresentation.Parser.AST
{
    public class StatementList : ITrlParseResult
    {
        public List<Statement> Statements { get; set; }

        public void WriteToStream(StreamWriter outputStream)
        {
            if (Statements != null && Statements.Any())
            {
                Statements.First().WriteToStream(outputStream);
                outputStream.WriteLine(";");
                foreach (var statement in Statements.Skip(1))
                {
                    statement.WriteToStream(outputStream);
                    outputStream.WriteLine(";");
                }
            }
        }
    }
}
