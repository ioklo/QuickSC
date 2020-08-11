using QuickSC.Syntax;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using static QuickSC.StaticAnalyzer.QsAnalyzer.Misc;

namespace QuickSC.StaticAnalyzer
{
    partial class QsExpAnalyzer
    {   
        abstract class AssignExpAnalyzer
        {
            QsAnalyzer analyzer;
            QsAnalyzer.Context context;

            public AssignExpAnalyzer(QsAnalyzer analyzer, QsAnalyzer.Context context)
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
                var typeArgs = GetTypeValues(idExp.TypeArgs, context);

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
                    var typeArgs = GetTypeValues(objIdExp.TypeArgs, context);
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
                if (IsVarStatic(varValue.VarId, context))
                    storageInfo = QsStorageInfo.MakeStaticMember((objTypeValue, memberExp.Object), varValue);
                else
                    storageInfo = QsStorageInfo.MakeInstanceMember(memberExp.Object, objTypeValue, QsName.MakeText(memberExp.MemberName));

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
                    QsSpecialNames.IndexerSet,
                    ImmutableArray<QsTypeValue>.Empty,
                    out var setter);

                context.TypeValueService.GetMemberFuncValue(
                    objTypeValue,
                    QsSpecialNames.IndexerGet,
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
    }
}