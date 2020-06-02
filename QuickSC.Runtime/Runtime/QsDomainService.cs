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
        ImmutableDictionary<string, IQsModule> modulesByName;

        public QsDomainService(QsMetadataService metadataService, IQsRuntimeModule runtimeModule, IEnumerable<IQsModule> modules)
        {
            this.metadataService = metadataService;

            var builder = ImmutableDictionary.CreateBuilder<string, IQsModule>();
            builder.Add(runtimeModule.ModuleName, runtimeModule);
            builder.AddRange(modules.Select(module => KeyValuePair.Create(module.ModuleName, module)));
            modulesByName = builder.ToImmutable();        
        }

        public QsFuncInst GetFuncInst(QsFuncValue funcValue)
        {
            return modulesByName[funcValue.FuncId.ModuleName].GetFuncInst(this, funcValue);
        }
        
        // 실행중 TypeValue는 모두 Apply된 상태이다
        public QsTypeInst GetTypeInst(QsTypeValue typeValue)
        {
            // typeValue -> typeEnv
            // X<int>.Y<short> => Tx -> int, Ty -> short
            switch (typeValue)
            {
                case QsTypeVarTypeValue tvtv:
                    Debug.Fail("실행중에 바인드 되지 않은 타입 인자가 나왔습니다");
                    throw new InvalidOperationException();

                case QsNormalTypeValue ntv:
                    {
                        return modulesByName[ntv.TypeId.ModuleName].GetTypeInst(this, ntv);
                    }

                case QsVoidTypeValue vtv:
                    throw new NotImplementedException(); // TODO: void는 따로 처리

                case QsFuncTypeValue ftv:
                    throw new NotImplementedException(); // TODO: 함수는 따로 처리

                default:
                    throw new NotImplementedException();
            }
        }
        
        public bool GetBaseTypeValue(QsNormalTypeValue ntv, out QsNormalTypeValue? outBaseTypeValue)
        {
            if (metadataService.GetBaseTypeValue(ntv, out var baseTypeValue))
            {
                outBaseTypeValue = (QsNormalTypeValue?)baseTypeValue;
                return true;
            }

            outBaseTypeValue = null;
            return false;
        }

        void MakeTypeEnv(QsNormalTypeValue ntv, ImmutableArray<QsTypeValue>.Builder builder)
        {
            if (ntv.Outer != null)
            {
                if (ntv.Outer is QsNormalTypeValue outerNTV)
                    MakeTypeEnv(outerNTV, builder);
                else
                    throw new InvalidOperationException(); // TODO: ntv.Outer를 normaltypeValue로 바꿔야 하지 않을까
            }

            foreach (var typeArg in ntv.TypeArgs)
            {
                builder.Add(typeArg);
            }
        }

        public QsTypeEnv MakeTypeEnv(QsNormalTypeValue ntv)
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
                if (fv.Outer is QsNormalTypeValue outerNTV)
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
    }
}
