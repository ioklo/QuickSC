using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using static QuickSC.StaticAnalyzer.QsAnalyzer.Misc;
using static QuickSC.StaticAnalyzer.QsAnalyzer;
using Gum.Syntax;

namespace QuickSC.StaticAnalyzer
{
    partial class QsExpAnalyzer
    {   
        abstract class AssignExpAnalyzer
        {
            QsAnalyzer analyzer;
            Context context;

            public AssignExpAnalyzer(QsAnalyzer analyzer, Context context)
            {
                this.analyzer = analyzer;
                this.context = context;
            }

            protected abstract Exp GetTargetExp();
            protected abstract QsTypeValue? AnalyzeDirect(QsTypeValue typeValue, QsStorageInfo storageInfo);
            protected abstract QsTypeValue? AnalyzeCall(
                QsTypeValue objTypeValue,
                Exp objExp,
                QsFuncValue? getter,
                QsFuncValue? setter,
                IEnumerable<(Exp Exp, QsTypeValue TypeValue)> args);

            QsTypeValue? AnalyzeAssignToIdExp(IdentifierExp idExp)
            {
                var typeArgs = GetTypeValues(idExp.TypeArgs, context);

                if (!context.GetIdentifierInfo(idExp.Value, typeArgs, null, out var idInfo))
                    return null;

                if (idInfo is IdentifierInfo.Var varIdInfo)
                    return AnalyzeDirect(varIdInfo.TypeValue, varIdInfo.StorageInfo);

                // TODO: Func
                return null;
            }

            QsTypeValue? AnalyzeAssignToMemberExp(MemberExp memberExp)
            {
                // i.m = e1
                if (memberExp.Object is IdentifierExp objIdExp)
                {
                    var typeArgs = GetTypeValues(objIdExp.TypeArgs, context);
                    if (!context.GetIdentifierInfo(objIdExp.Value, typeArgs, null, out var idInfo))
                        return null;

                    // X.m = e1
                    if (idInfo is IdentifierInfo.Type typeIdInfo)
                        return AnalyzeAssignToStaticMember(memberExp, typeIdInfo.TypeValue);
                }

                if (!analyzer.AnalyzeExp(memberExp.Object, null, context, out var objTypeValue))
                    return null;

                return AnalyzeAssignToInstanceMember(memberExp, objTypeValue);
            }

            QsTypeValue? AnalyzeAssignToStaticMember(MemberExp memberExp, QsTypeValue.Normal objNormalTypeValue)
            {
                if (!analyzer.CheckStaticMember(memberExp, objNormalTypeValue, context, out var varValue))
                    return null;

                var typeValue = context.TypeValueService.GetTypeValue(varValue);

                return AnalyzeDirect(typeValue, QsStorageInfo.MakeStaticMember(null, varValue));
            }

            QsTypeValue? AnalyzeAssignToInstanceMember(MemberExp memberExp, QsTypeValue objTypeValue)
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

            QsTypeValue? AnalyzeAssignToIndexerExp(IndexerExp indexerExp0)
            {
                if (!analyzer.AnalyzeExp(indexerExp0.Object, null, context, out var objTypeValue))
                    return null;

                if (!analyzer.AnalyzeExp(indexerExp0.Index, null, context, out var indexTypeValue))
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
                if (locExp is IdentifierExp idExp)
                {
                    outTypeValue = AnalyzeAssignToIdExp(idExp);
                }
                // eo.m = e1, eo.m++
                else if (locExp is MemberExp memberExp)
                {
                    outTypeValue = AnalyzeAssignToMemberExp(memberExp);
                }
                // eo[ei] = e1
                else if (locExp is IndexerExp indexerExp)
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