namespace Trl.TermDataRepresentation.Database
{
    public class ConstantSymbol
    {
        public ConstantSymbol(ulong value, ConstantSymbolType symbolType)
            => (Value, Type) = (value, symbolType);

        public ulong Value { get; }
        public ConstantSymbolType Type { get; }
    }
}
