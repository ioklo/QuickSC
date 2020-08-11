using QuickSC;
using QuickSC.Syntax;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection.Emit;
using System.Text;

namespace QuickSC.StaticAnalyzer
{
    // TypeExp를 TypeValue로 바꿔서 저장합니다.
    public partial class QsTypeExpEvaluator
    {
        QsTypeSkeletonCollector typeSkeletonCollector;

        public QsTypeExpEvaluator(QsTypeSkeletonCollector typeSkeletonCollector)
        {
            this.typeSkeletonCollector = typeSkeletonCollector;
        }

        bool EvaluateIdTypeExp(QsIdTypeExp exp, Context context, [NotNullWhen(returnValue: true)] out QsTypeValue? outTypeValue)
        {
            outTypeValue = null;

            if (exp.Name == "var")
            {
                if (exp.TypeArgs.Length != 0)
                {
                    context.AddError(exp, "var는 타입 인자를 가질 수 없습니다");
                    return false;
                }

                outTypeValue = QsTypeValue.MakeVar();
                context.AddTypeValue(exp, outTypeValue);
                return true;
            }
            else if (exp.Name == "void")
            {
                if (exp.TypeArgs.Length != 0)
                {
                    context.AddError(exp, "var는 타입 인자를 가질 수 없습니다");
                    return false;
                }

                outTypeValue = QsTypeValue.MakeVoid();
                context.AddTypeValue(exp, outTypeValue);
                return true;
            }

            // 1. TypeVar에서 먼저 검색
            if (context.GetTypeVar(exp.Name, out var typeVar))
            {   
                outTypeValue = typeVar;
                context.AddTypeValue(exp, typeVar);
                return true;
            }

            // TODO: 2. 현재 This Context에서 검색

            var typeArgs = new List<QsTypeValue>(exp.TypeArgs.Length);
            foreach (var typeArgExp in exp.TypeArgs)
            {
                if (!EvaluateTypeExp(typeArgExp, context, out var typeArg))
                    return false; // 그냥 진행하면 개수가 맞지 않을 것이므로

                typeArgs.Add(typeArg);
            }

            var typeArgList = QsTypeArgumentList.Make(null, typeArgs);
            var metaItemId = QsMetaItemId.Make(exp.Name, typeArgs.Count);

            // 3-1. GlobalSkeleton에서 검색
            List <QsTypeValue> candidates = new List<QsTypeValue>();
            if (context.GetSkeleton(metaItemId, out var skeleton))
            {
                // global이니까 outer는 null
                candidates.Add(QsTypeValue.MakeNormal(skeleton.TypeId, typeArgList));
            }

            // 3-2. Reference에서 검색, GlobalTypeSkeletons에 이름이 겹치지 않아야 한다.. RefMetadata들 끼리도 이름이 겹칠 수 있다
            foreach (var type in context.GetTypeInfos(metaItemId))
                candidates.Add(QsTypeValue.MakeNormal(type.TypeId, typeArgList));

            if (candidates.Count == 1)
            {
                outTypeValue = candidates[0];
                context.AddTypeValue(exp, outTypeValue);
                return true;
            }
            else if (1 < candidates.Count)
            {
                context.AddError(exp, $"이름이 같은 {exp} 타입이 여러개 입니다");
                return false;
            }
            else
            {
                context.AddError(exp, $"{exp}를 찾지 못했습니다");
                return false;
            }
        }

        
        bool EvaluateMemberTypeExp(QsMemberTypeExp exp, Context context, [NotNullWhen(returnValue: true)] out QsTypeValue? typeValue)
        {
            typeValue = null;

            if (!EvaluateTypeExp(exp.Parent, context, out var parentTypeValue))
                return false;

            var parentNTV = parentTypeValue as QsTypeValue.Normal;
            if (parentNTV == null)
            {
                context.AddError(exp.Parent, "멤버가 있는 타입이 아닙니다");
                return false;
            }

            var typeArgsBuilder = ImmutableArray.CreateBuilder<QsTypeValue>(exp.TypeArgs.Length);
            foreach (var typeArgExp in exp.TypeArgs)
            {
                if (!EvaluateTypeExp(typeArgExp, context, out var typeArg))
                    return false;

                typeArgsBuilder.Add(typeArg);
            }

            if (!GetMemberTypeValue(context, parentNTV, exp.MemberName, typeArgsBuilder.MoveToImmutable(), out typeValue))
            {
                context.AddError(exp, $"{parentTypeValue}에서 {exp.MemberName}을 찾을 수 없습니다");
                return false;
            }

            context.AddTypeValue(exp, typeValue);
            return true;
        }

