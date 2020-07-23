using QuickSC.Syntax;

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

            QsExpAnalyzer expAnalyzer;
            QsMemberExp memberExp;
            QsAnalyzerContext context;

            public MemberExpAnalyzer(QsExpAnalyzer expAnalyzer, QsMemberExp memberExp,QsAnalyzerContext context)
            {
                this.expAnalyzer = expAnalyzer;
                this.memberExp = memberExp;
                this.context = context;
            }

            public Result? Analyze()
            {
                // PASS, INVALID, VALID
                var result = AnalyzeObjectIdExp(out var bValid);
                if (result != null) return result;
                if (!bValid) return null;

                if (!expAnalyzer.AnalyzeExp(memberExp.Object, context, out var objTypeValue))
                    return null;

                return Analyze_Instance(objTypeValue);
            }

            private Result? AnalyzeObjectIdExp(out bool bValid)
            {
                if (memberExp.Object is QsIdentifierExp objIdExp)
                {
                    var typeArgs = QsAnalyzerMisc.GetTypeValues(objIdExp.TypeArgs, context);
                    if (!context.GetIdentifierInfo(objIdExp.Value, typeArgs, out var idInfo))
                    {
                        bValid = false;
                        return null; // INVALID
                    }

                    if (idInfo is QsAnalyzerIdentifierInfo.Type typeIdInfo)
                    {
                        bValid = true;
                        return Analyze_Type(typeIdInfo.TypeValue); // VALID
                    }
                }
                
                bValid = true;
                return null; // PASS
            }

            private Result? Analyze_Instance(QsTypeValue objTypeValue)
            {
                // TODO: Func추가
                QsTypeValue_Normal? objNormalTypeValue = objTypeValue as QsTypeValue_Normal;

                if (objNormalTypeValue == null)
                {
                    context.ErrorCollector.Add(memberExp, "멤버를 가져올 수 없습니다");
                    return null;
                }

                if (0 < memberExp.MemberTypeArgs.Length)
                    context.ErrorCollector.Add(memberExp, "멤버변수에는 타입인자를 붙일 수 없습니다");

                if (!context.TypeValueService.GetMemberVarValue(objNormalTypeValue, QsName.Text(memberExp.MemberName), out var varValue))
                {
                    context.ErrorCollector.Add(memberExp, $"{memberExp.MemberName}은 {objNormalTypeValue}의 멤버가 아닙니다");
                    return null;
                }

                // instance이지만 static 이라면, exp는 실행하고, static변수에서 가져온다
                var nodeInfo = QsAnalyzerMisc.IsVarStatic(varValue.VarId, context)
                    ? QsMemberExpInfo.MakeStatic(objTypeValue, new QsVarValue(objNormalTypeValue, varValue.VarId))
                    : QsMemberExpInfo.MakeInstance(objTypeValue, QsName.Text(memberExp.MemberName));

                var typeValue = context.TypeValueService.GetTypeValue(varValue);

                return new Result(nodeInfo, typeValue);
            }

            private Result? Analyze_Type(QsTypeValue objTypeValue)
            {
                QsTypeValue_Normal? objNormalTypeValue = objTypeValue as QsTypeValue_Normal;

                if (objNormalTypeValue == null)
                {
                    context.ErrorCollector.Add(memberExp, "멤버를 가져올 수 없습니다");
                    return null;
                }

                if (!context.TypeValueService.GetMemberVarValue(objNormalTypeValue, QsName.Text(memberExp.MemberName), out var varValue))
                    return null;
                
                if (0 < memberExp.MemberTypeArgs.Length)
                {
                    context.ErrorCollector.Add(memberExp, "멤버변수에는 타입인자를 붙일 수 없습니다");
                    return null;
                }

                if (!QsAnalyzerMisc.IsVarStatic(varValue.VarId, context)) // instance인데 static을 가져오는건 괜찮다
                {
                    context.ErrorCollector.Add(memberExp, "정적 변수가 아닙니다");
                    return null;
                }

                var nodeInfo = QsMemberExpInfo.MakeStatic(null, new QsVarValue(objNormalTypeValue, varValue.VarId));
                var typeValue = context.TypeValueService.GetTypeValue(varValue);
                    
                return new Result(nodeInfo, typeValue);
            }
        }
    }
}
