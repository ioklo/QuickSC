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

        // FuncValue -> 
        public QsFuncInst GetFuncInst(QsFuncId funcId, ImmutableArray<QsTypeInst> typeArgs)
        {
            if (typeArgs.Length != 0)
                throw new NotImplementedException();

            var template = funcTemplates[funcId];

            if (template is QsScriptFuncTemplate.FuncDecl fd)
            {
                return new QsScriptFuncInst(fd.SeqElemTypeValue, fd.bThisCall, null, ImmutableArray<QsValue>.Empty, fd.LocalVarCount, fd.Body);
            }

            throw new NotImplementedException();
            //else if (template is QsEnumDeclElemFuncTemplate enumTemplate)
            //{
            //    return new QsEnumElemFuncInst();
            //}
        }

        public QsTypeInst GetTypeInst(QsTypeId typeId, ImmutableArray<QsTypeInst> typeArgs)
        {
            throw new NotImplementedException();
        }
    }
}
