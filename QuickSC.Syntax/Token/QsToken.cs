using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace QuickSC.Token
{
    public abstract class QsToken
    {

    }

    public class QsStringToken : QsToken
    {
        public List<QsStringTokenElement> Elements { get; }
        public QsStringToken(List<QsStringTokenElement> elements) { Elements = elements; }

        public override bool Equals(object? obj)
        {
            return obj is QsStringToken token &&
                Enumerable.SequenceEqual(Elements, token.Elements);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Elements);
        }
    }

    public class QsIdentifierToken : QsToken
    {
        public string Value { get; }
        public QsIdentifierToken(string value) { Value = value; }

        public override bool Equals(object? obj)
        {
            return obj is QsIdentifierToken token &&
                   Value == token.Value;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Value);
        }
    }

    public class QsEndOfFileToken : QsToken
    {

    }
}
