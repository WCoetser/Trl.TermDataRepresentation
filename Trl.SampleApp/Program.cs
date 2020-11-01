using System;
using Trl.TermDataRepresentation.Database;
using Trl.TermDataRepresentation.Parser;
using Trl.TermDataRepresentation;

namespace Trl.SampleApp
{
    class Program
    {
        static void Main(string[] args)
        {
            var input = "0; 0 => inc(0);";

            // Parse input
            var parser = new TrlParser();
            var parseResult = parser.ParseToAst(input);
            if (!parseResult.Succeed)
            {
                Console.WriteLine("Syntax error.");
                return;
            }

            // Execute substitutions
            var termDatabase = new TermDatabase();
            termDatabase.Writer.StoreStatements(parseResult.Statements);

            // Track changes
            termDatabase.Writer.SetTermReplacementObserver(replacement =>
            {
                var originalTerm = replacement.OriginalRootTerm.ToSourceCode(termDatabase);
                var newTerm = replacement.NewRootTerm.ToSourceCode(termDatabase);
                Console.WriteLine($"{replacement.RewriteIteration}> Replaced {originalTerm} with {newTerm}");
            });

            // Execute the rewrite rules
            termDatabase.ExecuteRewriteRules(4);

            // Print output
            Console.WriteLine();
            Console.WriteLine("Output:");
            var output = termDatabase.Reader.ReadCurrentFrame();
            Console.WriteLine(output.ToSourceCode(true));
        }
    }
}
