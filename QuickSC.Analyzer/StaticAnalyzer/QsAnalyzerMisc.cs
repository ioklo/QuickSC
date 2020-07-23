using QuickSC.Syntax;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

namespace QuickSC.StaticAnalyzer
{
    static class QsAnalyzerMisc
    {
        public static ImmutableArray<QsTypeValue> GetTypeValues(ImmutableArray<QsTypeExp> typeExps, QsAnalyzerContext context)
        {
            return ImmutableArray.CreateRange(typeExps, typeExp => context.GetTypeValueByTypeExp(typeExp));
        }

        public static bool IsVarStatic(QsMetaItemId varId, QsAnalyzerContext context)
        {
            var varInfo = context.MetadataService.GetVarInfos(varId).Single();
            return varInfo.bStatic;
        }

        public static bool IsFuncStatic(QsMetaItemId funcId, QsAnalyzerContext context)
        {
            var funcInfo = context.MetadataService.GetFuncInfos(funcId).Single();
            return !funcInfo.bThisCall;
        }
    }
}
