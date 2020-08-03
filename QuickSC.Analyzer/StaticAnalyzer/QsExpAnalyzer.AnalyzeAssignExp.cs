using QuickSC.Syntax;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace QuickSC.StaticAnalyzer
{
    partial class QsExpAnalyzer
    {   
        abstract class AssignExpAnalyzer
        {
            QsAnalyzer analyzer;
            QsAnalyzerContext context;

            public AssignExpAnalyzer(QsAnalyzer analyzer, QsAnalyzerContext context)
            {
                this.analyzer = analyzer;
                this.context = context;
            }

            protected abstract QsExp GetTargetExp();
            protected abstract QsTypeValue? AnalyzeDirect(QsTypeValue typeValue, QsStorageInfo storageInfo);
            protected abstract QsTypeValue? AnalyzeCall(
                QsTypeValue objTypeValue,
                QsExp objExp,
                QsFuncValue? getter,
                QsFuncValue? setter,
                IEnumerable<(QsExp Exp, QsTypeValue TypeValue)> args);

            QsTypeValue? AnalyzeAssignToIdExp(QsIdentifierExp idExp)
            {
                var typeArgs = QsAnalyzerMisc.GetTypeValues(idExp.TypeArgs, context);

                if (!context.GetIdentifierInfo(idExp.Value, typeArgs, out var idInfo))
                    return null;

                if (idInfo is QsAnalyzerIdentifierInfo.Var varIdInfo)
                    return AnalyzeDirect(varIdInfo.TypeValue, varIdInfo.StorageInfo);

                // TODO: Func
                return null;
            }

            QsTypeValue? AnalyzeAssignToMemberExp(QsMemberExp memberExp)
            {
                // i.m = e1
                if (memberExp.Object is QsIdentifierExp objIdExp)
                {
                    var typeArgs = QsAnalyzerMisc.GetTypeValues(objIdExp.TypeArgs, context);
                    if (!context.GetIdentifierInfo(objIdExp.Value, typeArgs, out var idInfo))
                        return null;

                    // X.m = e1
                    if (idInfo is QsAnalyzerIdentifierInfo.Type typeIdInfo)
                        return AnalyzeAssignToStaticMember(memberExp, typeIdInfo.TypeValue);
                }

                if (!analyzer.AnalyzeExp(memberExp.Object, context, out var objTypeValue))
                    return null;

                return AnalyzeAssignToInstanceMember(memberExp, objTypeValue);
            }

            QsTypeValue? AnalyzeAssignToStaticMember(QsMemberExp memberExp, QsTypeValue objTypeValue)
            {
                if (!analyzer.CheckStaticMember(memberExp, objTypeValue, context, out var varValue))
                    return null;

                var typeValue = context.TypeValueService.GetTypeValue(varValue);

                return AnalyzeDirect(typeValue, QsStorageInfo.MakeStaticMember(null, varValue));
            }

            QsTypeValue? AnalyzeAssignToInstanceMember(QsMemberExp memberExp, QsTypeValue objTypeValue)
            {
                if (!analyzer.CheckInstanceMember(memberExp, objTypeValue, context, out var varValue))
                    return null;

                var typeValue = context.TypeValueService.GetTypeValue(varValue);

                // instance이지만 static 이라면, exp는 실행하고, static변수에서 가져온다
                QsStorageInfo storageInfo;
                if (QsAnalyzerMisc.IsVarStatic(varValue.VarId, context))
                    storageInfo = QsStorageInfo.MakeStaticMember(memberExp.Object, varValue);
                else
                    storageInfo = QsStorageInfo.MakeInstanceMember(memberExp.Object, objTypeValue, QsName.Text(memberExp.MemberName));

                return AnalyzeDirect(typeValue, storageInfo);
            }

            QsTypeValue? AnalyzeAssignToIndexerExp(QsIndexerExp indexerExp0)
            {
                if (!analyzer.AnalyzeExp(indexerExp0.Object, context, out var objTypeValue))
                    return null;

                if (!analyzer.AnalyzeExp(indexerExp0.Index, context, out var indexTypeValue))
                    return null;

                context.TypeValueService.GetMemberFuncValue(
                    objTypeValue,
                    QsName.Special(QsSpecialName.IndexerSet),
                    ImmutableArray<QsTypeValue>.Empty,
                    out var setter);

                context.TypeValueService.GetMemberFuncValue(
                    objTypeValue,
                    QsName.Special(QsSpecialName.IndexerGet),
                    ImmutableArray<QsTypeValue>.Empty,
                    out var getter);

                return AnalyzeCall(objTypeValue, indexerExp0.Object, getter, setter, ImmutableArray.Create((indexerExp0.Index, indexTypeValue)));
            }

            public bool Analyze([NotNullWhen(returnValue: true)] out QsTypeValue? outTypeValue)
            {
                var locExp = GetTargetExp();

                // x = e1, x++
                if (locExp is QsIdentifierExp idExp)
                {
                    outTypeValue = AnalyzeAssignToIdExp(idExp);
                }
                // eo.m = e1, eo.m++
                else if (locExp is QsMemberExp memberExp)
                {
                    outTypeValue = AnalyzeAssignToMemberExp(memberExp);
                }
                // eo[ei] = e1
                else if (locExp is QsIndexerExp indexerExp)
                {
                    outTypeValue = AnalyzeAssignToIndexerExp(indexerExp);
                }
                else
                {
                    context.ErrorCollector.Add(locExp, "식별자, 멤버 변수, 멤버 프로퍼티, 인덱서 에만 대입할 수 있습니다");
                    outTypeValue = null;
                }

                return outTypeValue != null;
            }
        }

        class BinaryAssignExpAnalyzer : AssignExpAnalyzer
        {
            QsAnalyzer analyzer;
            QsAnalyzerContext context;
            QsBinaryOpExp exp;

            public BinaryAssignExpAnalyzer(QsAnalyzer analyzer, QsBinaryOpExp exp, QsAnalyzerContext context)
                : base(analyzer, context)
            {
                this.analyzer = analyzer;
                this.context = context;
                this.exp = exp;
            }

            protected override QsExp GetTargetExp()
            {
                return exp.Operand0;
            }

            protected override QsTypeValue? AnalyzeDirect(QsTypeValue typeValue0, QsStorageInfo storageInfo)
            {
                // operand1 검사
                if (!analyzer.AnalyzeExp(exp.Operand1, context, out var typeValue1))
                    return null;

                if (!analyzer.IsAssignable(typeValue0, typeValue1, context))
                {
                    context.ErrorCollector.Add(exp, $"{typeValue1}를 {typeValue0}에 대입할 수 없습니다");
                    return null;
                }

                var nodeInfo = QsBinaryOpExpAssignInfo.MakeDirect(storageInfo);

                context.AddNodeInfo(exp, nodeInfo);
                return typeValue1;
            }

            protected override QsTypeValue? AnalyzeCall(
                QsTypeValue objTypeValue, 
                QsExp objExp, 
                QsFuncValue? getter, 
                QsFuncValue? setter,
                IEnumerable<(QsExp Exp, QsTypeValue TypeValue)> args)
            {
                // setter만 쓴다
                if (setter == null)
                {
                    context.ErrorCollector.Add(objExp, "객체에 setter함수가 없습니다");
                    return null;
                }

                if (QsAnalyzerMisc.IsFuncStatic(setter.FuncId, context))
                {
                    context.ErrorCollector.Add(objExp, "객체의 setter는 static일 수 없습니다");
                    return null;
                }

                var setterTypeValue = context.TypeValueService.GetTypeValue(setter);

                if (!analyzer.AnalyzeExp(exp.Operand1, context, out var operandTypeValue1))
                    return null;

                var setterArgTypeValues = args.Select(a => a.TypeValue).Append(operandTypeValue1).ToImmutableArray();
                if (!analyzer.CheckParamTypes(objExp, setterTypeValue.Params, setterArgTypeValues, context))
                    return null;

                if (setterTypeValue.Return != QsTypeValue_Void.Instance)
                {
                    context.ErrorCollector.Add(objExp, "setter는 void를 반환해야 합니다");
                    return null;
                }

                var nodeInfo = QsBinaryOpExpAssignInfo.MakeCallSetter(
                    objTypeValue,
                    objExp,
                    setter,
                    args,
                    operandTypeValue1);

                context.AddNodeInfo(exp, nodeInfo);
                return operandTypeValue1;
            }
        }

        // 두가지 
        // X.m = e;
        // e1.m = e2;
        // 요구사항 
        // 1. NodeInfo에 Normal, CallSetter를 넣는다
        // 2. Normal중 MemberExpInfo라면 QsMemberExpInfo를 붙이고, IdExpInfo라면 QsIdentifierExpInfo를 붙인다. =? 이건 기존의 Analyzer에 넣으면 될듯
        // 3. Normal의 나머지 케이스에 대해서는 에러를 낸다
        // 4. CallSetter는 일단 Indexer일때만 
        internal bool AnalyzeAssignExp(
            QsBinaryOpExp binaryOpExp,
            QsAnalyzerContext context,
            [NotNullWhen(returnValue: true)] out QsTypeValue? outTypeValue)
        {            
            var assignAnalyzer = new BinaryAssignExpAnalyzer(analyzer, binaryOpExp, context);
            return assignAnalyzer.Analyze(out outTypeValue);
        }
    }
}