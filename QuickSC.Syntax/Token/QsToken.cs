using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace QuickSC.Token
{
    public abstract class QsToken
    {
    }    

    public class QsEqualEqualToken : QsToken { public static QsEqualEqualToken Instance { get; } = new QsEqualEqualToken(); private QsEqualEqualToken() { } } // ==
    public class QsExclEqualToken : QsToken { public static QsExclEqualToken Instance { get; } = new QsExclEqualToken(); private QsExclEqualToken() { } } // !=

    public class QsPlusPlusToken : QsToken { public static QsPlusPlusToken Instance { get; } = new QsPlusPlusToken(); private QsPlusPlusToken() { } } // ++
    public class QsMinusMinusToken : QsToken { public static QsMinusMinusToken Instance { get; } = new QsMinusMinusToken(); private QsMinusMinusToken() { } } // --
    public class QsLessThanEqualToken : QsToken { public static QsLessThanEqualToken Instance { get; } = new QsLessThanEqualToken(); private QsLessThanEqualToken() { } } // <=
    public class QsGreaterThanEqualToken : QsToken { public static QsGreaterThanEqualToken Instance { get; } = new QsGreaterThanEqualToken(); private QsGreaterThanEqualToken() { } } // >=    
    public class QsEqualGreaterThanToken : QsToken { public static QsEqualGreaterThanToken Instance { get; } = new QsEqualGreaterThanToken(); private QsEqualGreaterThanToken() { } } // =>

    public class QsLessThanToken : QsToken { public static QsLessThanToken Instance { get; } = new QsLessThanToken(); private QsLessThanToken() { } } // <
    public class QsGreaterThanToken : QsToken { public static QsGreaterThanToken Instance { get; } = new QsGreaterThanToken(); private QsGreaterThanToken() { } } // >

    public class QsEqualToken : QsToken { public static QsEqualToken Instance { get; } = new QsEqualToken(); private QsEqualToken() { } } // =
    public class QsCommaToken : QsToken { public static QsCommaToken Instance { get; } = new QsCommaToken(); private QsCommaToken() { } } // ,
    public class QsSemiColonToken : QsToken { public static QsSemiColonToken Instance { get; } = new QsSemiColonToken(); private QsSemiColonToken() { } } // ;   
    public class QsLBraceToken : QsToken { public static QsLBraceToken Instance { get; } = new QsLBraceToken(); private QsLBraceToken() { } } // {
    public class QsRBraceToken : QsToken { public static QsRBraceToken Instance { get; } = new QsRBraceToken(); private QsRBraceToken() { } } // }
    public class QsLParenToken : QsToken { public static QsLParenToken Instance { get; } = new QsLParenToken(); private QsLParenToken() { } } // (
    public class QsRParenToken : QsToken { public static QsRParenToken Instance { get; } = new QsRParenToken(); private QsRParenToken() { } } // )
    public class QsLBracketToken : QsToken { public static QsLBracketToken Instance { get; } = new QsLBracketToken(); private QsLBracketToken() { } } // [
    public class QsRBracketToken : QsToken { public static QsRBracketToken Instance { get; } = new QsRBracketToken(); private QsRBracketToken() { } } // ]
    
    public class QsPlusToken : QsToken { public static QsPlusToken Instance { get; } = new QsPlusToken(); private QsPlusToken() { } } // +
    public class QsMinusToken : QsToken { public static QsMinusToken Instance { get; } = new QsMinusToken(); private QsMinusToken() { } } // -
    public class QsStarToken : QsToken { public static QsStarToken Instance { get; } = new QsStarToken(); private QsStarToken() { } } // *   
    public class QsSlashToken : QsToken { public static QsSlashToken Instance { get; } = new QsSlashToken(); private QsSlashToken() { } } // /    
    public class QsPercentToken : QsToken { public static QsPercentToken Instance { get; } = new QsPercentToken(); private QsPercentToken() { } } // %    
    public class QsExclToken : QsToken { public static QsExclToken Instance { get; } = new QsExclToken(); private QsExclToken() { } } // !    
    public class QsDotToken : QsToken { public static QsDotToken Instance { get; } = new QsDotToken(); private QsDotToken() { } } // .
    
    public class QsIfToken : QsToken { public static QsIfToken Instance { get; } = new QsIfToken(); private QsIfToken() { } }
    public class QsElseToken : QsToken { public static QsElseToken Instance { get; } = new QsElseToken(); private QsElseToken() { } }
    public class QsForToken : QsToken { public static QsForToken Instance { get; } = new QsForToken(); private QsForToken() { } }
    public class QsContinueToken : QsToken { public static QsContinueToken Instance { get; } = new QsContinueToken(); private QsContinueToken() { } }
    public class QsBreakToken : QsToken { public static QsBreakToken Instance { get; } = new QsBreakToken(); private QsBreakToken() { } }
    public class QsExecToken : QsToken { public static QsExecToken Instance { get; } = new QsExecToken(); private QsExecToken() { } }
    public class QsTaskToken : QsToken { public static QsTaskToken Instance { get; } = new QsTaskToken(); private QsTaskToken() { } }
    public class QsParamsToken : QsToken { public static QsParamsToken Instance { get; } = new QsParamsToken(); private QsParamsToken() { } }
    public class QsReturnToken : QsToken { public static QsReturnToken Instance { get; } = new QsReturnToken(); private QsReturnToken() { } }
    public class QsAsyncToken : QsToken { public static QsAsyncToken Instance { get; } = new QsAsyncToken(); private QsAsyncToken() { } }
    public class QsAwaitToken : QsToken { public static QsAwaitToken Instance { get; } = new QsAwaitToken(); private QsAwaitToken() { } }

    public class QsWhitespaceToken : QsToken { public static QsWhitespaceToken Instance { get; } = new QsWhitespaceToken(); private QsWhitespaceToken() { } } // \s
    public class QsNewLineToken : QsToken { public static QsNewLineToken Instance { get; } = new QsNewLineToken(); private QsNewLineToken() { } }     // \r \n \r\n

    public class QsDoubleQuoteToken : QsToken { public static QsDoubleQuoteToken Instance { get; } = new QsDoubleQuoteToken(); private QsDoubleQuoteToken() { } } // "
    public class QsDollarLBraceToken : QsToken { public static QsDollarLBraceToken Instance { get; } = new QsDollarLBraceToken(); private QsDollarLBraceToken() { } }
    public class QsEndOfFileToken : QsToken { public static QsEndOfFileToken Instance { get; } = new QsEndOfFileToken(); private QsEndOfFileToken() { } }

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
            return EqualityComparer<QsIntToken?>.Default.Equals(left, right);
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
            return EqualityComparer<QsBoolToken?>.Default.Equals(left, right);
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
            return EqualityComparer<QsTextToken?>.Default.Equals(left, right);
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
