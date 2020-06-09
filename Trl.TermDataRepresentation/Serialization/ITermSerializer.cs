namespace Trl.TermDataRepresentation.Serialization
{
    interface ITermSerializer<TObjectModel, TInput, TOutput>
    {
        public TObjectModel Deserialize(TInput input);

        public TOutput Serialize(TObjectModel input);
    }
}
