using System.Collections.Generic;
using System.IO;
using System.Linq;
using Trl.PegParser.Grammer.Semantics;

namespace Trl.TermDataRepresentation.Parser.AST
{
    public class ClassMemberMappingsList : GenericPassthroughResult<ITrlParseResult, TokenNames>, ITrlParseResult
    {
        public List<Identifier> ClassMembers { get; set; }

        public void WriteToStream(StreamWriter outputStream)
        {
            if (ClassMembers == null || !ClassMembers.Any())
            {
                outputStream.Write("<>");
                return;
            }
            outputStream.Write("<");
            ClassMembers.First().WriteToStream(outputStream);
            foreach (var classMember in ClassMembers.Skip(1))
            {
                outputStream.Write(",");
                classMember.WriteToStream(outputStream);
            }
            outputStream.Write(">");
        }
    }
}
