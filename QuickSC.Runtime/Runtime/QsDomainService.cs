using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Text;

namespace QuickSC.Runtime
{
    // 도메인: 프로그램 실행에 대한 격리 단위   
    public class QsDomainService
    {
        ImmutableDictionary<string, IQsModule> modulesByName;

        public QsDomainService(IQsRuntimeModule runtimeModule, IEnumerable<IQsModule> modules)
        {
            var builder = ImmutableDictionary.CreateBuilder<string, IQsModule>();
            builder.Add(runtimeModule.ModuleName, runtimeModule);
            builder.AddRange(modules.Select(module => KeyValuePair.Create(module.ModuleName, module)));
            modulesByName = builder.ToImmutable();
        }

        public QsTypeInst GetTypeInst(QsTypeId typeId, ImmutableArray<QsTypeInst> typeArgs)
        {
            return modulesByName[typeId.ModuleName].GetTypeInst(typeId, typeArgs);
        }

        public QsFuncInst GetFuncInst(QsFuncId funcId, ImmutableArray<QsTypeInst> typeArgs)
        {
            return modulesByName[funcId.ModuleName].GetFuncInst(funcId, typeArgs);
        }

        public QsTypeInst? GetBaseTypeInst(QsTypeInst curTypeInst)
        {
            throw new NotImplementedException();
        }

        // 로딩된 모듈에서 타입을 검색한다
    }
}
