namespace Trl.TermDataRepresentation.Database.Mutations
{
    /// <summary>
    /// Represents operations on term databases (see <see cref="TermDatabase"/>) that
    /// refactors the terms and rewrite rules represented by the current <see cref="Frame"/>
    /// on that database.
    /// </summary>
    public interface ITermDatabaseMutation
    {
        /// <summary>
        /// Changes the given frame terms and substitutions.
        /// </summary>
        /// <param name="inputFrame">Source frame for mutation.</param>
        /// <returns>Result of mutation.</returns>
        Frame CreateMutatedFrame(Frame inputFrame);
    }
}
