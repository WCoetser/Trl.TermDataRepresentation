using System.Collections.Generic;
using System.IO;
using System.Linq;
using Trl.PegParser.Grammer.Semantics;

namespace Trl.TermDataRepresentation.Parser.AST
{
    public class NonAcTerm 
        : GenericPassthroughResult<ITrlParseResult, TokenNames>, ITrlParseResult, ITrlTerm
    {
        public Identifier TermName { get; set; }
        public ClassMemberMappingsList ClassMemberMappings { get; set; }
        public List<ITrlTerm> Arguments { get; set; }

        public void WriteToStream(StreamWriter outputStream)
        {
            TermName.WriteToStream(outputStream);
            ClassMemberMappings?.WriteToStream(outputStream);
            outputStream.Write("(");
            if (Arguments != null && Arguments.Any())
            {
                Arguments[0].WriteToStream(outputStream);
                foreach (var arg in Arguments.Skip(1))
                {
                    outputStream.Write(",");
                    arg.WriteToStream(outputStream);
                }
            }            
            outputStream.Write(")");
        }
    }
}
