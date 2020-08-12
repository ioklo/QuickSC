﻿using QuickSC.Runtime;
using QuickSC.StaticAnalyzer;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

namespace QuickSC
{
    internal class QsScriptModule : IQsModule
    {
        private QsScriptMetadata scriptMetadata;
        private ImmutableDictionary<QsMetaItemId, QsScriptTemplate> templatesById;

        public QsScriptModule(QsScriptMetadata scriptMetadata, IEnumerable<QsScriptTemplate> templates)
        {
            this.scriptMetadata = scriptMetadata;
            this.templatesById = templates.ToImmutableDictionary(templ => templ.Id);
        }

        public string ModuleName => scriptMetadata.ModuleName;

        public bool GetFuncInfo(QsMetaItemId id, [NotNullWhen(true)] out QsFuncInfo? funcInfo)
        {
            return scriptMetadata.GetFuncInfo(id, out funcInfo);
        }       

        public bool GetTypeInfo(QsMetaItemId id, [NotNullWhen(true)] out IQsTypeInfo? typeInfo)
        {
            return scriptMetadata.GetTypeInfo(id, out typeInfo);
        }

        public bool GetVarInfo(QsMetaItemId id, [NotNullWhen(true)] out QsVarInfo? varInfo)
        {
            return scriptMetadata.GetVarInfo(id, out varInfo);
        }

        public QsFuncInst GetFuncInst(QsDomainService domainService, QsFuncValue funcValue)
        {
            var templ = templatesById[funcValue.FuncId];

            if (templ is QsScriptTemplate.Func funcTempl)
            {
                if (funcValue.TypeArgList.GetTotalLength() != 0)
                    throw new NotImplementedException();

                return new QsScriptFuncInst(
                    funcTempl.SeqElemTypeValue, 
                    funcTempl.bThisCall, 
                    null, ImmutableArray<QsValue>.Empty, funcTempl.LocalVarCount, funcTempl.Body);
            }

            throw new InvalidOperationException();
        }

        public QsTypeInst GetTypeInst(QsDomainService domainService, QsTypeValue.Normal typeValue)
        {
            var templ = templatesById[typeValue.TypeId];

            if (templ is QsScriptTemplate.Enum enumTempl)
            {
                return new QsEnumTypeInst(typeValue, enumTempl.DefaultElemName);
            }

            throw new InvalidOperationException();
        }

        public void OnLoad(QsDomainService domainService)
        {
        }
    }
}