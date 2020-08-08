using System;
using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using Trl.TermDataRepresentation.Parser;
using Trl.TermDataRepresentation.Parser.AST;

namespace Trl.Serialization.Translator
{
    internal class ObjectToAstTranslator
    {
        internal ITrlParseResult BuildAst<TObject>(TObject inputObject, string rootLabel)
        {
            ITrlParseResult expression = BuildAstForObject(inputObject);

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

        private ITrlParseResult BuildAstForObject(object inputObject)
        {
            if (inputObject == null)
            {
                return new Identifier
                {
                    Name = "null"
                };
            }
            else if (inputObject is string)
            {
                return new StringValue
                {
                    Value = Convert.ToString(inputObject)
                };
            }
            // NB: IEnumerable must be after string because string is IEnumerable
            else if (inputObject is IEnumerable)
            {
                var list = new TermList()
                {
                    Terms = new List<ITrlTerm>()
                };
                var inputEnumerable = (IEnumerable)inputObject;
                foreach (var item in inputEnumerable)
                {
                    list.Terms.Add((ITrlTerm)BuildAstForObject(item));
                }
                return list;
            }
            else if (IsNumeric(inputObject))
            {
                return new NumericValue
                {
                    Value = Convert.ToString(inputObject)
                };
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        private static bool IsNumeric(object inputObject)
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
