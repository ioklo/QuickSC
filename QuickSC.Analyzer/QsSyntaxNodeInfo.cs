using QuickSC.StaticAnalyzer;
using Gum.Syntax;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text;
using Gum.CompileTime;

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
        public TypeValue ElemTypeValue { get; }
        public QsListExpInfo(TypeValue elemTypeValue) { ElemTypeValue = elemTypeValue; }
    }

    public class QsMemberExpInfo : QsSyntaxNodeInfo
    {
        public class Instance : QsMemberExpInfo
        {
            public TypeValue ObjectTypeValue { get; }
            public Name VarName { get; }

            public Instance(TypeValue objectTypeValue, Name varName)
            {
                ObjectTypeValue = objectTypeValue;
                VarName = varName;
            }
        }

        public class Static : QsMemberExpInfo
        {
            public bool bEvaluateObject => ObjectTypeValue != null;
            public TypeValue? ObjectTypeValue { get; }
            public VarValue VarValue { get; }

            public Static(TypeValue? objectTypeValue, VarValue varValue)
            {
                ObjectTypeValue = objectTypeValue;
                VarValue = varValue;
            }
        }

        // E.One
        public class EnumElem : QsMemberExpInfo
        {
            public TypeValue.Normal EnumTypeValue { get; }
            public string Name { get; }

            public EnumElem(TypeValue.Normal enumTypeValue, string name)
            {
                EnumTypeValue = enumTypeValue;
                Name = name;
            }
        }

        // e.i
        public class EnumElemField : QsMemberExpInfo
        {
            public TypeValue.Normal ObjectTypeValue;
            public string Name { get; }

            public EnumElemField(TypeValue.Normal objTypeValue, string name)
            {
                ObjectTypeValue = objTypeValue;
                Name = name;
            }
        }

        public static QsMemberExpInfo MakeInstance(TypeValue objectTypeValue, Name varName) 
            => new Instance(objectTypeValue, varName);

        public static QsMemberExpInfo MakeStatic(TypeValue? objectTypeValue, VarValue varValue)
            => new Static(objectTypeValue, varValue);

        public static QsMemberExpInfo MakeEnumElem(TypeValue.Normal enumTypeValue, string elemName)
            => new EnumElem(enumTypeValue, elemName);

        public static QsMemberExpInfo MakeEnumElemField(TypeValue.Normal objTypeValue, string name)
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
            public TypeValue? ObjectTypeValue { get; }
            public Exp? Object { get;}            
            public FuncValue Setter { get;}
            public ImmutableArray<(Exp Exp, TypeValue TypeValue)> Arguments { get; }
            public TypeValue ValueTypeValue { get; }

            public CallSetter(
                TypeValue? objTypeValue,
                Exp? obj,
                FuncValue setter,
                IEnumerable<(Exp Exp, TypeValue TypeValue)> arguments,
                TypeValue valueTypeValue)
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
            TypeValue? objTypeValue,
            Exp? obj,
            FuncValue setter,
            IEnumerable<(Exp Exp, TypeValue TypeValue)> arguments,
            TypeValue valueTypeValue)
            => new CallSetter(objTypeValue, obj, setter, arguments, valueTypeValue);
    }

    public class QsUnaryOpExpAssignInfo : QsSyntaxNodeInfo
    {
        public class Direct : QsUnaryOpExpAssignInfo
        {
            public QsStorageInfo StorageInfo { get; }
            public FuncValue OperatorValue { get; }
            public bool bReturnPrevValue { get; }
            public TypeValue ValueTypeValue { get; }

            public Direct(QsStorageInfo storageInfo, FuncValue operatorValue, bool bReturnPrevValue, TypeValue valueTypeValue) 
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
            public TypeValue? ObjectTypeValue { get; }

            public TypeValue ValueTypeValue0 { get; }
            public TypeValue ValueTypeValue1 { get; }
            public bool bReturnPrevValue { get; }

            // Getter/setter Arguments without setter value
            public ImmutableArray<(Exp Exp, TypeValue TypeValue)> Arguments { get; }

            public FuncValue Getter { get; }            
            public FuncValue Setter { get; }
            public FuncValue Operator { get; }

            public CallFunc(
                Exp? objectExp,
                TypeValue? objectTypeValue,
                TypeValue valueTypeValue0,
                TypeValue valueTypeValue1,
                bool bReturnPrevValue,
                IEnumerable<(Exp Exp, TypeValue TypeValue)> arguments,
                FuncValue getter,
                FuncValue setter,
                FuncValue op)
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

        public static Direct MakeDirect(QsStorageInfo storageInfo, FuncValue operatorValue, bool bReturnPrevValue, TypeValue valueTypeValue)
            => new Direct(storageInfo, operatorValue, bReturnPrevValue, valueTypeValue);

        public static CallFunc MakeCallFunc(
            Exp? objectExp,
            TypeValue? objectTypeValue,
            TypeValue valueTypeValue0,
            TypeValue valueTypeValue1,
            bool bReturnPrevValue,
            IEnumerable<(Exp Exp, TypeValue TypeValue)> arguments,
            FuncValue getter,
            FuncValue setter,
            FuncValue op)
            => new CallFunc(objectExp, objectTypeValue, valueTypeValue0, valueTypeValue1, bReturnPrevValue, arguments, getter, setter, op);
    }

    public abstract class QsCallExpInfo : QsSyntaxNodeInfo
    {
        public class Normal : QsCallExpInfo
        {
            public FuncValue? FuncValue { get; }
            public ImmutableArray<TypeValue> ArgTypeValues { get; }

            public Normal(FuncValue? funcValue, IEnumerable<TypeValue> argTypeValues)
            {
                FuncValue = funcValue;
                ArgTypeValues = argTypeValues.ToImmutableArray();
            }
        }

        public class EnumValue : QsCallExpInfo
        {
            public  EnumElemInfo ElemInfo { get; }
            public ImmutableArray<TypeValue> ArgTypeValues { get; }

            public EnumValue(EnumElemInfo elemInfo, IEnumerable<TypeValue> argTypeValues)
            {
                ElemInfo = elemInfo;
                ArgTypeValues = argTypeValues.ToImmutableArray();
            }
        }

        public static Normal MakeNormal(FuncValue? funcValue, ImmutableArray<TypeValue> argTypeValues) => new Normal(funcValue, argTypeValues);
        public static EnumValue MakeEnumValue(EnumElemInfo elemInfo, IEnumerable<TypeValue> argTypeValues) => new EnumValue(elemInfo, argTypeValues);
    }

    public class QsMemberCallExpInfo : QsSyntaxNodeInfo
    {
        public TypeValue? ObjectTypeValue { get; }
        public ImmutableArray<TypeValue> ArgTypeValues { get; set; }

        // C.F(), x.F() // F is static
        public class StaticFuncCall : QsMemberCallExpInfo
        {
            public bool bEvaluateObject { get => ObjectTypeValue != null; }
            public FuncValue FuncValue { get; }

            public StaticFuncCall(TypeValue? objectTypeValue, ImmutableArray<TypeValue> argTypeValues, FuncValue funcValue)
                : base(objectTypeValue, argTypeValues)
            {
                FuncValue = funcValue;
            }
        }

        // x.F()
        public class InstanceFuncCall : QsMemberCallExpInfo
        {
            public FuncValue FuncValue { get; }
            public InstanceFuncCall(TypeValue? objectTypeValue, ImmutableArray<TypeValue> argTypeValues, FuncValue funcValue)
                : base(objectTypeValue, argTypeValues)
            {
                FuncValue = funcValue;
            }
        }

        // x.f() C.f()
        public class InstanceLambdaCall : QsMemberCallExpInfo
        {
            public Name VarName { get; }
            public InstanceLambdaCall(TypeValue? objectTypeValue, ImmutableArray<TypeValue> argTypeValues, Name varName)
                : base(objectTypeValue, argTypeValues)
            {
                VarName = varName;
            }
        }

        public class StaticLambdaCall : QsMemberCallExpInfo
        {
            public bool bEvaluateObject { get => ObjectTypeValue != null; }
            public VarValue VarValue { get; }
            public StaticLambdaCall(TypeValue? objectTypeValue, ImmutableArray<TypeValue> argTypeValues, VarValue varValue)
                : base(objectTypeValue, argTypeValues)
            {
                VarValue = varValue;
            }
        }

        public class EnumValue : QsMemberCallExpInfo
        {
            public EnumElemInfo ElemInfo { get; }
            public EnumValue(TypeValue? objectTypeValue, ImmutableArray<TypeValue> argTypeValues, EnumElemInfo elemInfo)
                : base(objectTypeValue, argTypeValues)
            {
                ElemInfo = elemInfo;
            }
        }

        // 네개 씩이나 나눠야 하다니
        public static QsMemberCallExpInfo MakeStaticFunc(TypeValue? objectTypeValue, ImmutableArray<TypeValue> argTypeValues, FuncValue funcValue)
            => new StaticFuncCall(objectTypeValue, argTypeValues, funcValue);

        public static QsMemberCallExpInfo MakeInstanceFunc(TypeValue? objectTypeValue, ImmutableArray<TypeValue> argTypeValues, FuncValue funcValue)
            => new InstanceFuncCall(objectTypeValue, argTypeValues, funcValue);

        public static QsMemberCallExpInfo MakeStaticLambda(TypeValue? objectTypeValue, ImmutableArray<TypeValue> argTypeValues, VarValue varValue)
            => new StaticLambdaCall(objectTypeValue, argTypeValues, varValue);

        public static QsMemberCallExpInfo MakeInstanceLambda(TypeValue? objectTypeValue, ImmutableArray<TypeValue> argTypeValues, Name varName)
            => new InstanceLambdaCall(objectTypeValue, argTypeValues, varName);

        public static QsMemberCallExpInfo MakeEnumValue(TypeValue? objectTypeValue, ImmutableArray<TypeValue> argTypeValues, EnumElemInfo elemInfo)
            => new EnumValue(objectTypeValue, argTypeValues, elemInfo);

        // 왜 private 인데 base()가 먹는지;
        private QsMemberCallExpInfo(TypeValue? objectTypeValue, ImmutableArray<TypeValue> argTypeValues)
        {
            ObjectTypeValue = objectTypeValue;
            ArgTypeValues = argTypeValues;
        }
    }

    public class QsExpStmtInfo : QsSyntaxNodeInfo
    {
        public TypeValue ExpTypeValue { get; }
        public QsExpStmtInfo(TypeValue expTypeValue)
        {
            ExpTypeValue = expTypeValue;
        }
    }

    public class QsVarDeclInfo : QsSyntaxNodeInfo
    {
        public class Element
        {
            public TypeValue TypeValue { get; }
            public QsStorageInfo StorageInfo { get; }
            
            public Element(TypeValue typeValue, QsStorageInfo storageInfo)
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
            public TypeValue TestTargetTypeValue { get; }
            public string ElemName { get; }

            public TestEnum(TypeValue testTargetTypeValue, string elemName)
            {
                TestTargetTypeValue = testTargetTypeValue;
                ElemName = elemName;
            }
        }

        public class TestClass : QsIfStmtInfo
        {
            public TypeValue TestTargetTypeValue { get; }
            public TypeValue TestTypeValue { get; }

            public TestClass(TypeValue testTargetTypeValue, TypeValue testTypeValue)
            {
                TestTargetTypeValue = testTargetTypeValue;
                TestTypeValue = testTypeValue;
            }
        }

        public static TestEnum MakeTestEnum(TypeValue testTargetTypeValue, string elemName) => new TestEnum(testTargetTypeValue, elemName);
        public static TestClass MakeTestClass(TypeValue testTargetTypeValue, TypeValue testTypeValue) => new TestClass(testTargetTypeValue, testTypeValue);
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
        public TypeValue? ContTypeValue { get; }
        public QsForStmtInfo(TypeValue? contTypeValue)
        {
            ContTypeValue = contTypeValue;
        }
    }

    public class QsExpForStmtInitializerInfo : QsSyntaxNodeInfo
    {
        public TypeValue ExpTypeValue { get; }
        public QsExpForStmtInitializerInfo(TypeValue expTypeValue)
        {
            ExpTypeValue = expTypeValue;
        }
    }

    public class QsForeachStmtInfo : QsSyntaxNodeInfo
    {
        public TypeValue ObjTypeValue { get; }
        public TypeValue EnumeratorTypeValue { get; }

        public TypeValue ElemTypeValue { get; }
        public int ElemLocalIndex { get; }
        public FuncValue GetEnumeratorValue { get; }
        public FuncValue MoveNextValue { get; }
        public FuncValue GetCurrentValue { get; }        

        public QsForeachStmtInfo(TypeValue objTypeValue, TypeValue enumeratorTypeValue, TypeValue elemTypeValue, int elemLocalIndex, FuncValue getEnumeratorValue, FuncValue moveNextValue, FuncValue getCurrentValue)
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
        public FuncValue FuncValue { get; }
        public TypeValue ObjectTypeValue { get; }
        public TypeValue IndexTypeValue { get; }

        public QsIndexerExpInfo(FuncValue funcValue, TypeValue objectTypeValue, TypeValue indexTypeValue)
        {
            FuncValue = funcValue;
            ObjectTypeValue = objectTypeValue;
            IndexTypeValue = indexTypeValue;
        }
    }    

    public class QsExpStringExpElementInfo : QsSyntaxNodeInfo
    {
        public TypeValue ExpTypeValue { get; }
        public QsExpStringExpElementInfo(TypeValue expTypeValue) 
        { 
            ExpTypeValue = expTypeValue; 
        }
    }
}
