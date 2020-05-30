using QuickSC.Runtime;
using QuickSC.Syntax;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;

namespace QuickSC
{
    abstract class QsScriptFuncTemplate
    {

    }

    class QsFuncDeclTemplate : QsScriptFuncTemplate
    {
        public QsFunc Func { get; }
        public QsFuncDecl FuncDecl { get; }
        public int LocalVarCount { get; }        
    }

    class QsEnumDeclElemFuncTemplate : QsScriptFuncTemplate
    {
        public QsEnumDeclElement EnumDeclElem { get; }
    }

    class QsScriptModule : IQsModule
    {
        public string ModuleName { get; }
        ImmutableDictionary<QsFuncId, QsScriptFuncTemplate> funcTemplates;

        public QsScriptModule(string moduleName)
        {
            ModuleName = moduleName;
        }

        // FuncValue -> 
        public QsFuncInst GetFuncInst(QsFuncId funcId, ImmutableArray<QsTypeInst> typeArgs)
        {
            var template = funcTemplates[funcId];

            if (template is QsFuncDeclTemplate funcDeclTemplate)
            {
                QsTypeValue? seqElemTypeValue = null;
                if (funcDeclTemplate.FuncDecl.FuncKind == QsFuncKind.Sequence)
                    seqElemTypeValue = funcDeclTemplate.Func.RetTypeValue;

                // TODO: Instantiation 미지원
                if (typeArgs.Length != 0)
                    throw new NotImplementedException();

                return new QsScriptFuncInst(
                    seqElemTypeValue,
                    funcDeclTemplate.Func.bThisCall,
                    null,
                    ImmutableArray<QsValue>.Empty,
                    funcDeclTemplate.LocalVarCount,
                    funcDeclTemplate.FuncDecl.Body);
            }
            else if (template is QsEnumDeclElemFuncTemplate enumTemplate)
            {
                return new QsEnumElemFuncInst();
            }
        }

        public QsTypeInst GetTypeInst(QsTypeId typeId, ImmutableArray<QsTypeInst> typeArgs)
        {
            throw new NotImplementedException();
        }
    }
}
