using QuickSC.StaticAnalyzer;
using Gum.Syntax;
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
        public QsStorageInfo StorageInfo { get; }
        public QsIdentifierExpInfo(QsStorageInfo storageInfo) { StorageInfo = storageInfo; }
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

        // E.One
        public class EnumElem : QsMemberExpInfo
        {
            public QsTypeValue.Normal EnumTypeValue { get; }
            public string Name { get; }

            public EnumElem(QsTypeValue.Normal enumTypeValue, string name)
            {
                EnumTypeValue = enumTypeValue;
                Name = name;
            }
        }

        // e.i
        public class EnumElemField : QsMemberExpInfo
        {
            public QsTypeValue.Normal ObjectTypeValue;
            public string Name { get; }

            public EnumElemField(QsTypeValue.Normal objTypeValue, string name)
            {
                ObjectTypeValue = objTypeValue;
                Name = name;
            }
        }

        public static QsMemberExpInfo MakeInstance(QsTypeValue objectTypeValue, QsName varName) 
            => new Instance(objectTypeValue, varName);

        public static QsMemberExpInfo MakeStatic(QsTypeValue? objectTypeValue, QsVarValue varValue)
            => new Static(objectTypeValue, varValue);

        public static QsMemberExpInfo MakeEnumElem(QsTypeValue.Normal enumTypeValue, string elemName)
            => new EnumElem(enumTypeValue, elemName);

        public static QsMemberExpInfo MakeEnumElemField(QsTypeValue.Normal objTypeValue, string name)
            => new EnumElemField(objTypeValue, name);
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

    public class QsBinaryOpExpAssignInfo : QsSyntaxNodeInfo
    {
        public class Direct : QsBinaryOpExpAssignInfo        
        {
            public QsStorageInfo StorageInfo { get; }
            public Direct(QsStorageInfo storageInfo) { StorageInfo = storageInfo; }
        }
        
        public class CallSetter : QsBinaryOpExpAssignInfo
        {
            public QsTypeValue? ObjectTypeValue { get; }
            public Exp? Object { get;}            
            public QsFuncValue Setter { get;}
            public ImmutableArray<(Exp Exp, QsTypeValue TypeValue)> Arguments { get; }
            public QsTypeValue ValueTypeValue { get; }

            public CallSetter(
                QsTypeValue? objTypeValue,
                Exp? obj,
                QsFuncValue setter,
                IEnumerable<(Exp Exp, QsTypeValue TypeValue)> arguments,
                QsTypeValue valueTypeValue)
            {
                ObjectTypeValue = objTypeValue;
                Object = obj;                
                Setter = setter;
                Arguments = arguments.ToImmutableArray();
                ValueTypeValue = valueTypeValue;
            }
        }

        public static Direct MakeDirect(QsStorageInfo storageInfo) 
            => new Direct(storageInfo);

        public static CallSetter MakeCallSetter(
            QsTypeValue? objTypeValue,
            Exp? obj,
            QsFuncValue setter,
            IEnumerable<(Exp Exp, QsTypeValue TypeValue)> arguments,
            QsTypeValue valueTypeValue)
            => new CallSetter(objTypeValue, obj, setter, arguments, valueTypeValue);
    }

    public class QsUnaryOpExpAssignInfo : QsSyntaxNodeInfo
    {
        public class Direct : QsUnaryOpExpAssignInfo
        {
            public QsStorageInfo StorageInfo { get; }
            public QsFuncValue OperatorValue { get; }
            public bool bReturnPrevValue { get; }
            public QsTypeValue ValueTypeValue { get; }

            public Direct(QsStorageInfo storageInfo, QsFuncValue operatorValue, bool bReturnPrevValue, QsTypeValue valueTypeValue) 
            { 
                StorageInfo = storageInfo;
                OperatorValue = operatorValue;
                this.bReturnPrevValue = bReturnPrevValue;
                ValueTypeValue = valueTypeValue;
            }
        }

        public class CallFunc : QsUnaryOpExpAssignInfo
        {
            public Exp? ObjectExp { get; }
            public QsTypeValue? ObjectTypeValue { get; }

            public QsTypeValue ValueTypeValue0 { get; }
            public QsTypeValue ValueTypeValue1 { get; }
            public bool bReturnPrevValue { get; }

            // Getter/setter Arguments without setter value
            public ImmutableArray<(Exp Exp, QsTypeValue TypeValue)> Arguments { get; }

            public QsFuncValue Getter { get; }            
            public QsFuncValue Setter { get; }
            public QsFuncValue Operator { get; }

            public CallFunc(
                Exp? objectExp,
                QsTypeValue? objectTypeValue,
                QsTypeValue valueTypeValue0,
                QsTypeValue valueTypeValue1,
                bool bReturnPrevValue,
                IEnumerable<(Exp Exp, QsTypeValue TypeValue)> arguments,
                QsFuncValue getter,
                QsFuncValue setter,
                QsFuncValue op)
            {
                ObjectExp = objectExp;
                ObjectTypeValue = objectTypeValue;
                ValueTypeValue0 = valueTypeValue0;
                ValueTypeValue1 = valueTypeValue1;
                this.bReturnPrevValue= bReturnPrevValue;

                Arguments = arguments.ToImmutableArray();
                Getter = getter;
                Setter = setter;
                Operator = op;
            }
        }

        public static Direct MakeDirect(QsStorageInfo storageInfo, QsFuncValue operatorValue, bool bReturnPrevValue, QsTypeValue valueTypeValue)
            => new Direct(storageInfo, operatorValue, bReturnPrevValue, valueTypeValue);

        public static CallFunc MakeCallFunc(
            Exp? objectExp,
            QsTypeValue? objectTypeValue,
            QsTypeValue valueTypeValue0,
            QsTypeValue valueTypeValue1,
            bool bReturnPrevValue,
            IEnumerable<(Exp Exp, QsTypeValue TypeValue)> arguments,
            QsFuncValue getter,
            QsFuncValue setter,
            QsFuncValue op)
            => new CallFunc(objectExp, objectTypeValue, valueTypeValue0, valueTypeValue1, bReturnPrevValue, arguments, getter, setter, op);
    }

    public abstract class QsCallExpInfo : QsSyntaxNodeInfo
    {
        public class Normal : QsCallExpInfo
        {
            public QsFuncValue? FuncValue { get; }
            public ImmutableArray<QsTypeValue> ArgTypeValues { get; }

            public Normal(QsFuncValue? funcValue, IEnumerable<QsTypeValue> argTypeValues)
            {
                FuncValue = funcValue;
                ArgTypeValues = argTypeValues.ToImmutableArray();
            }
        }

        public class EnumValue : QsCallExpInfo
        {
            public  QsEnumElemInfo ElemInfo { get; }
            public ImmutableArray<QsTypeValue> ArgTypeValues { get; }

            public EnumValue(QsEnumElemInfo elemInfo, IEnumerable<QsTypeValue> argTypeValues)
            {
                ElemInfo = elemInfo;
                ArgTypeValues = argTypeValues.ToImmutableArray();
            }
        }

        public static Normal MakeNormal(QsFuncValue? funcValue, ImmutableArray<QsTypeValue> argTypeValues) => new Normal(funcValue, argTypeValues);
        public static EnumValue MakeEnumValue(QsEnumElemInfo elemInfo, IEnumerable<QsTypeValue> argTypeValues) => new EnumValue(elemInfo, argTypeValues);
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

        public class EnumValue : QsMemberCallExpInfo
        {
            public QsEnumElemInfo ElemInfo { get; }
            public EnumValue(QsTypeValue? objectTypeValue, ImmutableArray<QsTypeValue> argTypeValues, QsEnumElemInfo elemInfo)
                : base(objectTypeValue, argTypeValues)
            {
                ElemInfo = elemInfo;
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

        public static QsMemberCallExpInfo MakeEnumValue(QsTypeValue? objectTypeValue, ImmutableArray<QsTypeValue> argTypeValues, QsEnumElemInfo elemInfo)
            => new EnumValue(objectTypeValue, argTypeValues, elemInfo);

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
            public QsStorageInfo StorageInfo { get; }
            
            public Element(QsTypeValue typeValue, QsStorageInfo storageInfo)
            {
                TypeValue = typeValue;
                StorageInfo = storageInfo;
            }
        }
        
        public ImmutableArray<Element> Elems;

        public QsVarDeclInfo(ImmutableArray<Element> elems)
        {
            Elems = elems;
        }
    }

    public abstract class QsIfStmtInfo : QsSyntaxNodeInfo
    {
        public class TestEnum : QsIfStmtInfo
        {
            public QsTypeValue TestTargetTypeValue { get; }
            public string ElemName { get; }

            public TestEnum(QsTypeValue testTargetTypeValue, string elemName)
            {
                TestTargetTypeValue = testTargetTypeValue;
                ElemName = elemName;
            }
        }

        public class TestClass : QsIfStmtInfo
        {
            public QsTypeValue TestTargetTypeValue { get; }
            public QsTypeValue TestTypeValue { get; }

            public TestClass(QsTypeValue testTargetTypeValue, QsTypeValue testTypeValue)
            {
                TestTargetTypeValue = testTargetTypeValue;
                TestTypeValue = testTypeValue;
            }
        }

        public static TestEnum MakeTestEnum(QsTypeValue testTargetTypeValue, string elemName) => new TestEnum(testTargetTypeValue, elemName);
        public static TestClass MakeTestClass(QsTypeValue testTargetTypeValue, QsTypeValue testTypeValue) => new TestClass(testTargetTypeValue, testTypeValue);
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
