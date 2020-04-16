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

    public class QsPlusPlusToken : QsSimpleToken { } // ++
    public class QsMinusMinusToken : QsSimpleToken { } // --
    public class QsLessThanEqualToken : QsSimpleToken { } // <=
    public class QsGreaterThanEqualToken : QsSimpleToken { } // >=    

    public class QsLessThanToken : QsSimpleToken { } // <
    public class QsGreaterThanToken : QsSimpleToken { } // >

    public class QsEqualToken : QsSimpleToken { } // =
    public class QsCommaToken : QsSimpleToken { } // ,
    public class QsSemiColonToken : QsSimpleToken { } // ;   
    public class QsLBraceToken : QsSimpleToken { } // {
    public class QsRBraceToken : QsSimpleToken { } // }
    public class QsLParenToken : QsSimpleToken { } // (
    public class QsRParenToken : QsSimpleToken { } // )

    public class QsIfToken : QsSimpleToken { }    // if 
    public class QsElseToken : QsSimpleToken { }  // else 
    public class QsForToken : QsSimpleToken { }  // for
    public class QsContinueToken : QsSimpleToken { } // continue
    public class QsBreakToken : QsSimpleToken { } // break

    public class QsWhitespaceToken : QsSimpleToken { } // \s
    public class QsNewLineToken: QsSimpleToken { }     // \r \n \r\n

    public class QsDoubleQuoteToken : QsSimpleToken { } // "
    public class QsDollarLBraceToken : QsSimpleToken { }
    public class QsEndOfFileToken : QsSimpleToken { }
    public class QsEndOfCommandToken : QsSimpleToken { }

    // digit
    public class QsIntToken : QsToken
    {
        public int Value { get; }
        public QsIntToken(int value) { Value = value; }

        public override bool Equals(object? obj)
        {
            return obj is QsIntToken token &&
                   Value == token.Value;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Value);
        }

        public static bool operator ==(QsIntToken? left, QsIntToken? right)
        {
            return EqualityComparer<QsIntToken>.Default.Equals(left, right);
        }

        public static bool operator !=(QsIntToken? left, QsIntToken? right)
        {
            return !(left == right);
        }
    }

    public class QsBoolToken : QsToken 
    { 
        public bool Value { get; }
        public QsBoolToken(bool value) { Value = value; }

        public override bool Equals(object? obj)
        {
            return obj is QsBoolToken token &&
                   Value == token.Value;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Value);
        }

        public static bool operator ==(QsBoolToken? left, QsBoolToken? right)
        {
            return EqualityComparer<QsBoolToken>.Default.Equals(left, right);
        }

        public static bool operator !=(QsBoolToken? left, QsBoolToken? right)
        {
            return !(left == right);
        }
    }

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
