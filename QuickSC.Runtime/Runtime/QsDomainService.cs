using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Text;

namespace QuickSC.Runtime
{
    // 도메인: 프로그램 실행에 대한 격리 단위   
    public class QsDomainService
    {
        QsMetadataService metadataService;
        Dictionary<QsMetaItemId, IQsModuleTypeInfo> typeInfos;
        Dictionary<QsMetaItemId, IQsModuleFuncInfo> funcInfos;

        // 모든 모듈의 전역 변수
        public Dictionary<QsMetaItemId, QsValue> globalValues { get; }

        public QsDomainService(QsMetadataService metadataService)
        {
            // TODO: metadataService와 LoadModule사이에 연관이 없다 실수하기 쉽다
            this.metadataService = metadataService;
            
            typeInfos = new Dictionary<QsMetaItemId, IQsModuleTypeInfo>();
            funcInfos = new Dictionary<QsMetaItemId, IQsModuleFuncInfo>();
            globalValues = new Dictionary<QsMetaItemId, QsValue>();
        }

        public QsValue GetGlobalValue(QsMetaItemId varId)
        {
            return globalValues[varId];
        }

        public void AddTypeInfos(IEnumerable<IQsModuleTypeInfo> typeInfos)
        {
            foreach (var typeInfo in typeInfos)
                this.typeInfos.Add(typeInfo.TypeId, typeInfo);
        }

        public void AddFuncInfos(IEnumerable<IQsModuleFuncInfo> funcInfos)
        {
            foreach (var funcInfo in funcInfos)
                this.funcInfos.Add(funcInfo.FuncId, funcInfo);
        }

        public void LoadModule(IQsModule module)
        {
            AddTypeInfos(module.TypeInfos);
            AddFuncInfos(module.FuncInfos);

            module.OnLoad(this);
        }

        public QsFuncInst GetFuncInst(QsFuncValue funcValue)
        {
            // TODO: caching
            return funcInfos[funcValue.FuncId].GetFuncInst(this, funcValue);
        }
        
        // 실행중 TypeValue는 모두 Apply된 상태이다
        public QsTypeInst GetTypeInst(QsTypeValue typeValue)
        {
            // typeValue -> typeEnv
            // X<int>.Y<short> => Tx -> int, Ty -> short
            switch (typeValue)
            {
                case QsTypeValue_TypeVar tvtv:
                    Debug.Fail("실행중에 바인드 되지 않은 타입 인자가 나왔습니다");
                    throw new InvalidOperationException();

                case QsTypeValue_Normal ntv:                    
                    return typeInfos[ntv.TypeId].GetTypeInst(this, ntv);

                case QsTypeValue_Void vtv:
                    throw new NotImplementedException(); // TODO: void는 따로 처리

                case QsTypeValue_Func ftv:
                    throw new NotImplementedException(); // TODO: 함수는 따로 처리

                default:
                    throw new NotImplementedException();
            }
        }
        
        public bool GetBaseTypeValue(QsTypeValue_Normal ntv, out QsTypeValue_Normal? outBaseTypeValue)
        {
            if (metadataService.GetBaseTypeValue(ntv, out var baseTypeValue))
            {
                outBaseTypeValue = (QsTypeValue_Normal?)baseTypeValue;
                return true;
            }

            outBaseTypeValue = null;
            return false;
        }

        void MakeTypeEnv(QsTypeValue_Normal ntv, ImmutableArray<QsTypeValue>.Builder builder)
        {
            if (ntv.Outer != null)
            {
                if (ntv.Outer is QsTypeValue_Normal outerNTV)
                    MakeTypeEnv(outerNTV, builder);
                else
                    throw new InvalidOperationException(); // TODO: ntv.Outer를 normaltypeValue로 바꿔야 하지 않을까
            }

            foreach (var typeArg in ntv.TypeArgs)
            {
                builder.Add(typeArg);
            }
        }

        public QsTypeEnv MakeTypeEnv(QsTypeValue_Normal ntv)
        {
            var builder = ImmutableArray.CreateBuilder<QsTypeValue>();

            MakeTypeEnv(ntv, builder);

            return new QsTypeEnv(builder.ToImmutable());
        }

        public QsTypeEnv MakeTypeEnv(QsFuncValue fv)
        {
            var builder = ImmutableArray.CreateBuilder<QsTypeValue>();

            if (fv.Outer != null)
            {
                if (fv.Outer is QsTypeValue_Normal outerNTV)
                    MakeTypeEnv(outerNTV, builder);
                else
                    throw new InvalidOperationException(); // TODO: ntv.Outer를 normaltypeValue로 바꿔야 하지 않을까
            }

            foreach (var typeArg in fv.TypeArgs)
            {
                builder.Add(typeArg);
            }

            return new QsTypeEnv(builder.ToImmutable());
        }

        public void SetGlobalValue(QsMetaItemId varId, QsValue value)
        {
            Debug.Assert(!globalValues.ContainsKey(varId));
            globalValues[varId] = value;
        }
    }
}
