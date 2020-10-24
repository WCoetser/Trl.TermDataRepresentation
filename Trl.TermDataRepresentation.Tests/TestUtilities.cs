using System;
using System.Collections.Generic;
using System.Linq;
using Trl.TermDataRepresentation.Database;
using Trl.TermDataRepresentation.Parser;

namespace Trl.TermDataRepresentation.Tests
{
    public static class TestUtilities
    {
        public static TermDatabase LoadStatements(string statements)
        {
            var termDatabase = new TermDatabase();
            var parser = new TrlParser();
            var parseResult = parser.ParseToAst(statements);
            if (!parseResult.Succeed)
            {
                throw new Exception(parseResult.Errors.First());
            }
            termDatabase.Writer.StoreStatements(parseResult.Statements);
            return termDatabase;
        }

        public static bool ContainsTheSameValues<T>(IEnumerable<T> lhs, IEnumerable<T> rhs)
        {
            if (lhs.Count() != rhs.Count())
            {
                return false;
            }
            foreach (var val in lhs)
            {
                if (!rhs.Contains(val))
                {
                    return false;
                }
            }
            return true;
        }
    }
}
