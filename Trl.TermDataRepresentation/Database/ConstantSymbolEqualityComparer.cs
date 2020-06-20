using System;
using System.Collections.Generic;

namespace Trl.TermDataRepresentation.Database
{
    public class ConstantSymbolEqualityComparer : IEqualityComparer<ConstantSymbol>
    {
        public bool Equals(ConstantSymbol x, ConstantSymbol y)
            => x.Value == y.Value && x.Type == y.Type;

        public int GetHashCode(ConstantSymbol obj)
            => HashCode.Combine(obj.Value, obj.Type);
    }
}
