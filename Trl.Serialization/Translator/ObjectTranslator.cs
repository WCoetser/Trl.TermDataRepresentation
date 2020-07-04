using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Trl.TermDataRepresentation.Database;
using Trl.TermDataRepresentation.Parser;
using Trl.TermDataRepresentation.Parser.AST;

namespace Trl.Serialization.Translator
{
    /// <summary>
    /// Translates .NET objects into TRL parse results and back for serialization and deserialization.
    /// </summary>
    internal class ObjectTranslator
    {
        private readonly TrlParser _parser;

        internal ObjectTranslator()
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

        internal ITrlParseResult BuildAst<TObject>(TObject inputObject, string rootLabel)
        {
            ITrlParseResult expression = (inputObject, IsNumeric(inputObject)) switch
            {
                (string inputString, _) => ConvertStringToAst(inputString),
                (_, true) => ConvertNumberToAst(inputObject),
                _ => throw new NotImplementedException()
            };

            return new Statement
            {
                Term = (ITrlTerm)expression,
                Label = new Label
                {
                    Identifiers = new List<Identifier>
                            {
                                new Identifier
                                {
                                    Name = rootLabel
                                }
                            }
                }
            };
        }

        private ITrlParseResult ConvertNumberToAst<TObject>(TObject inputObject)
            => new NumericValue
            {
                Value = Convert.ToString(inputObject)
            };

        private static bool IsNumeric<TObject>(TObject inputObject) 
            => inputObject is sbyte
            || inputObject is byte
            || inputObject is short
            || inputObject is ushort
            || inputObject is int
            || inputObject is uint
            || inputObject is long
            || inputObject is ulong
            || inputObject is BigInteger
            || inputObject is decimal
            || inputObject is float
            || inputObject is double;

        private ITrlParseResult ConvertStringToAst(string inputObject)
            => new StringValue
            {
                Value = Convert.ToString(inputObject)
            };
            
    }
}
