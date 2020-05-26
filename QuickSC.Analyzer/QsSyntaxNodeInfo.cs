using QuickSC.StaticAnalyzer;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using System.Text;

namespace QuickSC
{
    public abstract class QsSyntaxNodeInfo
    {
    }
    
    public class QsIdentifierExpInfo : QsSyntaxNodeInfo
    {
        public abstract class IdKind 
        {
            public class GlobalVar : IdKind
            {
                public QsVarId VarId { get; }
                public GlobalVar(QsVarId varId) { VarId = varId; }
            }

            public class LocalVar : IdKind
            {
                public int LocalIndex { get; }
                public LocalVar(int localIndex) { LocalIndex = localIndex; }
            }
        }

        // public class QsThisStorage : QsStorage
        // public class QsStaticThisStorage : QsStorage

        static public QsIdentifierExpInfo MakeGlobal(QsVarId varId) => new QsIdentifierExpInfo(new IdKind.GlobalVar(varId));
        static public QsIdentifierExpInfo MakeLocal(int localIndex) => new QsIdentifierExpInfo(new IdKind.LocalVar(localIndex));

        public IdKind Kind { get; }
        private QsIdentifierExpInfo(IdKind kind) { Kind = kind; }
    }

    public class QsLambdaExpInfo : QsSyntaxNodeInfo
    {
        public struct Elem
        {
            public abstract class ExpKind 
            {
                public class GlobalVar : ExpKind
                {
                    public QsVarId VarId { get; }
                    public GlobalVar(QsVarId varId) { VarId = varId; }
                }

                public class LocalVar : ExpKind
                {
                    public int LocalIndex { get; }
                    public LocalVar(int localIndex) { LocalIndex = localIndex; }
                }
            }

            public QsCaptureKind CaptureKind { get; }
            public ExpKind Kind { get; }

            public static Elem MakeGlobal(QsCaptureKind kind, QsVarId varId) => new Elem(kind, new ExpKind.GlobalVar(varId));
            public static Elem MakeLocal(QsCaptureKind kind, int localIndex ) => new Elem(kind, new ExpKind.LocalVar(localIndex));

            private Elem(QsCaptureKind captureKind, ExpKind expKind)
            {
                CaptureKind = captureKind;
                Kind = expKind;
            }
        }

        public bool bCaptureThis { get; }

        // 캡쳐 변수들
        public ImmutableArray<Elem> CaptureElems { get; }

        public QsLambdaExpInfo(bool bCaptureThis, ImmutableArray<Elem> captureElems)
        {
            this.bCaptureThis = bCaptureThis;
            CaptureElems = captureElems;
        }        
    }

    public class QsMemberExpInfo : QsSyntaxNodeInfo
    {
        public abstract class ExpKind
        {
            public class Instance : ExpKind
            {
                public QsVarId VarId { get; }
                public Instance(QsVarId varId)
                {
                    VarId = varId;
                }
            }

            public class Static : ExpKind
            {   
                public bool bEvaluateObject { get; }
                public QsTypeValue TypeValue { get; }
                public QsVarId VarId { get; }
                public Static(bool bEvaluateObject, QsTypeValue typeValue, QsVarId varId)
                {
                    this.bEvaluateObject = bEvaluateObject;
                    TypeValue = typeValue;
                    VarId = varId;
                }
            }
        }

        public ExpKind Kind { get; }

        public static QsMemberExpInfo MakeInstance(QsVarId varId) => new QsMemberExpInfo(new ExpKind.Instance(varId));
        public static QsMemberExpInfo MakeStatic(bool bEvaluateObject, QsTypeValue typeValue, QsVarId varId) => 
            new QsMemberExpInfo(new ExpKind.Static(bEvaluateObject, typeValue, varId));

        private QsMemberExpInfo(ExpKind kind) { Kind = kind; }
    }

    public class QsBinaryOpExpInfo : QsSyntaxNodeInfo
    {
        public enum OpType
        {
            Integer, String
        }

        public OpType Type { get; }
        public QsBinaryOpExpInfo(OpType type)
        {
            Type = type;
        }
    }

    public class QsCallExpInfo : QsSyntaxNodeInfo
    {
        public QsFuncValue? FuncValue { get; }
        public QsCallExpInfo(QsFuncValue? funcValue) { FuncValue = funcValue; }
    }
}
