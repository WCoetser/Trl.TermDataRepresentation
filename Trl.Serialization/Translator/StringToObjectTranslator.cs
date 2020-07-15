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
            if (typeof(TObject).IsAssignableFrom(typeof(ICollection<>)))
            {
                // TODO: Implement collection deserialization
                throw new NotImplementedException();
            }
            else
            {
                if (statementList.Statements.Count != 1)
                {
                    throw new Exception("More than one result for given label, and output object type is not a collection type.");
                }

                var statement = statementList.Statements.Single();
                return statement.Term switch
                {
                    StringValue str => ConvertToStringOrNumericObject<TObject>(str.Value),
                    NumericValue num => ConvertToStringOrNumericObject<TObject>(num.Value),
                    _ => throw new NotImplementedException()
                };
            }
        }

        internal TObject ConvertToStringOrNumericObject<TObject>(string value)
            => (TObject)Convert.ChangeType(value, typeof(TObject));

    }
}
