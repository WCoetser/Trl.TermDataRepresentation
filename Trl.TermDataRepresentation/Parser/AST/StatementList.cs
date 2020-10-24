using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Trl.TermDataRepresentation.Parser.AST
{
    public class StatementList : ITrlParseResult
    {
        public List<TermStatement> Statements { get; set; }

        public List<RewriteRule> RewriteRules { get; set; }

        public void WriteToStream(StreamWriter outputStream, bool prettyPrint = false)
        {
            if (Statements != null)
            {
                foreach (var statement in Statements)
                {
                    statement.WriteToStream(outputStream);
                    if (prettyPrint)
                    {
                        outputStream.WriteLine();
                    }
                }
            }

            if (RewriteRules != null)
            {
                foreach (var rule in RewriteRules)
                {
                    rule.WriteToStream(outputStream);
                    if (prettyPrint)
                    {
                        outputStream.WriteLine();
                    }
                }
            }
        }

        public void WriteToStream(StreamWriter outputStream)
        {
            WriteToStream(outputStream, prettyPrint: false);
        }
    }
}
