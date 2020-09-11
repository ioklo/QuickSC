using Gum.CompileTime;
using QuickSC.Runtime;
using QuickSC.StaticAnalyzer;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace QuickSC
{
    internal class QsScriptModule : IQsModule
    {
        private ScriptModuleInfo moduleInfo;
        private ImmutableDictionary<ModuleItemId, QsScriptTemplate> templatesById;
        private QsTypeValueApplier typeValueApplier;

        public QsScriptModule(
            ScriptModuleInfo moduleInfo, 
            Func<QsScriptModule, QsTypeValueApplier> typeValueApplierConstructor, 
            IEnumerable<QsScriptTemplate> templates)
        {
            this.moduleInfo = moduleInfo;
            this.typeValueApplier = typeValueApplierConstructor.Invoke(this);
            this.templatesById = templates.ToImmutableDictionary(templ => templ.Id);
        }

        public string ModuleName => moduleInfo.ModuleName;

        public bool GetFuncInfo(ModuleItemId id, [NotNullWhen(true)] out FuncInfo? funcInfo)
        {
            return moduleInfo.GetFuncInfo(id, out funcInfo);
        }       

        public bool GetTypeInfo(ModuleItemId id, [NotNullWhen(true)] out ITypeInfo? typeInfo)
        {
            return moduleInfo.GetTypeInfo(id, out typeInfo);
        }

        public bool GetVarInfo(ModuleItemId id, [NotNullWhen(true)] out VarInfo? varInfo)
        {
            return moduleInfo.GetVarInfo(id, out varInfo);
        }

        public QsFuncInst GetFuncInst(QsDomainService domainService, FuncValue funcValue)
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

        public QsTypeInst GetTypeInst(QsDomainService domainService, TypeValue.Normal typeValue)
        {
            var templ = templatesById[typeValue.TypeId];

            if (templ is QsScriptTemplate.Enum enumTempl)
            {
                // E<int>
                var defaultFieldInsts = enumTempl.DefaultFields.Select(field =>
                {
                    var fieldType = typeValueApplier.Apply(typeValue, field.TypeValue);
                    return (field.Name, domainService.GetTypeInst(fieldType));
                });

                return new QsEnumTypeInst(typeValue, enumTempl.DefaultElemName, defaultFieldInsts);
            }

            throw new InvalidOperationException();
        }

        public void OnLoad(QsDomainService domainService)
        {
        }
    }
}