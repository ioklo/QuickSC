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

        private void MakeTypeEnv_Normal(QsTypeValue_Normal typeValue, Dictionary<QsTypeValue_TypeVar, QsTypeValue> typeEnv)
        {
            if (typeValue.Outer != null)
                MakeTypeEnv(typeValue.Outer, typeEnv);

            var typeInfo = metadataService.GetTypeInfos(typeValue.TypeId).SingleOrDefault();
            if (typeInfo == null)
                Debug.Assert(false);

            var typeParams = typeInfo.GetTypeParams();

            Debug.Assert(typeParams.Length == typeValue.TypeArgs.Length);

            for (int i = 0; i < typeParams.Length; i++)
                typeEnv[new QsTypeValue_TypeVar(typeValue.TypeId, typeParams[i])] = typeValue.TypeArgs[i];
        }

        private void MakeTypeEnv(QsTypeValue typeValue, Dictionary<QsTypeValue_TypeVar, QsTypeValue> typeEnv)
        {
            switch (typeValue)
            {
                case QsTypeValue_Normal normalTypeValue: MakeTypeEnv_Normal(normalTypeValue, typeEnv); return;
                default: throw new InvalidOperationException();
            }
        }

        private QsTypeValue ApplyTypeEnv_Normal(QsTypeValue_Normal typeValue, Dictionary<QsTypeValue_TypeVar, QsTypeValue> typeEnv)
        {
            QsTypeValue? appliedOuter = (typeValue.Outer != null)
                ? ApplyTypeEnv(typeValue.Outer, typeEnv)
                : null;

            var appliedTypeArgs = new List<QsTypeValue>(typeValue.TypeArgs.Length);
            foreach (var typeArg in typeValue.TypeArgs)
            {
                var appliedTypeArg = ApplyTypeEnv(typeArg, typeEnv);
                appliedTypeArgs.Add(appliedTypeArg);
            }

            return new QsTypeValue_Normal(appliedOuter, typeValue.TypeId, appliedTypeArgs);
        }

        // 
        private QsTypeValue_Func ApplyTypeEnv_Func(QsTypeValue_Func typeValue, Dictionary<QsTypeValue_TypeVar, QsTypeValue> typeEnv)
        {
            return new QsTypeValue_Func(
                ApplyTypeEnv(typeValue.Return, typeEnv),
                ImmutableArray.CreateRange(
                    typeValue.Params,
                    paramTypeValue => ApplyTypeEnv(paramTypeValue, typeEnv)));
        }

        // T, [T -> ]
        private QsTypeValue ApplyTypeEnv_TypeVar(QsTypeValue_TypeVar typeValue, Dictionary<QsTypeValue_TypeVar, QsTypeValue> typeEnv)
        {
            if (typeEnv.TryGetValue(typeValue, out var appliedTypeValue))
                return appliedTypeValue;

            return typeValue;
        }

        private QsTypeValue ApplyTypeEnv(QsTypeValue typeValue, Dictionary<QsTypeValue_TypeVar, QsTypeValue> typeEnv)
        {
            return typeValue switch
            {
                QsTypeValue_Normal normalTypeValue => ApplyTypeEnv_Normal(normalTypeValue, typeEnv),
                QsTypeValue_Func funcTypeValue => ApplyTypeEnv_Func(funcTypeValue, typeEnv),
                QsTypeValue_TypeVar typeVarTypeValue => ApplyTypeEnv_TypeVar(typeVarTypeValue, typeEnv),
                QsTypeValue_Void vtv => vtv,
                _ => throw new NotImplementedException()
            };
        }

        // class X<T> { class Y<U> { S<T>.List<U> u; } } => ApplyTypeValue_Normal(X<int>.Y<short>, S<T>.List<U>, context) => S<int>.Dict<short>
        private QsTypeValue Apply_Normal(QsTypeValue_Normal context, QsTypeValue typeValue)
        {
            var typeEnv = new Dictionary<QsTypeValue_TypeVar, QsTypeValue>();
            if (context != null)
                MakeTypeEnv(context, typeEnv);

            return ApplyTypeEnv(typeValue, typeEnv);
        }

        // 주어진 funcValue 컨텍스트 내에서, typeValue를 치환하기
        public QsTypeValue_Func Apply_Func(QsFuncValue context, QsTypeValue_Func typeValue)
        {
            var funcInfo = metadataService.GetFuncInfos(context.FuncId).Single();

            var typeEnv = new Dictionary<QsTypeValue_TypeVar, QsTypeValue>();

            if (context.Outer != null)
                MakeTypeEnv(context.Outer, typeEnv);

            for (int i = 0; i < funcInfo.TypeParams.Length; i++)
                typeEnv[new QsTypeValue_TypeVar(funcInfo.FuncId, funcInfo.TypeParams[i])] = context.TypeArgs[i];

            return ApplyTypeEnv_Func(typeValue, typeEnv);
        }

        public QsTypeValue Apply(QsTypeValue? context, QsTypeValue typeValue)
        {
            if (context is QsTypeValue_Normal context_normal)
                return Apply_Normal(context_normal, typeValue);

            return typeValue;
        }
    }
}