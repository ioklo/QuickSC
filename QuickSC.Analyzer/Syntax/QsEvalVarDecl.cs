using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;

namespace QuickSC.Syntax
{
    public abstract class QsEvalVarDecl { }

    public class QsGlobalVarDecl : QsEvalVarDecl
    {
        public struct Elem
        {
            public QsTypeValue TypeValue { get; }            
            public QsVarId VarId { get; }
            public QsExp? InitExp { get; }
            public Elem(QsTypeValue typeValue, QsVarId varId, QsExp? initExp) { TypeValue = typeValue; VarId = varId; InitExp = initExp; }
        }

        public ImmutableArray<Elem> Elems { get; }

        public QsGlobalVarDecl(ImmutableArray<Elem> elems)
        {
            Elems = elems;
        }
    }

    public class QsLocalVarDecl : QsEvalVarDecl
    {
        public struct Elem
        {
            public QsTypeValue TypeValue { get; }
            public int LocalIndex { get; }
            public QsExp? InitExp { get; }
            public Elem(QsTypeValue typeValue, int localIndex, QsExp? initExp) { TypeValue = typeValue; LocalIndex = localIndex; InitExp = initExp; }
        }

        public ImmutableArray<Elem> Elems { get; }

        public QsLocalVarDecl(ImmutableArray<Elem> elems)
        {
            Elems = elems;
        }
    }

}
