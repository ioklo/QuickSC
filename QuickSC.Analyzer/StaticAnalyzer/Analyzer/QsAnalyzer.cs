using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net;
using System.Text;
using QuickSC.Syntax;

namespace QuickSC.StaticAnalyzer
{
    public partial class QsAnalyzer
    {   
        QsMetadataBuilder typeAndFuncBuilder;
        QsCapturer capturer;

        QsExpAnalyzer expAnalyzer;
        QsStmtAnalyzer stmtAnalyzer;        

        public QsAnalyzer(QsMetadataBuilder typeAndFuncBuilder, QsCapturer capturer)
        {
            this.typeAndFuncBuilder = typeAndFuncBuilder;

            // 내부 전용 클래스는 new를 써서 직접 만들어도 된다 (DI, 인자로 받을 필요 없이)
            this.capturer = capturer;
            this.expAnalyzer = new QsExpAnalyzer(this);
            this.stmtAnalyzer = new QsStmtAnalyzer(this);
        }
        
        internal bool AnalyzeVarDecl(QsVarDecl varDecl, Context context)
        {
            // 1. int x  // x를 추가
            // 2. int x = initExp // x 추가, initExp가 int인지 검사
            // 3. var x = initExp // initExp의 타입을 알아내고 x를 추가
            // 4. var x = 1, y = "string"; // 각각 한다

            var elemsBuilder = ImmutableArray.CreateBuilder<QsVarDeclInfo.Element>(varDecl.Elems.Length);
            var declTypeValue = context.GetTypeValueByTypeExp(varDecl.Type);

            foreach (var elem in varDecl.Elems)
            {
                if (elem.InitExp == null)
                {
                    if (declTypeValue is QsTypeValue.Var)
                    {
                        context.ErrorCollector.Add(elem, $"{elem.VarName}의 타입을 추론할 수 없습니다");
                        return false;
                    }
                    else
                    {
                        AddElement(elem.VarName, declTypeValue, context);
                    }
                }
                else
                {
                    // var 처리
                    QsTypeValue typeValue;
                    if (declTypeValue is QsTypeValue.Var)
                    {
                        if (!AnalyzeExp(elem.InitExp, null, context, out var initExpTypeValue))
                            return false;

                        typeValue = initExpTypeValue;
                    }
                    else
                    {
                        if (!AnalyzeExp(elem.InitExp, declTypeValue, context, out var initExpTypeValue))
                            return false;

                        typeValue = declTypeValue;

                        if (!IsAssignable(declTypeValue, initExpTypeValue, context))
                            context.ErrorCollector.Add(elem, $"타입 {initExpTypeValue}의 값은 타입 {varDecl.Type}의 변수 {elem.VarName}에 대입할 수 없습니다.");
                    }

                    AddElement(elem.VarName, typeValue, context);
                }
            }

            context.AddNodeInfo(varDecl, new QsVarDeclInfo(elemsBuilder.MoveToImmutable()));
            return true;

            void AddElement(string name, QsTypeValue typeValue, Context context)
            {
                // TODO: globalScope에서 public인 경우는, globalStorage로 
                if (context.IsGlobalScope())
                {
                    int varId = context.AddPrivateGlobalVarInfo(name, typeValue);
                    elemsBuilder.Add(new QsVarDeclInfo.Element(typeValue, QsStorageInfo.MakePrivateGlobal(varId)));
                }
                else
                {
                    int localVarIndex = context.AddLocalVarInfo(name, typeValue);
                    elemsBuilder.Add(new QsVarDeclInfo.Element(typeValue, QsStorageInfo.MakeLocal(localVarIndex)));
                }
            }
        }        

        public bool AnalyzeStringExpElement(QsStringExpElement elem, Context context)
        {
            bool bResult = true;

            if (elem is QsExpStringExpElement expElem)
            {
                // TODO: exp의 결과 string으로 변환 가능해야 하는 조건도 고려해야 한다
                if (AnalyzeExp(expElem.Exp, null, context, out var expTypeValue))
                {
                    context.AddNodeInfo(elem, new QsExpStringExpElementInfo(expTypeValue));
                }
                else
                {
                    bResult = false;
                }
            }

            return bResult;
        }