        // Error를 만들지 않습니다
        private bool GetMemberTypeValue(
            Context context,
            QsTypeValue.Normal parent, 
            string memberName, 
            ImmutableArray<QsTypeValue> typeArgs, 
            [NotNullWhen(returnValue: true)] out QsTypeValue? typeValue)
        {
            typeValue = null;
            
            if (!(parent is QsTypeValue.Normal normalParent))
                return false;
            
            if (!context.GetSkeleton(normalParent.TypeId, out var parentSkeleton))
                return false;                

            if (!parentSkeleton.GetMemberTypeId(memberName, typeArgs.Length, out var childTypeId))
                return false;

            typeValue = QsTypeValue.MakeNormal(childTypeId, QsTypeArgumentList.Make(parent.TypeArgList, typeArgs));
            return true;
        }

        bool EvaluateTypeExp(QsTypeExp exp, Context context, [NotNullWhen(returnValue:true)] out QsTypeValue? typeValue)
        {
            if (exp is QsIdTypeExp idExp)
                return EvaluateIdTypeExp(idExp, context, out typeValue);

            else if (exp is QsMemberTypeExp memberExp)
                return EvaluateMemberTypeExp(memberExp, context, out typeValue);

            else 
                throw new NotImplementedException();
        }

        void EvaluateEnumDecl(QsEnumDecl enumDecl, Context context)
        {
            var typeId = context.GetTypeId(enumDecl);

            context.ExecInScope(typeId, enumDecl.TypeParams, () =>
            {
                foreach (var elem in enumDecl.Elems)
                {
                    foreach (var param in elem.Params)
                    {
                        // 성공여부와 상관없이 계속 진행한다
                        EvaluateTypeExp(param.Type, context, out var _);
                    }
                }
            });
        }

        void EvaluateFuncDecl(QsFuncDecl funcDecl, Context context)
        {
            EvaluateTypeExp(funcDecl.RetType, context, out var _);

            foreach(var param in funcDecl.Params)
                EvaluateTypeExp(param.Type, context, out var _);

            var funcId = context.GetFuncId(funcDecl);

            context.ExecInScope(funcId, funcDecl.TypeParams, () =>
            {   
                EvaluateStmt(funcDecl.Body, context);
            });
        }

        void EvaluateVarDecl(QsVarDecl varDecl, Context context)
        {
            EvaluateTypeExp(varDecl.Type, context, out var _);

            foreach (var elem in varDecl.Elems)
                if (elem.InitExp != null)
                    EvaluateExp(elem.InitExp, context);
        }

        void EvaluateStringExpElements(ImmutableArray<QsStringExpElement> elems, Context context)
        {
            foreach (var elem in elems)
            {
                switch (elem)
                {
                    case QsTextStringExpElement textElem: break;
                    case QsExpStringExpElement expElem: EvaluateExp(expElem.Exp, context); break;
                    default: throw new NotImplementedException();
                }
            }
        }

        void EvaluateTypeExps(ImmutableArray<QsTypeExp> typeExps, Context context)
        {
            foreach (var typeExp in typeExps)
                EvaluateTypeExp(typeExp, context, out var _);
        }

        void EvaluateIdExp(QsIdentifierExp idExp, Context context) 
        {
            EvaluateTypeExps(idExp.TypeArgs, context);
        }

        void EvaluateBoolLiteralExp(QsBoolLiteralExp boolExp, Context context) 
        {

        }

        void EvaluateIntLiteralExp(QsIntLiteralExp intExp, Context context) 
        { 

        }

        void EvaluateStringExp(QsStringExp stringExp, Context context)
        {
            EvaluateStringExpElements(stringExp.Elements, context);
        }

        void EvaluateUnaryOpExp(QsUnaryOpExp unaryOpExp, Context context)
        {
            EvaluateExp(unaryOpExp.Operand, context);
        }

        void EvaluateBinaryOpExp(QsBinaryOpExp binaryOpExp, Context context)
        {
            EvaluateExp(binaryOpExp.Operand0, context);
            EvaluateExp(binaryOpExp.Operand1, context);
        }

        void EvaluateCallExp(QsCallExp callExp, Context context)
        {
            EvaluateExp(callExp.Callable, context);
            EvaluateTypeExps(callExp.TypeArgs, context);

            foreach (var arg in callExp.Args)
                EvaluateExp(arg, context);
        }

