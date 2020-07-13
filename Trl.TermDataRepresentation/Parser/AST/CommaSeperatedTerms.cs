using System;
using System.Collections.Generic;
using System.IO;

namespace Trl.TermDataRepresentation.Parser.AST
{
    /// <summary>
    /// Intermediate parse result class.
    /// </summary>
    internal class CommaSeperatedTerms : ITrlParseResult
    {
        public List<ITrlTerm> Terms { get; set; }

        public void WriteToStream(StreamWriter outputStream)
        {
            throw new Exception("Intermediate class, do not use for serialization.");
        }
    }
}
