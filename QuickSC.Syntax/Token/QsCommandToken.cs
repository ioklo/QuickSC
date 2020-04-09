using System;
using System.Collections.Generic;

namespace QuickSC.Token
{
    public abstract class QsCommandToken
    {
    }

    public class QsStringCommandToken : QsCommandToken
    {
        public QsStringToken Token { get; }
        public QsStringCommandToken(QsStringToken token) { Token = token; }

        public override bool Equals(object? obj)
        {
            return obj is QsStringCommandToken token &&
                   EqualityComparer<QsStringToken>.Default.Equals(Token, token.Token);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Token);
        }
    }

    public class QsIdentifierCommandToken : QsCommandToken
    {
        public QsIdentifierToken Token { get; }
        public QsIdentifierCommandToken(QsIdentifierToken token) { Token = token; }

        public override bool Equals(object? obj)
        {
            return obj is QsIdentifierCommandToken token &&
                   EqualityComparer<QsIdentifierToken>.Default.Equals(Token, token.Token);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Token);
        }
    }
}