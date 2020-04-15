using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace QuickSC.Token
{
    public abstract class QsToken
    {
    }

    public class QsSimpleToken : QsToken
    {
        public override bool Equals(object? obj)
        {
            return obj != null && this.GetType() == obj.GetType();
        }

        public override int GetHashCode()
        {
            return this.GetType().GetHashCode();
        }

        // TODO: 제대로 동작하는지 확인 필요
        public static bool operator ==(QsSimpleToken? left, QsSimpleToken? right)
        {
            return EqualityComparer<QsSimpleToken>.Default.Equals(left, right);
        }

        public static bool operator !=(QsSimpleToken? left, QsSimpleToken? right)
        {
            return !(left == right);
        }
    }

    public class QsEqualToken : QsSimpleToken { } // =
    public class QsCommaToken : QsSimpleToken { } // ,
    public class QsSemiColonToken : QsSimpleToken { } // ;

    public class QsWhitespaceToken : QsSimpleToken { }
    public class QsBeginStringToken : QsSimpleToken { } // "
    public class QsEndStringToken : QsSimpleToken { }   
    public class QsBeginInnerExpToken : QsSimpleToken { }
    public class QsEndInnerExpToken : QsSimpleToken { }
    public class QsEndOfFileToken : QsSimpleToken { }
    public class QsEndOfCommandToken : QsSimpleToken { }

    public class QsTextToken : QsToken
    {
        public string Text { get; }
        public QsTextToken(string text) { Text = text; }

        public override bool Equals(object? obj)
        {
            return obj is QsTextToken token &&
                   Text == token.Text;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Text);
        }

        public static bool operator ==(QsTextToken? left, QsTextToken? right)
        {
            return EqualityComparer<QsTextToken>.Default.Equals(left, right);
        }

        public static bool operator !=(QsTextToken? left, QsTextToken? right)
        {
            return !(left == right);
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
}