        public bool AnalyzeLambda(
            QsStmt body,
            ImmutableArray<QsLambdaExpParam> parameters,
            Context context,
            [NotNullWhen(returnValue: true)] out QsCaptureInfo? outCaptureInfo,
            [NotNullWhen(returnValue: true)] out QsTypeValue.Func? outFuncTypeValue,
            out int outLocalVarCount)
        {
            outCaptureInfo = null;
            outFuncTypeValue = null;
            outLocalVarCount = 0;

            // capture에 필요한 정보를 가져옵니다
            if (!capturer.Capture(parameters.Select(param => param.Name), body, out var captureResult))
            {
                context.ErrorCollector.Add(body, "변수 캡쳐에 실패했습니다");
                return false;
            }

            // 람다 함수 컨텍스트를 만든다
            var lambdaFuncId = context.MakeLabmdaFuncId();

            // 캡쳐된 variable은 새 VarId를 가져야 한다
            var funcContext = new FuncContext(lambdaFuncId, null, false);

            // 필요한 변수들을 찾는다
            var elemsBuilder = ImmutableArray.CreateBuilder<QsCaptureInfo.Element>();
            foreach (var needCapture in captureResult.NeedCaptures)
            {
                if (context.GetIdentifierInfo(needCapture.VarName, ImmutableArray<QsTypeValue>.Empty, null, out var idInfo))
                {
                    if (idInfo is IdentifierInfo.Var varIdInfo)
                    {
                        switch (varIdInfo.StorageInfo)
                        {
                            // 지역 변수라면 
                            case QsStorageInfo.Local localStorage:
                                elemsBuilder.Add(new QsCaptureInfo.Element(needCapture.Kind, localStorage));
                                funcContext.AddLocalVarInfo(needCapture.VarName, varIdInfo.TypeValue);
                                break;

                            case QsStorageInfo.ModuleGlobal moduleGlobalStorage:
                            case QsStorageInfo.PrivateGlobal privateGlobalStorage:
                                break;

                            default:
                                throw new InvalidOperationException();
                        }

                        continue;
                    }
                }

                context.ErrorCollector.Add(body, "캡쳐실패");
                return false;                
            }            

            var paramTypeValuesBuilder = ImmutableArray.CreateBuilder<QsTypeValue>(parameters.Length);
            foreach (var param in parameters)
            {
                if (param.Type == null)
                {
                    context.ErrorCollector.Add(param, "람다 인자 타입추론은 아직 지원하지 않습니다");
                    return false;
                }

                var paramTypeValue = context.GetTypeValueByTypeExp(param.Type);

                paramTypeValuesBuilder.Add(paramTypeValue);
                funcContext.AddLocalVarInfo(param.Name, paramTypeValue);
            }

            bool bResult = true;

            context.ExecInFuncScope(funcContext, () =>
            {
                bResult &= AnalyzeStmt(body, context);
            });

            outCaptureInfo = new QsCaptureInfo(false, elemsBuilder.ToImmutable());
            outFuncTypeValue = QsTypeValue.MakeFunc(
                funcContext.GetRetTypeValue() ?? QsTypeValue.MakeVoid(),
                paramTypeValuesBuilder.MoveToImmutable());
            outLocalVarCount = funcContext.GetLocalVarCount();

            return bResult;
        }

        public bool AnalyzeExp(QsExp exp, QsTypeValue? hintTypeValue, Context context, [NotNullWhen(returnValue: true)] out QsTypeValue? typeValue)
        {
            return expAnalyzer.AnalyzeExp(exp, hintTypeValue, context, out typeValue);
        }

        public bool AnalyzeStmt(QsStmt stmt, Context context)
        {
            return stmtAnalyzer.AnalyzeStmt(stmt, context);
        }
        
