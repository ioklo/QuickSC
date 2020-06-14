using QuickSC.Syntax;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;

namespace QuickSC
{
    // 잘못만듦, QsTypeValue는 직접 new로 생성하세요
    //public class QsTypeValueFactory
    //{
    //    Dictionary<string, QsType> types;

    //    static QsType MakeEmptyType(QsMetaItemId typeId)
    //    {
    //        return new QsDefaultType(typeId, null, ImmutableArray<string>.Empty, thisTypeValue => new QsDefaultTypeData(
    //            null,
    //            ImmutableDictionary<string, QsType>.Empty,
    //            ImmutableDictionary<QsMemberFuncId, QsFuncType>.Empty,
    //            ImmutableDictionary<string, QsTypeValue>.Empty));
    //    }

    //    QsType MakeListType(QsMetaItemId typeId, QsTypeValue voidTypeValue, QsTypeValue intTypeValue)
    //    {
    //        return new QsDefaultType(typeId, null, ImmutableArray.Create("T"), thisTypeValue =>
    //        {
    //            var funcTypes = ImmutableDictionary.CreateBuilder<QsMemberFuncId, QsFuncType>();                
    //            var elemTypeValue = thisTypeValue.TypeArgs[0]; // T
    //            var ienumeratorTypeValue = GetTypeValue("IEnumerator", elemTypeValue);

    //            // void List<T>.Add(T item)
    //            funcTypes.Add(new QsMemberFuncId("Add"),
    //                new QsFuncType(true, ImmutableArray<string>.Empty, voidTypeValue, elemTypeValue));

    //            // void List<T>.RemoveAt(int index)
    //            funcTypes.Add(new QsMemberFuncId("RemoveAt"), 
    //                new QsFuncType(true, ImmutableArray<string>.Empty, voidTypeValue, ImmutableArray.Create<QsTypeValue>(intTypeValue)));

    //            // IEnumerator<T> List<T>.GetEnumerator();
    //            funcTypes.Add(new QsMemberFuncId("GetEnumerator"),
    //                new QsFuncType(true, ImmutableArray<string>.Empty, ienumeratorTypeValue!));

    //            return new QsDefaultTypeData(
    //                null, // no base
    //                ImmutableDictionary<string, QsType>.Empty,
    //                funcTypes.ToImmutable(),
    //                ImmutableDictionary<string, QsTypeValue>.Empty);
    //        });   
    //    }

    //    QsType MakeIEnumeratorType(QsTypeValue boolTypeValue)
    //    {
    //        return new QsDefaultType(null, ImmutableArray.Create("T"), thisTypeValue =>
    //        {
    //            var funcTypes = ImmutableDictionary.CreateBuilder<QsMemberFuncId, QsFuncType>();
    //            var typeArgs = thisTypeValue.GetTypeArgs();
    //            var elemTypeValue = typeArgs[0];

    //            // bool IEnumerator<T>.MoveNext()
    //            funcTypes.Add(new QsMemberFuncId("MoveNext"),
    //                new QsFuncType(true, ImmutableArray<string>.Empty, boolTypeValue));

    //            // T GetCurrent()
    //            funcTypes.Add(new QsMemberFuncId("GetCurrent"),
    //                new QsFuncType(true, ImmutableArray<string>.Empty, elemTypeValue));
                
    //            return new QsDefaultTypeData(
    //                null, // no base
    //                ImmutableDictionary<string, QsType>.Empty,
    //                funcTypes.ToImmutable(),
    //                ImmutableDictionary<string, QsTypeValue>.Empty);
    //        });
    //    }

    //    public QsTypeValueFactory(ImmutableDictionary<string, QsTypeSkeleton> skeletons)
    //    {
    //        types = new Dictionary<string, QsType>();
            
    //        // 임시
    //        types.Add("void", MakeEmptyType(skeletons["void"].TypeId));
    //        types.Add("int", MakeEmptyType(skeletons["int"].TypeId));
    //        types.Add("bool", MakeEmptyType(skeletons["bool"].TypeId));
    //        types.Add("string", MakeEmptyType(skeletons["string"].TypeId));

    //        var boolTypeValue = new QsNormalTypeValue(null, skeletons["bool"].TypeId);
    //        var ienumeratorType = MakeIEnumeratorType(boolTypeValue!);
    //        types.Add("IEnumerator", ienumeratorType);