        void EvaluateLambdaExp(QsLambdaExp lambdaExp, Context context) 
        {
            foreach (var param in lambdaExp.Params)
                if(param.Type != null)
                    EvaluateTypeExp(param.Type, context, out var _);

            EvaluateStmt(lambdaExp.Body, context);
        }

        void EvaluateIndexerExp(QsIndexerExp exp, Context context)
        {
            EvaluateExp(exp.Object, context);

            EvaluateExp(exp.Index, context);
        }

        void EvaluateMemberCallExp(QsMemberCallExp memberCallExp, Context context)
        {
            EvaluateExp(memberCallExp.Object, context);
            EvaluateTypeExps(memberCallExp.MemberTypeArgs, context);

            foreach (var arg in memberCallExp.Args)
                EvaluateExp(arg, context);
        }

        void EvaluateMemberExp(QsMemberExp memberExp, Context context)
        {
            EvaluateExp(memberExp.Object, context);

            EvaluateTypeExps(memberExp.MemberTypeArgs, context);
        }

        void EvaluateListExp(QsListExp listExp, Context context)
        {
            foreach (var elem in listExp.Elems)
                EvaluateExp(elem, context);
        }

        void EvaluateExp(QsExp exp, Context context)
        {
            switch(exp)
            {
                case QsIdentifierExp idExp: EvaluateIdExp(idExp, context); break;
                case QsBoolLiteralExp boolExp: EvaluateBoolLiteralExp(boolExp, context); break;
                case QsIntLiteralExp intExp: EvaluateIntLiteralExp(intExp, context); break;
                case QsStringExp stringExp: EvaluateStringExp(stringExp, context); break;
                case QsUnaryOpExp unaryOpExp: EvaluateUnaryOpExp(unaryOpExp, context); break;
                case QsBinaryOpExp binaryOpExp: EvaluateBinaryOpExp(binaryOpExp, context); break;
                case QsCallExp callExp: EvaluateCallExp(callExp, context); break;
                case QsLambdaExp lambdaExp: EvaluateLambdaExp(lambdaExp, context); break;
                case QsIndexerExp indexerExp: EvaluateIndexerExp(indexerExp, context); break;
                case QsMemberCallExp memberCallExp: EvaluateMemberCallExp(memberCallExp, context); break;
                case QsMemberExp memberExp: EvaluateMemberExp(memberExp, context); break;
                case QsListExp listExp: EvaluateListExp(listExp, context); break;
                default: throw new NotImplementedException();
            }
        }
        
        void EvaluateCommandStmt(QsCommandStmt cmdStmt, Context context)
        {
            foreach (var cmd in cmdStmt.Commands)
                EvaluateStringExpElements(cmd.Elements, context);
        }

        void EvaluateVarDeclStmt(QsVarDeclStmt varDeclStmt, Context context) 
        {
            EvaluateVarDecl(varDeclStmt.VarDecl, context);
        }

        void EvaluateIfStmt(QsIfStmt ifStmt, Context context)
        {
            EvaluateExp(ifStmt.Cond, context);

            if (ifStmt.TestType != null)
                EvaluateTypeExp(ifStmt.TestType, context, out var _);

            EvaluateStmt(ifStmt.Body, context);

            if (ifStmt.ElseBody != null)
                EvaluateStmt(ifStmt.ElseBody, context);
        }

        void EvaluateForStmtInitializer(QsForStmtInitializer initializer, Context context)
        {
            switch(initializer)
            {
                case QsExpForStmtInitializer expInit: EvaluateExp(expInit.Exp, context); break;
                case QsVarDeclForStmtInitializer varDeclInit: EvaluateVarDecl(varDeclInit.VarDecl, context); break;
                default: throw new NotImplementedException();
            }
        }

        void EvaluateForStmt(QsForStmt forStmt, Context context)
        {
            if (forStmt.Initializer != null)
                EvaluateForStmtInitializer(forStmt.Initializer, context);

            if (forStmt.CondExp != null)
                EvaluateExp(forStmt.CondExp, context);

            if (forStmt.ContinueExp != null)
                EvaluateExp(forStmt.ContinueExp, context);

            EvaluateStmt(forStmt.Body, context);
        }

        void EvaluateContinueStmt(QsContinueStmt continueStmt, Context context)
        {
        }

        void EvaluateBreakStmt(QsBreakStmt breakStmt, Context context)
        {
        }

        void EvaluateReturnStmt(QsReturnStmt returnStmt, Context context) 
        {
            if (returnStmt.Value != null)
                EvaluateExp(returnStmt.Value, context);
        }

