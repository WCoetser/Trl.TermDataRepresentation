using System;
using System.Collections.Generic;
using System.Numerics;
using Trl.TermDataRepresentation.Parser;
using Trl.TermDataRepresentation.Parser.AST;

namespace Trl.Serialization.Translator
{
    class ObjectToAstTranslator
    {
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


        private ITrlParseResult ConvertStringToAst(string inputObject)
            => new StringValue
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
    }
}
