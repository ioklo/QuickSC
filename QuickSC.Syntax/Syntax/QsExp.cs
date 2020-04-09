using System;
using System.Collections.Generic;
using System.Text;

namespace QuickSC.Syntax
{
    public abstract class QsExp
    {
    }

    public class QsIdentifierExp : QsExp
    {
        public string Value;
        public QsIdentifierExp(string value) { Value = value; }
    }

    public class QsStringExp : QsExp
    {
        public List<QsStringExpElement> Elements { get; }
        
        public QsStringExp(List<QsStringExpElement> elements)
        {
            Elements = elements;
        }
    }
}
