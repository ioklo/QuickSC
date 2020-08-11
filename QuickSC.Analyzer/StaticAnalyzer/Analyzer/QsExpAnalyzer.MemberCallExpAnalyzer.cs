using QuickSC.Syntax;
using System;
using System.Collections.Immutable;
using static QuickSC.StaticAnalyzer.QsAnalyzer.Misc;

namespace QuickSC.StaticAnalyzer
{
    partial class QsExpAnalyzer
    {
        class MemberCallExpAnalyzer
        {
            public struct Result
            {
                public QsTypeValue.Func TypeValue { get; }
                public QsMemberCallExpInfo NodeInfo { get; }
                public ImmutableArray<QsTypeValue> ArgTypeValues { get; }

                public Result(QsTypeValue.Func typeValue, QsMemberCallExpInfo nodeInfo, ImmutableArray<QsTypeValue> argTypeValues)
                {
                    TypeValue = typeValue;
                    NodeInfo = nodeInfo;
                    ArgTypeValues = argTypeValues;
                }
            }

            QsExpAnalyzer expAnalyzer;
            QsMemberCallExp exp;
            QsAnalyzer.Context context;
            ImmutableArray<QsTypeValue> args;

            public MemberCallExpAnalyzer(QsExpAnalyzer expAnalyzer, QsMemberCallExp exp, QsAnalyzer.Context context)
            {
                this.expAnalyzer = expAnalyzer;
                this.exp = exp;
                this.context = context;
            }

            public Result? Analyze()
            {
                if (!expAnalyzer.AnalyzeExps(exp.Args, context, out args))
                    return null;

                // id인 경우는 따로 처리
                if (exp.Object is QsIdentifierExp objIdExp)
                {
                    return AnalyzeObjectIdExp(objIdExp);
                }
                else
                {
                    if (!expAnalyzer.AnalyzeExp(exp.Object, context, out var objTypeValue))
                        return null;

                    return Analyze_Instance(objTypeValue);
                }
            }

            private Result? AnalyzeObjectIdExp(QsIdentifierExp objIdExp)
            {
                var typeArgs = GetTypeValues(objIdExp.TypeArgs, context);

                if (!context.GetIdentifierInfo(objIdExp.Value, typeArgs, out var idInfo))
                    return null;

                if (idInfo is QsAnalyzerIdentifierInfo.Type typeIdInfo)
                {
                    return Analyze_Type(typeIdInfo);
                }
                else if (idInfo is QsAnalyzerIdentifierInfo.Func funcIdInfo)
                {
                    var objTypeValue = context.TypeValueService.GetTypeValue(funcIdInfo.FuncValue);
                    return Analyze_Instance(objTypeValue);
                }
                else if (idInfo is QsAnalyzerIdentifierInfo.Var varIdInfo)
                {
                    var objTypeValue = varIdInfo.TypeValue;
                    return Analyze_Instance(objTypeValue);
                }

                throw new InvalidOperationException();
            }

            private Result? Analyze_Instance(QsTypeValue objTypeValue)
            {
                var memberTypeArgs = GetTypeValues(exp.MemberTypeArgs, context);

                // 1. 함수에서 찾기.. FuncValue도 같이 주는것이 좋을 듯 하다
                if (context.TypeValueService.GetMemberFuncValue(objTypeValue, QsName.MakeText(exp.MemberName), memberTypeArgs, out var funcValue))
                {
                    // staticObject인 경우는 StaticFunc만, 아니라면 모든 함수가 가능 
                    var funcTypeValue = context.TypeValueService.GetTypeValue(funcValue);

                    var nodeInfo = IsFuncStatic(funcValue.FuncId, context)
                        ? QsMemberCallExpInfo.MakeStaticFunc(objTypeValue, args, funcValue)
                        : QsMemberCallExpInfo.MakeInstanceFunc(objTypeValue, args, funcValue);

                    return new Result(funcTypeValue, nodeInfo, args);
                }

                // 2. 변수에서 찾기
                if (memberTypeArgs.Length == 0)
                {
                    if (context.TypeValueService.GetMemberVarValue(objTypeValue, QsName.MakeText(exp.MemberName), out var varValue))
                    {
                        // TODO: as 대신 함수로 의미 부여하기, 호출 가능하면? 쿼리하는 함수로 변경
                        var varFuncTypeValue = context.TypeValueService.GetTypeValue(varValue) as QsTypeValue.Func;

                        if (varFuncTypeValue == null)
                        {
                            context.ErrorCollector.Add(exp, $"호출 가능한 타입이 아닙니다");
                            return null;
                        }

                        var nodeInfo = IsVarStatic(varValue.VarId, context)
                            ? QsMemberCallExpInfo.MakeStaticLambda(objTypeValue, args, varValue)
                            : QsMemberCallExpInfo.MakeInstanceLambda(objTypeValue, args, varValue.VarId.Name);

                        return new Result(varFuncTypeValue, nodeInfo, args);
                    }
                }

                // 변수에서 찾기 VarId도 같이 주는것이 좋을 것 같다
                context.ErrorCollector.Add(exp, $"{exp.Object}에 {exp.MemberName} 함수가 없습니다");
                return null;
            }

            private Result? Analyze_Type(QsAnalyzerIdentifierInfo.Type typeIdInfo)
            {
                var objTypeValue = typeIdInfo.TypeValue;
                var memberTypeArgs = GetTypeValues(exp.MemberTypeArgs, context);

                // 1. 함수에서 찾기
                if (context.TypeValueService.GetMemberFuncValue(objTypeValue, QsName.MakeText(exp.MemberName), memberTypeArgs, out var memberFuncValue))
                {
                    if (!IsFuncStatic(memberFuncValue.FuncId, context))
                    {
                        context.ErrorCollector.Add(exp, "정적 함수만 호출할 수 있습니다");
                        return null;
                    }

                    // staticObject인 경우는 StaticFunc만, 아니라면 모든 함수가 가능 
                    var funcTypeValue = context.TypeValueService.GetTypeValue(memberFuncValue);
                    var nodeInfo = QsMemberCallExpInfo.MakeStaticFunc(null, args, memberFuncValue);

                    return new Result(funcTypeValue, nodeInfo, args);
                }

                // 2. 변수에서 찾기
                if (memberTypeArgs.Length == 0)
                {
                    if (context.TypeValueService.GetMemberVarValue(objTypeValue, QsName.MakeText(exp.MemberName), out var varValue))
                    {
                        if (!IsVarStatic(varValue.VarId, context))
                        {
                            context.ErrorCollector.Add(exp, "정적 변수만 참조할 수 있습니다");
                            return null;
                        }

                        // TODO: as 대신 함수로 의미 부여하기, 호출 가능하면? 쿼리하는 함수로 변경
                        var varFuncTypeValue = context.TypeValueService.GetTypeValue(varValue) as QsTypeValue.Func;

                        if (varFuncTypeValue == null)
                        {
                            context.ErrorCollector.Add(exp, $"호출 가능한 타입이 아닙니다");
                            return null;
                        }

                        return new Result(varFuncTypeValue, QsMemberCallExpInfo.MakeStaticLambda(null, args, varValue), args);
                    }
                }

                // 변수에서 찾기 VarId도 같이 주는것이 좋을 것 같다
                context.ErrorCollector.Add(exp, $"{exp.Object}에 {exp.MemberName} 함수가 없습니다");
                return null;
            }
        }
    }
}