        public bool AnalyzeFuncDecl(QsFuncDecl funcDecl, Context context)
        {
            var funcInfo = context.GetFuncInfoByDecl(funcDecl);

            var bResult = true;
            
            var funcContext = new FuncContext(funcInfo.FuncId, funcInfo.RetTypeValue, funcInfo.bSeqCall);

            
            context.ExecInFuncScope(funcContext, () =>
            {   
                if (0 < funcDecl.TypeParams.Length || funcDecl.VariadicParamIndex != null)
                    throw new NotImplementedException();
                
                // 파라미터 순서대로 추가
                foreach (var param in funcDecl.Params)
                {
                    var paramTypeValue = context.GetTypeValueByTypeExp(param.Type);
                    context.AddLocalVarInfo(param.Name, paramTypeValue);
                }

                bResult &= AnalyzeStmt(funcDecl.Body, context);

                // TODO: Body가 실제로 리턴을 제대로 하는지 확인해야 할 필요가 있다
                context.AddTemplate(QsScriptTemplate.MakeFunc(
                    funcInfo.FuncId,
                    funcDecl.FuncKind == QsFuncKind.Sequence ? funcInfo.RetTypeValue : null,
                    funcInfo.bThisCall, context.GetLocalVarCount(), funcDecl.Body));
            });

            return bResult;
        }

        public bool AnalyzeEnumDecl(QsEnumDecl enumDecl, Context context)
        {
            var enumInfo = context.GetEnumInfoByDecl(enumDecl);
            var defaultElemInfo = enumInfo.GetDefaultElemInfo();
            var defaultFields = defaultElemInfo.FieldInfos.Select(fieldInfo => (fieldInfo.Name, fieldInfo.TypeValue));

            context.AddTemplate(QsScriptTemplate.MakeEnum(enumInfo.TypeId, enumInfo.GetDefaultElemInfo().Name, defaultFields));

            return true;
        }

        bool AnalyzeScript(QsScript script, Context context)
        {
            bool bResult = true;

            // 4. 최상위 script를 분석한다
            foreach (var elem in script.Elements)
            {
                switch (elem)
                {
                    case QsStmtScriptElement stmtElem: 
                        bResult &= AnalyzeStmt(stmtElem.Stmt, context); 
                        break;
                }
            }

            // 5. 각 func body를 분석한다 (4에서 얻게되는 글로벌 변수 정보가 필요하다)
            foreach (var elem in script.Elements)
            {
                switch (elem)
                {
                    // TODO: classDecl
                    case QsFuncDeclScriptElement funcElem: 
                        bResult &= AnalyzeFuncDecl(funcElem.FuncDecl, context);
                        break;

                    case QsEnumDeclScriptElement enumElem:
                        bResult &= AnalyzeEnumDecl(enumElem.EnumDecl, context);
                        break;
                }
            }

            context.AddNodeInfo(script, new QsScriptInfo(context.GetLocalVarCount()));

            return bResult;
        }

        public (int PrivateGlobalVarCount, 
            ImmutableDictionary<IQsSyntaxNode, QsSyntaxNodeInfo> InfosByNode,
            ImmutableArray<QsScriptTemplate> Templates,
            QsTypeValueService TypeValueService,
            QsScriptMetadata ScriptMetadata)? 

            AnalyzeScript(
            string moduleName,
            QsScript script,
            IEnumerable<IQsMetadata> metadatas,
            IQsErrorCollector errorCollector)
        {
            // 3. Type, Func만들기, MetadataBuilder
            var buildResult = typeAndFuncBuilder.BuildScript(moduleName, metadatas, script, errorCollector);
            if (buildResult == null)
                return null;

            var metadataService = new QsMetadataService(metadatas.Append(buildResult.ScriptMetadata));
            var typeValueApplier = new QsTypeValueApplier(metadataService);
            var typeValueService = new QsTypeValueService(metadataService, typeValueApplier);

            var context = new Context(
                metadataService,
                typeValueService,
                buildResult.TypeExpTypeValueService,
                buildResult.FuncInfosByDecl,
                buildResult.EnumInfosByDecl,
                errorCollector);

            bool bResult = AnalyzeScript(script, context);

            if (!bResult || errorCollector.HasError)
            {
                return null;
            }

            return (
                context.GetPrivateGlobalVarCount(),
                context.MakeInfosByNode(),
                context.GetTemplates().ToImmutableArray(), 
                typeValueService, buildResult.ScriptMetadata);
        }

