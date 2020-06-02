using QuickSC.Runtime;
using QuickSC.StaticAnalyzer;
using QuickSC.Syntax;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;

namespace QuickSC
{   
    class QsScriptModule : IQsModule
    {
        public string ModuleName { get; }
        ImmutableDictionary<QsFuncId, QsScriptFuncTemplate> funcTemplates;

        public QsScriptModule(string moduleName, ImmutableDictionary<QsFuncId, QsScriptFuncTemplate> funcTemplates)
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

        public QsTypeInst GetTypeInst(QsDomainService domainService, QsNormalTypeValue typeValue)
        {
            throw new NotImplementedException();
        }

        public QsFuncInst GetFuncInst(QsDomainService domainService, QsFuncValue funcValue)
        {
            if (funcValue.TypeArgs.Length != 0)
                throw new NotImplementedException();

            var template = funcTemplates[funcValue.FuncId];

            if (template is QsScriptFuncTemplate.FuncDecl fd)
            {
                return new QsScriptFuncInst(fd.SeqElemTypeValue, fd.bThisCall, null, ImmutableArray<QsValue>.Empty, fd.LocalVarCount, fd.Body);
            }
            //else if (template is QsEnumDeclElemFuncTemplate enumTemplate)
            //{
            //    return new QsEnumElemFuncInst();
            //}

            throw new NotImplementedException();


        }
    }
}
