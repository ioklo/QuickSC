using System;
using System.Collections.Generic;

namespace QuickSC.Token
{
    // recursive token
    public abstract class QsStringTokenElement
    {

    }

    public class QsTextStringTokenElement : QsStringTokenElement
    {
        public string Value { get; }
        public QsTextStringTokenElement(string value) { Value = value; }

        public override bool Equals(object? obj)
        {
            return obj is QsTextStringTokenElement element &&
                   Value == element.Value;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Value);
        }        
    }

    public class QsTokenStringTokenElement : QsStringTokenElement
    {
        public QsToken Token { get; }
        public QsTokenStringTokenElement(QsToken token) { Token = token; }

        public override bool Equals(object? obj)
        {
            return obj is QsTokenStringTokenElement element &&
                   EqualityComparer<QsToken>.Default.Equals(Token, element.Token);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Token);
        }
    }
}

