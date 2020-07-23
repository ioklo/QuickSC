using QuickSC.StaticAnalyzer;
using QuickSC.Syntax;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
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
        public class Instance : QsMemberExpInfo
        {
            public QsTypeValue ObjectTypeValue { get; }
            public QsName VarName { get; }

            public Instance(QsTypeValue objectTypeValue, QsName varName)
            {
                ObjectTypeValue = objectTypeValue;
                VarName = varName;
            }
        }

        public class Static : QsMemberExpInfo
        {
            public bool bEvaluateObject => ObjectTypeValue != null;
            public QsTypeValue? ObjectTypeValue { get; }
            public QsVarValue VarValue { get; }

            public Static(QsTypeValue? objectTypeValue, QsVarValue varValue)
            {
                ObjectTypeValue = objectTypeValue;
                VarValue = varValue;
            }
        }        

        public static QsMemberExpInfo MakeInstance(QsTypeValue objectTypeValue, QsName varName) 
            => new Instance(objectTypeValue, varName);

        public static QsMemberExpInfo MakeStatic(QsTypeValue? objectTypeValue, QsVarValue varValue)
            => new Static(objectTypeValue, varValue);
    }

    public class QsBinaryOpExpInfo : QsSyntaxNodeInfo
    {
        public enum OpType
        {
            Bool, Integer, String
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
        public ImmutableArray<QsTypeValue> ArgTypeValues { get; }

        public QsCallExpInfo(QsFuncValue? funcValue, ImmutableArray<QsTypeValue> argTypeValues) 
        { 
            FuncValue = funcValue;
            ArgTypeValues = argTypeValues;
        }
    }

    public class QsMemberCallExpInfo : QsSyntaxNodeInfo
    {
        public QsTypeValue? ObjectTypeValue { get; }
        public ImmutableArray<QsTypeValue> ArgTypeValues { get; set; }

        // C.F(), x.F() // F is static
        public class StaticFuncCall : QsMemberCallExpInfo
        {
            public bool bEvaluateObject { get => ObjectTypeValue != null; }
            public QsFuncValue FuncValue { get; }

            public StaticFuncCall(QsTypeValue? objectTypeValue, ImmutableArray<QsTypeValue> argTypeValues, QsFuncValue funcValue)
                : base(objectTypeValue, argTypeValues)
            {
                FuncValue = funcValue;
            }
        }

        // x.F()
        public class InstanceFuncCall : QsMemberCallExpInfo
        {
            public QsFuncValue FuncValue { get; }
            public InstanceFuncCall(QsTypeValue? objectTypeValue, ImmutableArray<QsTypeValue> argTypeValues, QsFuncValue funcValue)
                : base(objectTypeValue, argTypeValues)
            {
                FuncValue = funcValue;
            }
        }

        // x.f() C.f()
        public class InstanceLambdaCall : QsMemberCallExpInfo
        {
            public QsName VarName { get; }
            public InstanceLambdaCall(QsTypeValue? objectTypeValue, ImmutableArray<QsTypeValue> argTypeValues, QsName varName)
                : base(objectTypeValue, argTypeValues)
            {
                VarName = varName;
            }
        }

        public class StaticLambdaCall : QsMemberCallExpInfo
        {
            public bool bEvaluateObject { get => ObjectTypeValue != null; }
            public QsVarValue VarValue { get; }
            public StaticLambdaCall(QsTypeValue? objectTypeValue, ImmutableArray<QsTypeValue> argTypeValues, QsVarValue varValue)
                : base(objectTypeValue, argTypeValues)
            {
                VarValue = varValue;
            }
        }

        // 네개 씩이나 나눠야 하다니
        public static QsMemberCallExpInfo MakeStaticFunc(QsTypeValue? objectTypeValue, ImmutableArray<QsTypeValue> argTypeValues, QsFuncValue funcValue)
            => new StaticFuncCall(objectTypeValue, argTypeValues, funcValue);

        public static QsMemberCallExpInfo MakeInstanceFunc(QsTypeValue? objectTypeValue, ImmutableArray<QsTypeValue> argTypeValues, QsFuncValue funcValue)
            => new InstanceFuncCall(objectTypeValue, argTypeValues, funcValue);

        public static QsMemberCallExpInfo MakeStaticLambda(QsTypeValue? objectTypeValue, ImmutableArray<QsTypeValue> argTypeValues, QsVarValue varValue)
            => new StaticLambdaCall(objectTypeValue, argTypeValues, varValue);

        public static QsMemberCallExpInfo MakeInstanceLambda(QsTypeValue? objectTypeValue, ImmutableArray<QsTypeValue> argTypeValues, QsName varName)
            => new InstanceLambdaCall(objectTypeValue, argTypeValues, varName);

        // 왜 private 인데 base()가 먹는지;
        private QsMemberCallExpInfo(QsTypeValue? objectTypeValue, ImmutableArray<QsTypeValue> argTypeValues)
        {
            ObjectTypeValue = objectTypeValue;
            ArgTypeValues = argTypeValues;
        }
    }

    public class QsExpStmtInfo : QsSyntaxNodeInfo
    {
        public QsTypeValue ExpTypeValue { get; }
        public QsExpStmtInfo(QsTypeValue expTypeValue)
        {
            ExpTypeValue = expTypeValue;
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

    public class QsForStmtInfo : QsSyntaxNodeInfo
    {
        public QsTypeValue? ContTypeValue { get; }
        public QsForStmtInfo(QsTypeValue? contTypeValue)
        {
            ContTypeValue = contTypeValue;
        }
    }

    public class QsExpForStmtInitializerInfo : QsSyntaxNodeInfo
    {
        public QsTypeValue ExpTypeValue { get; }
        public QsExpForStmtInitializerInfo(QsTypeValue expTypeValue)
        {
            ExpTypeValue = expTypeValue;
        }
    }

    public class QsForeachStmtInfo : QsSyntaxNodeInfo
    {
        public QsTypeValue ObjTypeValue { get; }
        public QsTypeValue EnumeratorTypeValue { get; }

        public QsTypeValue ElemTypeValue { get; }
        public int ElemLocalIndex { get; }
        public QsFuncValue GetEnumeratorValue { get; }
        public QsFuncValue MoveNextValue { get; }
        public QsFuncValue GetCurrentValue { get; }        

        public QsForeachStmtInfo(QsTypeValue objTypeValue, QsTypeValue enumeratorTypeValue, QsTypeValue elemTypeValue, int elemLocalIndex, QsFuncValue getEnumeratorValue, QsFuncValue moveNextValue, QsFuncValue getCurrentValue)
        {
            ObjTypeValue = objTypeValue;
            EnumeratorTypeValue = enumeratorTypeValue;

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
        public QsTypeValue ObjectTypeValue { get; }
        public QsTypeValue IndexTypeValue { get; }

        public QsIndexerExpInfo(QsFuncValue funcValue, QsTypeValue objectTypeValue, QsTypeValue indexTypeValue)
        {
            FuncValue = funcValue;
            ObjectTypeValue = objectTypeValue;
            IndexTypeValue = indexTypeValue;
        }
    }    

    public class QsExpStringExpElementInfo : QsSyntaxNodeInfo
    {
        public QsTypeValue ExpTypeValue { get; }
        public QsExpStringExpElementInfo(QsTypeValue expTypeValue) 
        { 
            ExpTypeValue = expTypeValue; 
        }
    }
}
