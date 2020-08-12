using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;

namespace QuickSC
{
    public class QsTypeValueApplier
    {
        QsMetadataService metadataService;

        public QsTypeValueApplier(QsMetadataService metadataService)
        {
            this.metadataService = metadataService;
        }

        private void MakeTypeEnv_Type(QsMetaItemId typeId, QsTypeArgumentList typeArgList, Dictionary<QsTypeValue.TypeVar, QsTypeValue> typeEnv)
        {
            var typeInfo = metadataService.GetTypeInfos(typeId).SingleOrDefault();
            if (typeInfo == null)
                Debug.Assert(false);

            if (typeInfo.OuterTypeId != null)
                MakeTypeEnv_Type(typeInfo.OuterTypeId, typeArgList.Outer!, typeEnv);

            var typeParams = typeInfo.GetTypeParams();

            Debug.Assert(typeParams.Count == typeArgList.Args.Length);

            for (int i = 0; i < typeParams.Count; i++)
                typeEnv[QsTypeValue.MakeTypeVar(typeId, typeParams[i])] = typeArgList.Args[i];
        }

        // ApplyTypeEnv_Normal(Normal (Z, [[T], [U], []]), { T -> int, U -> short })
        // 
        // Normal(Z, [[int], [short], []])

        private QsTypeArgumentList ApplyTypeEnv_TypeArgumentList(QsTypeArgumentList typeArgList, Dictionary<QsTypeValue.TypeVar, QsTypeValue> typeEnv)
        {
            QsTypeArgumentList? appliedOuter = null;

            if (typeArgList.Outer != null)
                appliedOuter = ApplyTypeEnv_TypeArgumentList(typeArgList.Outer, typeEnv);

            var appliedTypeArgs = new List<QsTypeValue>(typeArgList.Args.Length);
            foreach (var typeArg in typeArgList.Args)
            {
                var appliedTypeArg = ApplyTypeEnv(typeArg, typeEnv);
                appliedTypeArgs.Add(appliedTypeArg);
            }

            return QsTypeArgumentList.Make(appliedOuter, appliedTypeArgs);
        }

        private QsTypeValue ApplyTypeEnv_Normal(QsTypeValue.Normal ntv, Dictionary<QsTypeValue.TypeVar, QsTypeValue> typeEnv)
        {
            var appliedTypeArgList = ApplyTypeEnv_TypeArgumentList(ntv.TypeArgList, typeEnv);
            return QsTypeValue.MakeNormal(ntv.TypeId, appliedTypeArgList);
        }

        // 
        private QsTypeValue.Func ApplyTypeEnv_Func(QsTypeValue.Func typeValue, Dictionary<QsTypeValue.TypeVar, QsTypeValue> typeEnv)
        {
            return QsTypeValue.MakeFunc(
                ApplyTypeEnv(typeValue.Return, typeEnv),
                ImmutableArray.CreateRange(
                    typeValue.Params,
                    paramTypeValue => ApplyTypeEnv(paramTypeValue, typeEnv)));
        }

        // T, [T -> ]
        private QsTypeValue ApplyTypeEnv_TypeVar(QsTypeValue.TypeVar typeValue, Dictionary<QsTypeValue.TypeVar, QsTypeValue> typeEnv)
        {
            if (typeEnv.TryGetValue(typeValue, out var appliedTypeValue))
                return appliedTypeValue;

            return typeValue;
        }

        private QsTypeValue ApplyTypeEnv(QsTypeValue typeValue, Dictionary<QsTypeValue.TypeVar, QsTypeValue> typeEnv)
        {
            return typeValue switch
            {
                QsTypeValue.Normal normalTypeValue => ApplyTypeEnv_Normal(normalTypeValue, typeEnv),
                QsTypeValue.Func funcTypeValue => ApplyTypeEnv_Func(funcTypeValue, typeEnv),
                QsTypeValue.TypeVar typeVarTypeValue => ApplyTypeEnv_TypeVar(typeVarTypeValue, typeEnv),
                QsTypeValue.Void vtv => vtv,
                _ => throw new NotImplementedException()
            };
        }

        // class X<T> { class Y<U> { S<T>.List<U> u; } } => ApplyTypeValue_Normal(X<int>.Y<short>, S<T>.List<U>, context) => S<int>.Dict<short>
        private QsTypeValue Apply_Normal(QsTypeValue.Normal context, QsTypeValue typeValue)
        {
            var typeEnv = new Dictionary<QsTypeValue.TypeVar, QsTypeValue>();

            MakeTypeEnv_Type(context.TypeId, context.TypeArgList, typeEnv);

            return ApplyTypeEnv(typeValue, typeEnv);
        }

        // 주어진 funcValue 컨텍스트 내에서, typeValue를 치환하기
        public QsTypeValue.Func Apply_Func(QsFuncValue context, QsTypeValue.Func typeValue)
        {
            var funcInfo = metadataService.GetFuncInfos(context.FuncId).Single();

            var typeEnv = new Dictionary<QsTypeValue.TypeVar, QsTypeValue>();
            if (funcInfo.OuterId != null)
            {
                // TODO: Outer가 꼭 TypeId이지는 않을 것 같다. FuncId일 수도
                MakeTypeEnv_Type(funcInfo.OuterId, context.TypeArgList.Outer!, typeEnv);
            }

            for (int i = 0; i < funcInfo.TypeParams.Length; i++)
                typeEnv[QsTypeValue.MakeTypeVar(funcInfo.FuncId, funcInfo.TypeParams[i])] = context.TypeArgList.Args[i];

            return ApplyTypeEnv_Func(typeValue, typeEnv);
        }

        public QsTypeValue Apply(QsTypeValue? context, QsTypeValue typeValue)
        {
            if (context is QsTypeValue.Normal context_normal)
                return Apply_Normal(context_normal, typeValue);

            return typeValue;
        }
    }
}