        void EvaluateBlockStmt(QsBlockStmt blockStmt, Context context)
        {
            foreach (var stmt in blockStmt.Stmts)
                EvaluateStmt(stmt, context);
        }

        void EvaluateExpStmt(QsExpStmt expStmt, Context context)
        {
            EvaluateExp(expStmt.Exp, context);
        }

        void EvaluateTaskStmt(QsTaskStmt taskStmt, Context context)
        {
            EvaluateStmt(taskStmt.Body, context);
        }

        void EvaluateAwaitStmt(QsAwaitStmt awaitStmt, Context context)
        {
            EvaluateStmt(awaitStmt.Body, context);
        }

        void EvaluateAsyncStmt(QsAsyncStmt asyncStmt, Context context)
        {
            EvaluateStmt(asyncStmt.Body, context);
        }

        void EvaluateForeachStmt(QsForeachStmt foreachStmt, Context context) 
        {
            EvaluateTypeExp(foreachStmt.Type, context, out var _);
            EvaluateExp(foreachStmt.Obj, context);
            EvaluateStmt(foreachStmt.Body, context);
        }

        void EvaluateYieldStmt(QsYieldStmt yieldStmt, Context context)
        {
            EvaluateExp(yieldStmt.Value, context);
        }

        void EvaluateStmt(QsStmt stmt, Context context)
        {
            switch (stmt)
            {
                case QsCommandStmt cmdStmt: EvaluateCommandStmt(cmdStmt, context); break;
                case QsVarDeclStmt varDeclStmt: EvaluateVarDeclStmt(varDeclStmt, context); break;
                case QsIfStmt ifStmt: EvaluateIfStmt(ifStmt, context); break;
                case QsForStmt forStmt: EvaluateForStmt(forStmt, context); break;                        
                case QsContinueStmt continueStmt: EvaluateContinueStmt(continueStmt, context); break;
                case QsBreakStmt breakStmt: EvaluateBreakStmt(breakStmt, context); break;
                case QsReturnStmt returnStmt: EvaluateReturnStmt(returnStmt, context); break;
                case QsBlockStmt blockStmt: EvaluateBlockStmt(blockStmt, context); break;
                case QsBlankStmt blankStmt: break;
                case QsExpStmt expStmt: EvaluateExpStmt(expStmt, context); break;
                case QsTaskStmt taskStmt: EvaluateTaskStmt(taskStmt, context); break;
                case QsAwaitStmt awaitStmt: EvaluateAwaitStmt(awaitStmt, context); break;
                case QsAsyncStmt asyncStmt: EvaluateAsyncStmt(asyncStmt, context); break;
                case QsForeachStmt foreachStmt: EvaluateForeachStmt(foreachStmt, context); break;
                case QsYieldStmt yieldStmt: EvaluateYieldStmt(yieldStmt, context); break;                        
                default: throw new NotImplementedException();
            };
        }

        void EvaluateScript(QsScript script, Context context)
        {
            foreach(var elem in script.Elements)
            {
                switch(elem)
                {
                    case QsEnumDeclScriptElement enumDeclElem: EvaluateEnumDecl(enumDeclElem.EnumDecl, context); break;
                    case QsFuncDeclScriptElement funcDeclElem: EvaluateFuncDecl(funcDeclElem.FuncDecl, context);  break;
                    case QsStmtScriptElement stmtDeclElem: EvaluateStmt(stmtDeclElem.Stmt, context);  break;
                    default:
                        throw new NotImplementedException();
                }
            }
        }

        public (QsSyntaxNodeMetaItemService SyntaxNodeMetaItemService, QsTypeExpTypeValueService TypeExpTypeValueService)? 
            EvaluateScript(
            QsScript script,
            IEnumerable<IQsMetadata> metadatas,
            IQsErrorCollector errorCollector)
        {
            var collectResult = typeSkeletonCollector.CollectScript(script, errorCollector);
            if (collectResult == null)
                return null;

            var metadataService = new QsMetadataService(metadatas);

            var context = new Context(
                metadataService,
                collectResult.Value.SyntaxNodeMetaItemService,
                collectResult.Value.TypeSkeletons,
                errorCollector);

            EvaluateScript(script, context);

            if (errorCollector.HasError)
            {
                return null;
            }

            var typeExpTypeValueService = new QsTypeExpTypeValueService(context.GetTypeValuesByTypeExp());

            return (collectResult.Value.SyntaxNodeMetaItemService, typeExpTypeValueService);
        }

    }
}
