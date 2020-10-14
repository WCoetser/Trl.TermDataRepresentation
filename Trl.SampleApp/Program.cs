using System;
using Trl.TermDataRepresentation.Database;
using Trl.TermDataRepresentation.Parser;

namespace Trl.SampleApp
{
    class Program
    {
        static void Main(string[] args)
        {
            var input = @"
0;
0 => inc(0);
";

            var parser = new TrlParser();
            var parseResult = parser.ParseToAst(input);
            if (!parseResult.Succeed)
            {
                Console.WriteLine("Syntax error.");
                return;
            }

            var termDatabase = new TermDatabase();
            termDatabase.Writer.StoreStatements(parseResult.Statements);
            termDatabase.ExecuteRewriteRules(4);

            Console.WriteLine("Output:");
            var output = termDatabase.Reader.ReadCurrentFrame();
            Console.WriteLine(output.ToSourceCode(true));
        }
    }
}