    //        var voidTypeValue = new QsNormalTypeValue(null, skeletons["void"].TypeId);
    //        var intTypeValue = new QsNormalTypeValue(null, skeletons["int"].TypeId);
    //        types.Add("List", MakeListType(skeletons["List"].TypeId, voidTypeValue, intTypeValue));
    //    }

    //    public QsTypeValue? GetTypeValue(string name, params QsTypeValue[] typeArgs)
    //    {
    //        if (name == "var")
    //            return QsVarTypeValue.Instance;

    //        if (types.TryGetValue(name, out var type))
    //            return new QsNormalTypeValue(null, type, typeArgs);

    //        return null;
    //    }

    //    public static QsType CreateEnumElemType(QsTypeValue baseTypeValue)
    //    {
    //        return new QsDefaultType(baseTypeValue, ImmutableArray<string>.Empty, thisTypeValue =>
    //            new QsDefaultTypeData(                    
    //                baseTypeValue, // enum E<T>{ First } => E<T>.First : E<T>
    //                ImmutableDictionary<string, QsType>.Empty,
    //                ImmutableDictionary<QsMemberFuncId, QsFuncType>.Empty,
    //                ImmutableDictionary<string, QsTypeValue>.Empty));
    //    }
        
    //    public static QsType MakeEnumType(QsTypeExpEvaluator typeExpEvaluator, QsTypeEvalContext context, QsEnumDecl enumDecl, QsTypeValue? outer)
    //    {


    //        return new QsDefaultType(outer, enumDecl.TypeParams, thisTypeValue =>
    //        {
    //            var typesBuilder = ImmutableDictionary.CreateBuilder<string, QsType>();
    //            var funcTypesBuilder = ImmutableDictionary.CreateBuilder<QsMemberFuncId, QsFuncType>();
    //            var varTypeValuesBuilder = ImmutableDictionary.CreateBuilder<string, QsTypeValue>();

    //            foreach (var elem in enumDecl.Elems)
    //            {
    //                var memberType = CreateEnumElemType(thisTypeValue);
    //                typesBuilder.Add(elem.Name, memberType);

    //                if (0 < elem.Params.Length)
    //                {
    //                    var argTypes = ImmutableArray.CreateBuilder<QsTypeValue>(elem.Params.Length);
    //                    foreach(var param in elem.Params)
    //                    {
    //                        var typeValue = typeExpEvaluator.Evaluate(param.Type, context);
    //                        argTypes.Add(typeValue);
    //                    }

    //                    var funcType = new QsFuncType(false, ImmutableArray<string>.Empty, thisTypeValue, argTypes.MoveToImmutable());

    //                    funcTypesBuilder.Add(new QsMemberFuncId(elem.Name), funcType);
    //                }
    //                else
    //                {
    //                    // E.First
    //                    var varTypeValue = new QsNormalTypeValue(thisTypeValue, memberType);
    //                    varTypeValuesBuilder.Add(elem.Name, varTypeValue);
    //                }
    //            }

    //            return new QsDefaultTypeData(
    //                null,
    //                typesBuilder.ToImmutable(),
    //                funcTypesBuilder.ToImmutable(),
    //                varTypeValuesBuilder.ToImmutable());
    //        });

    //        // typeInst에서 할 것
    //        //foreach (var elem in enumDecl.Elems)
    //        //{
    //        //    var memberType = new QsEnumElemType(this);
    //        //    types.Add(elem.Name, memberType);

    //        //    if (0 < elem.Params.Length)
    //        //    {
    //        //        var callable = new QsNativeCallable((thisValue, args, context) => NativeConstructor(elem, memberType, thisValue, args, context));
    //        //        memberFuncs.Add(elem.Name, callable);
    //        //    }
    //        //    else
    //        //    {
    //        //        // 이건 TypeInst에서 작성해야 할 것
    //        //        // var value = new QsEnumValue(memberType, ImmutableDictionary<string, QsValue>.Empty);
    //        //        // memberValues.Add(elem.Name, value);
    //        //    }
    //        //
    //    }

    //    public void AddGlobalEnum(QsEnumDecl enumDecl)
    //    {
    //        var enumType = MakeEnumType(enumDecl, null);

    //    }
    //}
}
