using QuickSC.Runtime;
using QuickSC.StaticAnalyzer;
using System;
using System.Collections.Immutable;

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
}
