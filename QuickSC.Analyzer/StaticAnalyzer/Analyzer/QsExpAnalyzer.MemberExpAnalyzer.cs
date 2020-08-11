using QuickSC.Syntax;
using static QuickSC.StaticAnalyzer.QsAnalyzer.Misc;

namespace QuickSC.StaticAnalyzer
{
    partial class QsExpAnalyzer
    {
        class MemberExpAnalyzer
        {
            public struct Result
            {
                public QsMemberExpInfo MemberExpInfo { get;}
                public QsTypeValue TypeValue { get;}
                public Result(QsMemberExpInfo memberExpInfo, QsTypeValue typeValue)
                {
                    MemberExpInfo = memberExpInfo;
                    TypeValue = typeValue;
                }
            }

            QsAnalyzer analyzer;
            QsMemberExp memberExp;
            QsAnalyzer.Context context;

            public MemberExpAnalyzer(QsAnalyzer analyzer, QsMemberExp memberExp, QsAnalyzer.Context context)
            {
                this.analyzer = analyzer;
                this.memberExp = memberExp;
                this.context = context;
            }

            public Result? Analyze()
            {
                if (memberExp.Object is QsIdentifierExp objIdExp)
                {
                    var typeArgs = GetTypeValues(objIdExp.TypeArgs, context);
                    if (!context.GetIdentifierInfo(objIdExp.Value, typeArgs, out var idInfo))
                        return null;

                    if (idInfo is QsAnalyzerIdentifierInfo.Type typeIdInfo)
                        return Analyze_Type(typeIdInfo.TypeValue); 
                }
                
                if (!analyzer.AnalyzeExp(memberExp.Object, context, out var objTypeValue))
                    return null;

                return Analyze_Instance(objTypeValue);
            }
            
            private Result? Analyze_Instance(QsTypeValue objTypeValue)
            {
                if (!analyzer.CheckInstanceMember(memberExp, objTypeValue, context, out var varValue))
                    return null;

                // instance이지만 static 이라면, exp는 실행하고, static변수에서 가져온다
                var nodeInfo = IsVarStatic(varValue.VarId, context)
                    ? QsMemberExpInfo.MakeStatic(objTypeValue, varValue)
                    : QsMemberExpInfo.MakeInstance(objTypeValue, QsName.MakeText(memberExp.MemberName));

                var typeValue = context.TypeValueService.GetTypeValue(varValue);

                return new Result(nodeInfo, typeValue);
            }

            private Result? Analyze_Type(QsTypeValue objTypeValue)
            {
                if (!analyzer.CheckStaticMember(memberExp, objTypeValue, context, out var varValue))
                    return null;
                
                var nodeInfo = QsMemberExpInfo.MakeStatic(null, varValue);
                var typeValue = context.TypeValueService.GetTypeValue(varValue);
                    
                return new Result(nodeInfo, typeValue);
            }
        }
    }
}