        public bool IsAssignable(QsTypeValue toTypeValue, QsTypeValue fromTypeValue, Context context)
        {
            // B <- D
            // 지금은 fromType의 base들을 찾아가면서 toTypeValue와 맞는 것이 있는지 본다
            // TODO: toTypeValue가 interface라면, fromTypeValue의 interface들을 본다

            QsTypeValue? curType = fromTypeValue;
            while (curType != null)
            {
                if (EqualityComparer<QsTypeValue>.Default.Equals(toTypeValue, curType))
                    return true;

                if (!context.TypeValueService.GetBaseTypeValue(curType, out var outType))
                    return false;

                curType = outType;
            }

            return false;
        }

        public QsTypeValue GetIntTypeValue()
        {
            return QsTypeValue.MakeNormal(QsMetaItemId.Make("int"));
        }

        public QsTypeValue GetBoolTypeValue()
        {
            return QsTypeValue.MakeNormal(QsMetaItemId.Make("bool"));
        }

        public QsTypeValue GetStringTypeValue()
        {
            return QsTypeValue.MakeNormal(QsMetaItemId.Make("string")); ;
        }

        public bool CheckInstanceMember(
            QsMemberExp memberExp,
            QsTypeValue objTypeValue,
            Context context,
            [NotNullWhen(returnValue: true)] out QsVarValue? outVarValue)
        {
            outVarValue = null;

            // TODO: Func추가
            QsTypeValue.Normal? objNormalTypeValue = objTypeValue as QsTypeValue.Normal;

            if (objNormalTypeValue == null)
            {
                context.ErrorCollector.Add(memberExp, "멤버를 가져올 수 없습니다");
                return false;
            }

            if (0 < memberExp.MemberTypeArgs.Length)
                context.ErrorCollector.Add(memberExp, "멤버변수에는 타입인자를 붙일 수 없습니다");

            if (!context.TypeValueService.GetMemberVarValue(objNormalTypeValue, QsName.MakeText(memberExp.MemberName), out outVarValue))
            {
                context.ErrorCollector.Add(memberExp, $"{memberExp.MemberName}은 {objNormalTypeValue}의 멤버가 아닙니다");
                return false;
            }

            return true;
        }

        public bool CheckStaticMember(
            QsMemberExp memberExp,
            QsTypeValue.Normal objNormalTypeValue,
            Context context,
            [NotNullWhen(returnValue: true)] out QsVarValue? outVarValue)
        {
            outVarValue = null;

            if (!context.TypeValueService.GetMemberVarValue(objNormalTypeValue, QsName.MakeText(memberExp.MemberName), out outVarValue))
            {
                context.ErrorCollector.Add(memberExp, "멤버가 존재하지 않습니다");
                return false;
            }

            if (0 < memberExp.MemberTypeArgs.Length)
            {
                context.ErrorCollector.Add(memberExp, "멤버변수에는 타입인자를 붙일 수 없습니다");
                return false;
            }

            if (!Misc.IsVarStatic(outVarValue.VarId, context))
            {
                context.ErrorCollector.Add(memberExp, "정적 변수가 아닙니다");
                return false;
            }

            return true;
        }

        public bool CheckParamTypes(object objForErrorMsg, ImmutableArray<QsTypeValue> parameters, IReadOnlyList<QsTypeValue> args, Context context)
        {
            if (parameters.Length != args.Count)
            {
                context.ErrorCollector.Add(objForErrorMsg, $"함수는 인자를 {parameters.Length}개 받는데, 호출 인자는 {args.Count} 개입니다");
                return false;
            }

            for (int i = 0; i < parameters.Length; i++)
            {
                if (!IsAssignable(parameters[i], args[i], context))
                {
                    context.ErrorCollector.Add(objForErrorMsg, $"함수의 {i + 1}번 째 매개변수 타입은 {parameters[i]} 인데, 호출 인자 타입은 {args[i]} 입니다");
                    return false;
                }
            }

            return true;
        }

    }
}
