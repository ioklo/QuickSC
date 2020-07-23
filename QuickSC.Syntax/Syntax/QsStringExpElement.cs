using System;
using System.Collections.Generic;
using System.Text;

namespace QuickSC.Syntax
{
    public abstract class QsStringExpElement : IQsSyntaxNode
    {
    }

    public class QsTextStringExpElement : QsStringExpElement
    {
        public string Text { get; }
        public QsTextStringExpElement(string text) { Text = text; }

        public override bool Equals(object? obj)
        {
            return obj is QsTextStringExpElement element &&
                   Text == element.Text;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Text);
        }

        public static bool operator ==(QsTextStringExpElement? left, QsTextStringExpElement? right)
        {
            return EqualityComparer<QsTextStringExpElement?>.Default.Equals(left, right);
        }

        public static bool operator !=(QsTextStringExpElement? left, QsTextStringExpElement? right)
        {
            return !(left == right);
        }
    }

    public class QsExpStringExpElement : QsStringExpElement
    {
        public QsExp Exp { get; }
        public QsExpStringExpElement(QsExp exp) { Exp = exp; }

        public override bool Equals(object? obj)
        {
            return obj is QsExpStringExpElement element &&
                   EqualityComparer<QsExp>.Default.Equals(Exp, element.Exp);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Exp);
        }

        public static bool operator ==(QsExpStringExpElement? left, QsExpStringExpElement? right)
        {
            return EqualityComparer<QsExpStringExpElement?>.Default.Equals(left, right);
        }

        public static bool operator !=(QsExpStringExpElement? left, QsExpStringExpElement? right)
        {
            return !(left == right);
        }
    }
}
