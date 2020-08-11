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
    class QsVoidTypeInst : QsTypeInst
    {
        public static QsVoidTypeInst Instance { get; } = new QsVoidTypeInst();

        private QsVoidTypeInst() { }

        public override QsTypeValue GetTypeValue()
        {
            return QsTypeValue.MakeVoid();
        }

        public override QsValue MakeDefaultValue()
        {
            return QsVoidValue.Instance;
        }
    }

    class QsFuncTypeInst : QsTypeInst
    {
        private QsTypeValue.Func typeValue;

        public QsFuncTypeInst(QsTypeValue.Func typeValue) { this.typeValue = typeValue; }

        public override QsTypeValue GetTypeValue()
        {
            return typeValue;
        }

        public override QsValue MakeDefaultValue()
        {
            return new QsFuncInstValue();
        }
    }

    // 도메인: 프로그램 실행에 대한 격리 단위   
    public class QsDomainService
    {
        // 모든 모듈의 전역 변수
        public Dictionary<QsMetaItemId, QsValue> globalValues { get; }
        List<IQsModule> modules;

        public QsDomainService()
        {
            globalValues = new Dictionary<QsMetaItemId, QsValue>();
            modules = new List<IQsModule>();
        }

        public QsValue GetGlobalValue(QsMetaItemId varId)
        {
            return globalValues[varId];
        }
        
        public void LoadModule(IQsModule module)
        {
            modules.Add(module);
            module.OnLoad(this);
        }

        public QsFuncInst GetFuncInst(QsFuncValue funcValue)
        {
            // TODO: caching
            foreach (var module in modules)
                if (module.GetFuncInfo(funcValue.FuncId, out var _))
                    return module.GetFuncInst(this, funcValue);

            throw new InvalidOperationException();
        }
        
        // 실행중 TypeValue는 모두 Apply된 상태이다
        public QsTypeInst GetTypeInst(QsTypeValue typeValue)
        {
            // X<int>.Y<short> => Tx -> int, Ty -> short
            switch (typeValue)
            {
                case QsTypeValue.TypeVar tvtv:
                    Debug.Fail("실행중에 바인드 되지 않은 타입 인자가 나왔습니다");
                    throw new InvalidOperationException();

                case QsTypeValue.Normal ntv:
                    {
                        foreach (var module in modules)
                            if (module.GetTypeInfo(ntv.TypeId, out var _))
                                return module.GetTypeInst(this, ntv);

                        throw new InvalidOperationException();
                    }

                case QsTypeValue.Void vtv:
                    return QsVoidTypeInst.Instance;
                
                case QsTypeValue.Func ftv:
                    return new QsFuncTypeInst(ftv);

                default:
                    throw new NotImplementedException();
            }
        }
        
        
        public void InitGlobalValue(QsMetaItemId varId, QsValue value)
        {
            Debug.Assert(!globalValues.ContainsKey(varId));
            globalValues[varId] = value;
        }
    }
}
