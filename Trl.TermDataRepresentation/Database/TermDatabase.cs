using System;
using System.Collections.Generic;
using Trl.IntegerMapper;
using Trl.IntegerMapper.EqualityComparerIntegerMapper;
using Trl.IntegerMapper.StringIntegerMapper;
using Trl.TermDataRepresentation.Database.Mutations;

namespace Trl.TermDataRepresentation.Database
{
    /// <summary>
    /// Main storage for terms.
    /// </summary>    
    public class TermDatabase
    {
        /// <summary>
        /// Used to create human readable representation of term database content.
        /// </summary>
        internal IIntegerMapper<string> StringMapper { get; }

        /// <summary>
        /// Stores terms, mapping them to unique integers. The same term may not exist
        /// more than once.
        /// </summary>
        internal IIntegerMapper<Term> TermMapper { get; }

        /// <summary>
        /// Maps integers for string labels to integers for term identifiers.
        /// </summary>
        internal Dictionary<ulong, HashSet<Term>> LabelToTerm { get; }

        /// <summary>
        /// The current collection of root terms, substitutions, and term evaluators.
        /// </summary>
        internal Frame CurrentFrame { get; set; }

        private readonly Lazy<TermDatabaseWriter> _writer;

        private readonly Lazy<TermDatabaseReader> _reader;

        public TermDatabase()
        {
            StringMapper = new StringMapper();
            TermMapper = new EqualityComparerMapper<Term>(new IntegerMapperTermEqualityComparer());
            LabelToTerm = new Dictionary<ulong, HashSet<Term>>();
            CurrentFrame = new Frame(this);
            _writer = new Lazy<TermDatabaseWriter>(() =>  new TermDatabaseWriter(this));
            _reader = new Lazy<TermDatabaseReader>(() => new TermDatabaseReader(this));
        }

        /// <summary>
        /// Rewriteed collection of root terms.
        /// </summary>
        /// <param name="maxIterations">Maximum number of times to apply rewrite rules. This helps prefent non-terminatio
        /// in certain scenarios.</param>
        public void ExecuteRewriteRules(int maxIterations = 100000)
        {
            CurrentFrame.Rewrite(maxIterations);
        }

        /// <summary>
        /// Measure the database.
        /// </summary>
        public DatabaseMetrics GetDatabaseMetrics() 
        {
            return new DatabaseMetrics
            {
                RewriteRuleCount = Convert.ToInt32(CurrentFrame.Substitutions.Count),
                StringCount = StringMapper.MappedObjectsCount,
                TermCount = TermMapper.MappedObjectsCount,
                LabelCount = LabelToTerm.Count
            };
        }

        /// <summary>
        /// Gets a class that can be used to easily load data.
        /// </summary>
        public TermDatabaseWriter Writer 
        {
            get 
            { 
                return _writer.Value; 
            } 
        } 

        /// <summary>
        /// Gets a class that can be used to easily read data.
        /// </summary>
        public TermDatabaseReader Reader
        {
            get
            {
                return _reader.Value;
            }
        }

        /// <summary>
        /// Changes the current frame with root terms and substitutions
        /// based on the given mutation.
        /// </summary>
        /// <param name="termDatabaseMutation"></param>
        public void MutateDatabase(ITermDatabaseMutation termDatabaseMutation)
        {
            CurrentFrame = termDatabaseMutation.CreateMutatedFrame(CurrentFrame);
        }
    }
}
