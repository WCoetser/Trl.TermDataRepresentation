using System.Collections.Generic;
using Trl.PegParser.Grammer;
using Trl.TermDataRepresentation.Parser.AST;

namespace Trl.TermDataRepresentation.Parser.SemanticValidations
{
    internal class SemanticValidator
    {
        internal List<string> GetSemanticErrors(ParseResult<TokenNames, ITrlParseResult> parseResult)
        {
            var errorList = new List<string>();

            if (!parseResult.Succeed)
            {
                errorList.Add(Errors.Syntax);
            }

            if (parseResult.SemanticActionResult == null)
            {
                return errorList;
            }

            UpdateWithClassMemberMappingErrors(parseResult, errorList);

            return errorList;
        }

        private void UpdateWithClassMemberMappingErrors(ParseResult<TokenNames, ITrlParseResult> parseResult, List<string> errorList)
        {
            var statements = (StatementList)parseResult.SemanticActionResult;
            foreach (var statement in statements.Statements)
            {
                CheckClassMappingMembers(statement.Term, errorList);
            }
        }

        private void CheckClassMappingMembers(ITrlTerm term, List<string> errorList)
        {
            var nonAcTerm = term as NonAcTerm;

            if (nonAcTerm == null)
            {
                return;
            }

            // Check arguments
            foreach (ITrlTerm arg in nonAcTerm.Arguments)
            {
                CheckClassMappingMembers(arg, errorList);
            }

            if (nonAcTerm.ClassMemberMappings == null)
            {
                return;
            }

            // Check that class member mappings count is the same as argument count
            var classMemberCount = nonAcTerm.ClassMemberMappings.ClassMembers.Count;
            if (classMemberCount != nonAcTerm.Arguments.Count)
            {
                errorList.Add(string.Format(Errors.NumberOfClassMembers, nonAcTerm.ToSourceCode()));
            }

            // Check that class member name mappings do not contain namespacing
            if (classMemberCount > 0)
            {
                foreach (var member in nonAcTerm.ClassMemberMappings.ClassMembers)
                {
                    if (member.Name.Contains("."))
                    {
                        errorList.Add(string.Format(Errors.NamespacedClassMembers, member.ToSourceCode()));
                    }
                }
            }
        }
    }
}
