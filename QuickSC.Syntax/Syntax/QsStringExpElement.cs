using System;
using System.Collections.Generic;
using System.Text;

namespace QuickSC.Syntax
{
    public abstract class QsStringExpElement
    {
    }

    public class QsTextStringExpElement : QsStringExpElement
    {
        public string Text { get; }
        public QsTextStringExpElement(string text) { Text = text; }
    }
}
