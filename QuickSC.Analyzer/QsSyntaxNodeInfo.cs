using QuickSC.StaticAnalyzer;
using QuickSC.Syntax;
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
        public QsStorage Storage { get; }
        public QsIdentifierExpInfo(QsStorage storage) { Storage = storage; }
    }

    public class QsLambdaExpInfo : QsSyntaxNodeInfo
    {   
        public QsCaptureInfo CaptureInfo { get; }
        public int LocalVarCount { get; }

        public QsLambdaExpInfo(QsCaptureInfo captureInfo, int localVarCount)
        {
            CaptureInfo = captureInfo;
            LocalVarCount = localVarCount;
        }        
    }

    public class QsListExpInfo : QsSyntaxNodeInfo
    {
        public QsTypeValue ElemTypeValue { get; }
        public QsListExpInfo(QsTypeValue elemTypeValue) { ElemTypeValue = elemTypeValue; }
    }

    public class QsMemberExpInfo : QsSyntaxNodeInfo
    {
        public abstract class ExpKind
        {
            public class Instance : ExpKind
            {
                public QsMetaItemId VarId { get; }
                public Instance(QsMetaItemId varId)
                {
                    VarId = varId;
                }
            }

            public class Static : ExpKind
            {   
                public bool bEvaluateObject { get; }
                public QsVarValue VarValue { get; }                
                public Static(bool bEvaluateObject, QsVarValue varValue)
                {
                    this.bEvaluateObject = bEvaluateObject;
                    VarValue = varValue;
                }
            }
        }

        public ExpKind Kind { get; }

        public static QsMemberExpInfo MakeInstance(QsMetaItemId varId) => new QsMemberExpInfo(new ExpKind.Instance(varId));
        public static QsMemberExpInfo MakeStatic(bool bEvaluateObject, QsVarValue varValue) => 
            new QsMemberExpInfo(new ExpKind.Static(bEvaluateObject, varValue));

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

    public class QsMemberCallExpInfo : QsSyntaxNodeInfo
    {
        public abstract class CallKind
        {
            // C.F(), x.F() // F is static
            public class StaticFuncCall : CallKind
            {
                public bool bEvaluateObject { get; }
                public QsFuncValue FuncValue { get; }
                public StaticFuncCall(bool bEvaluateObject, QsFuncValue funcValue)
                {
                    this.bEvaluateObject = bEvaluateObject;
                    FuncValue = funcValue;
                }
            }

            // x.F()
            public class InstanceFuncCall : CallKind
            {
                public QsFuncValue FuncValue { get; }
                public InstanceFuncCall(QsFuncValue funcValue)
                {
                    FuncValue = funcValue;

                }
            }

            // x.f() C.f()
            public class InstanceLambdaCall : CallKind
            {
                public QsMetaItemId VarId { get; }
                public InstanceLambdaCall(QsMetaItemId varId)
                {
                    VarId = varId;
                }
            }

            public class StaticLambdaCall : CallKind
            {
                public bool bEvaluateObject { get; }                
                public QsVarValue VarValue { get; }
                public StaticLambdaCall(bool bEvaluateObject, QsVarValue varValue)
                {
                    this.bEvaluateObject = bEvaluateObject;
                    VarValue = varValue;
                }
            }
        }

        // 네개 씩이나 나눠야 하다니
        public CallKind Kind { get; }

        public static QsMemberCallExpInfo MakeStaticFunc(bool bEvaluateObject, QsFuncValue funcValue)
            => new QsMemberCallExpInfo(new CallKind.StaticFuncCall(bEvaluateObject, funcValue));

        public static QsMemberCallExpInfo MakeInstanceFunc(QsFuncValue funcValue)
            => new QsMemberCallExpInfo(new CallKind.InstanceFuncCall(funcValue));

        public static QsMemberCallExpInfo MakeStaticLambda(bool bEvaluateObject, QsVarValue varValue)
            => new QsMemberCallExpInfo(new CallKind.StaticLambdaCall(bEvaluateObject, varValue));

        public static QsMemberCallExpInfo MakeInstanceLambda(QsMetaItemId varId)
            => new QsMemberCallExpInfo(new CallKind.InstanceLambdaCall(varId));


        private QsMemberCallExpInfo(CallKind kind)
        {
            Kind = kind;
        }
    }

    public class QsVarDeclInfo : QsSyntaxNodeInfo
    {
        public class Element
        {
            public QsTypeValue TypeValue { get; }
            public QsStorage Storage { get; }
            
            public Element(QsTypeValue typeValue, QsStorage storage)
            {
                TypeValue = typeValue;
                Storage = storage;
            }
        }
        
        public ImmutableArray<Element> Elems;

        public QsVarDeclInfo(ImmutableArray<Element> elems)
        {
            Elems = elems;
        }
    }

    public class QsIfStmtInfo : QsSyntaxNodeInfo
    {
        public QsTypeValue TestTypeValue { get; }

        public QsIfStmtInfo(QsTypeValue testTypeValue)
        {
            TestTypeValue = testTypeValue;
        }
    }

    public class QsTaskStmtInfo : QsSyntaxNodeInfo
    {
        public QsCaptureInfo CaptureInfo { get; }
        public int LocalVarCount { get; }

        public QsTaskStmtInfo(QsCaptureInfo captureInfo, int localVarCount)
        {
            CaptureInfo = captureInfo;
            LocalVarCount = localVarCount;
        }

    }

    public class QsAsyncStmtInfo : QsSyntaxNodeInfo
    {
        public QsCaptureInfo CaptureInfo { get; }
        public int LocalVarCount { get; }

        public QsAsyncStmtInfo(QsCaptureInfo captureInfo, int localVarCount)
        {
            CaptureInfo = captureInfo;
            LocalVarCount = localVarCount;
        }
    }

    public class QsForeachStmtInfo : QsSyntaxNodeInfo
    {
        public QsTypeValue ElemTypeValue { get; }
        public int ElemLocalIndex { get; }
        public QsFuncValue GetEnumeratorValue { get; }
        public QsFuncValue MoveNextValue { get; }
        public QsFuncValue GetCurrentValue { get; }

        public QsForeachStmtInfo(QsTypeValue elemTypeValue, int elemLocalIndex, QsFuncValue getEnumeratorValue, QsFuncValue moveNextValue, QsFuncValue getCurrentValue)
        {
            ElemTypeValue = elemTypeValue;
            ElemLocalIndex = elemLocalIndex;
            GetEnumeratorValue = getEnumeratorValue;
            MoveNextValue = moveNextValue;
            GetCurrentValue = getCurrentValue;
        }
    }

    public class QsScriptInfo : QsSyntaxNodeInfo
    {
        public int LocalVarCount { get; }
        public QsScriptInfo(int localVarCount)
        {
            LocalVarCount = localVarCount;
        }
    }

    public class QsIndexerExpInfo : QsSyntaxNodeInfo
    {
        public QsFuncValue FuncValue { get; }
        public QsIndexerExpInfo(QsFuncValue funcValue)
        {
            FuncValue = funcValue;
        }
    }
}
