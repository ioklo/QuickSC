using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text;

namespace QuickSC.Runtime
{
    // 도메인: 프로그램 실행에 대한 격리 단위   
    public class QsDomainService
    {
        Dictionary<string, IQsModule> modulesByName;

        public QsDomainService()
        {
            modulesByName = new Dictionary<string, IQsModule>();
        }

        public void AddModule(IQsModule module)
        {
            modulesByName.Add(module.ModuleName, module);
        }

        public QsTypeInst GetTypeInst(QsTypeId typeId, ImmutableArray<QsTypeInst> typeArgs)
        {
            if (typeId.ModuleName == null)
            {
                throw new NotImplementedException();
            }
            else
            {
                return modulesByName[typeId.ModuleName].GetTypeInst(typeId, typeArgs);
            }
        }

        public QsFuncInst GetFuncInst(QsFuncId funcId, ImmutableArray<QsTypeInst> typeArgs)
        {
            if (funcId.ModuleName == null)
            {
                throw new NotImplementedException();
            }
            else
            {
                return modulesByName[funcId.ModuleName].GetFuncInst(funcId, typeArgs);
            }
        }

        // 로딩된 모듈에서 타입을 검색한다
    }
}
