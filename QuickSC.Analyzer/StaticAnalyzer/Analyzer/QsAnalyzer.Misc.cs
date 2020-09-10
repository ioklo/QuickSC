using Gum.Syntax;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

namespace QuickSC.StaticAnalyzer
{
    public partial class QsAnalyzer
    {
        public static class Misc
        {
            public static ImmutableArray<QsTypeValue> GetTypeValues(ImmutableArray<TypeExp> typeExps, Context context)
            {
                return ImmutableArray.CreateRange(typeExps, typeExp => context.GetTypeValueByTypeExp(typeExp));
            }

            public static bool IsVarStatic(QsMetaItemId varId, Context context)
            {
                var varInfo = context.MetadataService.GetVarInfos(varId).Single();
                return varInfo.bStatic;
            }

            public static bool IsFuncStatic(QsMetaItemId funcId, Context context)
            {
                var funcInfo = context.MetadataService.GetFuncInfos(funcId).Single();
                return !funcInfo.bThisCall;
            }
        }
    }
}
