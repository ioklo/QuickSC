using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Linq;
using System.Collections.Immutable;

namespace QuickSC
{
    public class QsTypeValueService
    {
        QsMetadataService metadataService;
        QsTypeValueApplier typeValueApplier;

        public QsTypeValueService(QsMetadataService metadataService, QsTypeValueApplier typeValueApplier)
        {
            this.metadataService = metadataService;
            this.typeValueApplier = typeValueApplier;
        }
        
        //private bool GetMemberVarInfo(QsMetaItemId typeId, QsName name, [NotNullWhen(returnValue: true)] out QsVarInfo? outVarInfo)
        //{
        //    outVarInfo = null;

        //    var typeInfo = metadataService.GetTypesById(typeId).SingleOrDefault();
        //    if (typeInfo == null)
        //        return false;

        //    if (!typeInfo.GetMemberVarId(name, out var varId))
        //        return false;

        //    outVarInfo = metadataService.GetVarsById(varId.Value).SingleOrDefault();
        //    return outVarInfo != null;
        //}

        //public bool GetMemberVarTypeValue(QsTypeValue typeValue, QsName name, [NotNullWhen(returnValue: true)] out QsTypeValue? outTypeValue)
        //{
        //    outTypeValue = null;

        //    var typeValue_normal = typeValue as QsTypeValue_Normal;
        //    if (typeValue_normal == null) return false;

        //    if (!GetMemberVarInfo(typeValue_normal.TypeId, name, out var memberVarInfo))
        //        return false;

        //    outTypeValue = typeValueApplier.Apply(typeValue_normal, memberVarInfo.TypeValue);
        //    return true;
        //}        
        
        // class X<T> { class Y<U> { Dict<T, U> x; } } 
        // GetTypeValue(X<int>.Y<short>, x) => Dict<int, short>
        public QsTypeValue GetTypeValue(QsVarValue varValue)
        {
            var varInfo = metadataService.GetVarInfos(varValue.VarId).Single();
            return typeValueApplier.Apply(varValue.Outer, varInfo.TypeValue);
        }

        // class X<T> { class Y<U> { S<T> F<V>(V v, List<U> u); } } => MakeFuncTypeValue(X<int>.Y<short>, F, context) 
        // (V, List<short>) => S<int>
        public QsTypeValue_Func GetTypeValue(QsFuncValue funcValue)
        {
            var funcInfo = metadataService.GetFuncInfos(funcValue.FuncId).Single();

            // 
            QsTypeValue retTypeValue;
            if (funcInfo.bSeqCall)
            {
                var enumerableId = new QsMetaItemId(new QsMetaItemIdElem("Enumerable", 1));
                retTypeValue = new QsTypeValue_Normal(null, enumerableId, funcInfo.RetTypeValue);
            }
            else
            {
                retTypeValue = funcInfo.RetTypeValue;
            }

            return typeValueApplier.Apply_Func(funcValue, new QsTypeValue_Func(retTypeValue, funcInfo.ParamTypeValues));
        }


        // 
        // GetFuncTypeValue_NormalTypeValue(X<int>.Y<short>, "Func", <bool>) =>   (int, short) => bool
        // 
        //private bool GetMemberFuncTypeValue_Normal(
        //    bool bStaticOnly,
        //    QsTypeValue_Normal typeValue,
        //    QsName memberFuncId,
        //    ImmutableArray<QsTypeValue> typeArgs,
        //    [NotNullWhen(returnValue: true)] out QsTypeValue_Func? funcTypeValue)
        //{
        //    funcTypeValue = null;

        //    if (!GetTypeById(typeValue.TypeId, out var type))
        //        return false;

        //    if (!type.GetMemberFuncId(memberFuncId, out var memberFunc))
        //        return false;

        //    if (!GetFuncById(memberFunc.Value.FuncId, out var func))
        //        return false;

        //    if (func.TypeParams.Length != typeArgs.Length)
        //        return false;

        //    funcTypeValue = MakeFuncTypeValue(typeValue, func, typeArgs);
        //    return true;
        //}

