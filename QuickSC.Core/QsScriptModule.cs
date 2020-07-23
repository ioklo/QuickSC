using QuickSC.Runtime;
using QuickSC.StaticAnalyzer;
using System;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

namespace QuickSC
{
    internal class QsScriptModule : IQsModule
    {
        private QsScriptMetadata scriptMetadata;
        private ImmutableDictionary<QsMetaItemId, QsScriptFuncTemplate> funcTemplatesById;

        public QsScriptModule(QsScriptMetadata scriptMetadata, ImmutableDictionary<QsMetaItemId, QsScriptFuncTemplate> funcTemplatesById)
        {
            this.scriptMetadata = scriptMetadata;
            this.funcTemplatesById = funcTemplatesById;
        }

        public string ModuleName => scriptMetadata.ModuleName;

        public bool GetFuncInfo(QsMetaItemId id, [NotNullWhen(true)] out QsFuncInfo? funcInfo)
        {
            return scriptMetadata.GetFuncInfo(id, out funcInfo);
        }       

        public bool GetTypeInfo(QsMetaItemId id, [NotNullWhen(true)] out QsTypeInfo? typeInfo)
        {
            return scriptMetadata.GetTypeInfo(id, out typeInfo);
        }

        public bool GetVarInfo(QsMetaItemId id, [NotNullWhen(true)] out QsVarInfo? varInfo)
        {
            return scriptMetadata.GetVarInfo(id, out varInfo);
        }

        public QsFuncInst GetFuncInst(QsDomainService domainService, QsFuncValue funcValue)
        {
            var funcTempl = funcTemplatesById[funcValue.FuncId];

            if (funcTempl is QsScriptFuncTemplate.FuncDecl funcDeclTempl)
            {
                if (funcValue.TypeArgs.Length != 0)
                    throw new NotImplementedException();

                return new QsScriptFuncInst(funcDeclTempl.SeqElemTypeValue, funcDeclTempl.bThisCall, null, ImmutableArray<QsValue>.Empty, funcDeclTempl.LocalVarCount, funcDeclTempl.Body);
            }

            throw new NotImplementedException();
        }

        public QsTypeInst GetTypeInst(QsDomainService domainService, QsTypeValue_Normal typeValue)
        {
            throw new System.NotImplementedException();
        }

        public void OnLoad(QsDomainService domainService)
        {
        }
    }
}