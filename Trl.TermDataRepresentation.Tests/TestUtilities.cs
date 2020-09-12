using System;
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
    }
}
