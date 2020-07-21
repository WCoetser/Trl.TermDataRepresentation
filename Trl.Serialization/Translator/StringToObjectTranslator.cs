using System;
using System.Collections.Generic;
using System.Linq;
using Trl.TermDataRepresentation.Database;
using Trl.TermDataRepresentation.Parser;
using Trl.TermDataRepresentation.Parser.AST;

namespace Trl.Serialization.Translator
{
    internal class StringToObjectTranslator
    {
        private readonly TrlParser _parser;

        internal StringToObjectTranslator()
        {
            _parser = new TrlParser();
        }

        internal TObject BuildObject<TObject>(string inputString, string rootLabel)
        {
            var database = new TermDatabase();
            var ast = _parser.ParseToAst(inputString);
            if (!ast.Succeed)
            {
                throw new Exception("Syntax error.");
            }
            database.SaveStatements(ast.Statements);
            var statementList = database.ReadStatementsForLabel(rootLabel);
            if (statementList == default || statementList.Statements.Count == 0)
            {
                return default;
            }
            if (statementList.Statements.Count != 1)
            {
                throw new Exception("More than one result for given label");
            }
            return (TObject)ConvertToObject(typeof(TObject), statementList.Statements.Single().Term);
        }

        private object ConvertToObject(Type targetType, ITrlTerm term)
        {            
            if (targetType.IsAssignableFrom(typeof(ICollection<>)))
            {
                // TODO: Implement collection deserialization
                throw new NotImplementedException();
            }
            else
            {                
                return term switch
                {
                    StringValue str => ConvertToStringOrNumericObject(targetType, str.Value),
                    NumericValue num => ConvertToStringOrNumericObject(targetType, num.Value),
                    Identifier id => ConvertIdentifier(targetType, id),
                    _ => throw new NotImplementedException()
                };
            }
        }

        private object ConvertIdentifier(Type _, Identifier id)
        {
            return id.Name switch
            {
                "null" => null,
                _ => throw new NotImplementedException()
            };
        }

        internal object ConvertToStringOrNumericObject(Type targetType, string value)
            => Convert.ChangeType(value, targetType);
    }
}
