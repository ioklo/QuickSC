using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;

namespace QuickSC.StaticAnalyzer
{
    public struct QsVarDeclInfoElem
    {
        public QsTypeValue TypeValue { get; }
        public QsStorage Storage { get; }

        public QsVarDeclInfoElem(QsTypeValue typeValue, QsStorage storage)
        {
            TypeValue = typeValue;
            Storage = storage;
        }
    }

    public class QsVarDeclInfo
    {
        public ImmutableArray<QsVarDeclInfoElem> Elems { get; }
        public QsVarDeclInfo(ImmutableArray<QsVarDeclInfoElem> elems)
        {
            Elems = elems;
        }
    }
}
