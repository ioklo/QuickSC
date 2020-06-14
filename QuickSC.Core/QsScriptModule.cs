using QuickSC.Runtime;
using QuickSC.StaticAnalyzer;
using QuickSC.Syntax;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

namespace QuickSC
{
    class QsScriptModuleFuncInfo : IQsModuleFuncInfo
    {
        public QsMetaItemId FuncId { get; }
        QsScriptFuncTemplate.FuncDecl funcDecl;

        public QsScriptModuleFuncInfo(QsMetaItemId funcId, QsScriptFuncTemplate.FuncDecl funcDecl)
        {
            FuncId = funcId;
            this.funcDecl = funcDecl;
        }

        public QsFuncInst GetFuncInst(QsDomainService domainService, QsFuncValue funcValue)
        {
            if (funcValue.TypeArgs.Length != 0)
                throw new NotImplementedException();            

            return new QsScriptFuncInst(funcDecl.SeqElemTypeValue, funcDecl.bThisCall, null, ImmutableArray<QsValue>.Empty, funcDecl.LocalVarCount, funcDecl.Body);
        }
    }

    class QsScriptModule : IQsModule
    {
        public string ModuleName { get; }
        ImmutableDictionary<QsMetaItemId, QsScriptFuncTemplate> funcTemplates;

        public QsScriptModule(string moduleName, ImmutableDictionary<QsMetaItemId, QsScriptFuncTemplate> funcTemplates)
        {
            ModuleName = moduleName;
            this.funcTemplates = funcTemplates;
        }

        // enum E { F, S(int i); } 
        // => type E
        // => type E.F : E (baseType E)
        // => type E.S : E { int i; } (baseType E)
        // Enum의 TypeInst로 가야한다
        //static ValueTask<QsEvalResult<QsValue>> NativeConstructor(
        //    QsEnumDeclElement elem, 
        //    QsEnumElemType elemType, 
        //    QsValue thisValue, ImmutableArray<QsValue> args, QsEvalContext context)
        //{
        //    if (elem.Params.Length != args.Length)
        //        return new ValueTask<QsEvalResult<QsValue>>(QsEvalResult<QsValue>.Invalid);

        //    var values = ImmutableDictionary.CreateBuilder<string, QsValue>();
        //    for (int i = 0; i < elem.Params.Length; i++)
        //        values.Add(elem.Params[i].Name, args[i]);

        //    return new ValueTask<QsEvalResult<QsValue>>(new QsEvalResult<QsValue>(new QsEnumValue(elemType, values.ToImmutable()), context));
        //}
        
        public IEnumerable<IQsModuleTypeInfo> TypeInfos
        {
            get => Enumerable.Empty<IQsModuleTypeInfo>();
        }

        public IEnumerable<IQsModuleFuncInfo> FuncInfos
        {
            get
            {
                foreach (var (id, funcTemplate) in funcTemplates)
                {
                    if (funcTemplate is QsScriptFuncTemplate.FuncDecl funcDecl)
                        yield return new QsScriptModuleFuncInfo(id, funcDecl);
                    //else if (template is QsEnumDeclElemFuncTemplate enumTemplate)
                    //{
                    //    return new QsEnumElemFuncInst();
                    //}
                }
            }
        }
    }
}