        //public bool GetMemberFuncTypeValue(
        //    bool bStaticOnly,
        //    QsTypeValue typeValue,
        //    QsName memberFuncId,
        //    ImmutableArray<QsTypeValue> typeArgs,
        //    [NotNullWhen(returnValue: true)] out QsTypeValue_Func? funcTypeValue)
        //{
        //    // var / typeVar / normal / func

        //    if (typeValue is QsTypeValue_Normal typeValue_normal)
        //        return GetMemberFuncTypeValue_Normal(bStaticOnly, typeValue_normal, memberFuncId, typeArgs, out funcTypeValue);

        //    throw new NotImplementedException();
        //}

        // class N<T> : B<T> => N.GetBaseType => B<T(N)>
        private bool GetBaseTypeValue_Normal(QsTypeValue_Normal typeValue, out QsTypeValue? outBaseTypeValue)
        {
            outBaseTypeValue = null;

            var typeInfo = metadataService.GetTypeInfos(typeValue.TypeId).SingleOrDefault();
            if (typeInfo == null) return false;

            var baseTypeValue = typeInfo.GetBaseTypeValue();
            if (baseTypeValue == null)
                return true; // BaseType은 null일 수 있다

            outBaseTypeValue = typeValueApplier.Apply(typeValue, baseTypeValue);
            return true;
        }

        public bool GetBaseTypeValue(QsTypeValue typeValue, out QsTypeValue? baseTypeValue)
        {
            baseTypeValue = null;

            return typeValue switch
            {
                QsTypeValue_Normal normalTypeValue => GetBaseTypeValue_Normal(normalTypeValue, out baseTypeValue),
                _ => false
            };
        }

        public bool GetMemberFuncValue(
            QsTypeValue objTypeValue, 
            QsName funcName, 
            IReadOnlyCollection<QsTypeValue> typeArgs,
            [NotNullWhen(returnValue: true)] out QsFuncValue? funcValue)
        {
            funcValue = null;

            QsTypeValue_Normal? ntv = objTypeValue as QsTypeValue_Normal;
            if (ntv == null) return false;

            var typeInfo = metadataService.GetTypeInfos(ntv.TypeId).SingleOrDefault();
            if (typeInfo == null)
                return false;

            if (!typeInfo.GetMemberFuncId(funcName, out var memberFuncId))
                return false;

            var funcInfo = metadataService.GetFuncInfos(memberFuncId.Value).SingleOrDefault();

            if (funcInfo == null)
                return false;

            // 함수는 typeArgs가 모자라도 최대한 매칭한다
            if (funcInfo.TypeParams.Length < typeArgs.Count)
                return false;

            var typeArgsBuilder = ImmutableArray.CreateBuilder<QsTypeValue>(typeArgs.Count + funcInfo.TypeParams.Length);

            foreach (var typeArg in typeArgs)
                typeArgsBuilder.Add(typeArg);

            foreach (var typeParam in funcInfo.TypeParams)
                typeArgsBuilder.Add(new QsTypeValue_TypeVar(memberFuncId.Value, typeParam));

            funcValue = new QsFuncValue(objTypeValue, memberFuncId.Value, typeArgsBuilder.MoveToImmutable());
            return true;
        }

        public bool GetMemberVarValue(
            QsTypeValue objTypeValue, 
            QsName varName,
            [NotNullWhen(returnValue: true)] out QsVarValue? outVarValue)
        {
            outVarValue = null;

            var ntv = objTypeValue as QsTypeValue_Normal;
            if (ntv == null) return false;

            var typeInfo = metadataService.GetTypeInfos(ntv.TypeId).SingleOrDefault();
            if (typeInfo == null)
                return false;

            if (!typeInfo.GetMemberVarId(varName, out var memberVarId))
                return false;

            outVarValue = new QsVarValue(objTypeValue, memberVarId.Value);
            return true;
        }

        // X<T>.Y<U>{ Dict<T, U> x; } 
        // GetVarTypeValue(X<int>.Y<short>, x) => Dict<int, short>
        public QsTypeValue GetVarTypeValue(QsVarValue varValue)
        {
            var varInfo = metadataService.GetVarInfos(varValue.VarId).Single();

            return typeValueApplier.Apply(varValue.Outer, varInfo.TypeValue);
        }
    }
}
