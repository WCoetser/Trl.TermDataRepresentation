using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
            return term switch
            {
                StringValue str => str.Value,
                NumericValue num => ConvertToNumeric(targetType, num.Value),
                Identifier id => ConvertIdentifier(targetType, id),
                TermList termList => ConvertToCollectionOrArray(targetType, termList),
                _ => throw new NotImplementedException()
            };
        }

        private object ConvertToCollectionOrArray(Type targetType, TermList termList)
        {
            bool isTargetObject = targetType.IsAssignableFrom(typeof(object));

            // Array case
            if (targetType.IsArray || isTargetObject)
            {
                var arrayElementType = isTargetObject ? typeof(object) : targetType.GetElementType();
                Array outputArray = Array.CreateInstance(arrayElementType, termList.Terms.Count);
                for (int i = 0; i < termList.Terms.Count; i++)
                {
                    outputArray.SetValue(ConvertToObject(arrayElementType, termList.Terms[i]), i);
                }
                return outputArray;
            }

            // ICollection case
            var constructorInfo = targetType.GetConstructor(Array.Empty<Type>());
            var collectionObject = constructorInfo.Invoke(Array.Empty<object>());
            var genericArg = targetType.GenericTypeArguments.Single();
            var addMethod = targetType.GetMethod("Add", BindingFlags.Public | BindingFlags.DeclaredOnly | BindingFlags.Instance);
            foreach (ITrlTerm subTerm in termList.Terms)
            {
                var collectionItem = ConvertToObject(genericArg, subTerm);
                addMethod.Invoke(collectionObject, new[] { collectionItem });
            }

            return collectionObject;
        }

        private object ConvertIdentifier(Type _, Identifier id)
        {
            return id.Name switch
            {
                "null" => null,
                _ => throw new NotImplementedException()
            };
        }

        internal object ConvertToNumeric(Type targetType, string value)
        {
            if (targetType.IsAssignableFrom(typeof(object)))
            {
                return Convert.ChangeType(value, typeof(decimal));
            }
            return Convert.ChangeType(value, targetType);
        }
    }
}
