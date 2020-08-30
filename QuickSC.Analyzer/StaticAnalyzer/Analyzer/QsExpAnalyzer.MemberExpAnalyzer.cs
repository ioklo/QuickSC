using QuickSC.Syntax;
using System;
using System.Linq;
using static QuickSC.StaticAnalyzer.QsAnalyzer;
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
            Context context;

            public MemberExpAnalyzer(QsAnalyzer analyzer, QsMemberExp memberExp, Context context)
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
                    if (!context.GetIdentifierInfo(objIdExp.Value, typeArgs, null, out var idInfo))
                        return null;

                    if (idInfo is IdentifierInfo.Type typeIdInfo)
                        return Analyze_EnumOrType(typeIdInfo.TypeValue); 
                }
                
                if (!analyzer.AnalyzeExp(memberExp.Object, null, context, out var objTypeValue))
                    return null;

                return Analyze_EnumElemOrInstance(objTypeValue);
            }

            private Result? Analyze_EnumElemOrInstance(QsTypeValue objTypeValue)
            {
                // enumElem의 경우
                if (objTypeValue is QsTypeValue.EnumElem enumElem)
                {
                    var enumInfo = (IQsEnumInfo)context.MetadataService.GetTypeInfos(enumElem.EnumTypeValue.TypeId).Single();
                    if (!enumInfo.GetElemInfo(enumElem.Name, out var elemInfo))
                    {
                        context.ErrorCollector.Add(memberExp, $"{memberExp.MemberName}은 {enumInfo}의 멤버가 아닙니다");
                        return null;
                    }
                    
                    foreach(var fieldInfo in elemInfo.Value.FieldInfos)
                    {
                        if (fieldInfo.Name == memberExp.MemberName)
                        {
                            var nodeInfo = QsMemberExpInfo.MakeEnumElemField(enumElem.EnumTypeValue, memberExp.MemberName);
                            var typeValue = context.TypeValueService.Apply(enumElem.EnumTypeValue, fieldInfo.TypeValue);

                            return new Result(nodeInfo, typeValue);
                        }
                    }

                    return null;
                }

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

            private Result? Analyze_EnumOrType(QsTypeValue.Normal objNTV)
            {
                var typeInfo = context.MetadataService.GetTypeInfos(objNTV.TypeId).Single();
                if (typeInfo is IQsEnumInfo enumTypeInfo)
                {
                    if (!enumTypeInfo.GetElemInfo(memberExp.MemberName, out var elemInfo))
                        return null;
                    
                    if (elemInfo.Value.FieldInfos.Length == 0)
                    {
                        var nodeInfo = QsMemberExpInfo.MakeEnumElem(objNTV, memberExp.MemberName);
                        var typeValue = objNTV;
                         
                        return new Result(nodeInfo, typeValue);
                    }
                    else
                    {
                        // TODO: FieldInfo가 있을 경우 함수로 감싸기
                        throw new NotImplementedException();
                    }
                }

                return Analyze_Type(objNTV);
            }

            private Result? Analyze_Type(QsTypeValue.Normal objNTV)
            {
                if (!analyzer.CheckStaticMember(memberExp, objNTV, context, out var varValue))
                    return null;

                var nodeInfo = QsMemberExpInfo.MakeStatic(null, varValue);
                var typeValue = context.TypeValueService.GetTypeValue(varValue);

                return new Result(nodeInfo, typeValue);
            }
        }
    }
}
