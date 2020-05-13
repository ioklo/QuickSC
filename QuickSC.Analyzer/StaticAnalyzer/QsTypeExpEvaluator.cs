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
    public class QsTypeEvalContext
    {
        public ImmutableDictionary<QsTypeIdLocation, QsTypeId> TypeIdsByLocation { get; }
        public ImmutableDictionary<QsFuncIdLocation, QsFuncId> FuncIdsByLocation { get; }
        public ImmutableDictionary<QsTypeId, QsTypeSkeleton> TypeSkeletons { get; }
        public ImmutableDictionary<(string Name, int TypeParamCount), QsTypeSkeleton> GlobalTypeSkeletons { get; }

        public Dictionary<QsTypeExp, QsTypeValue> TypeValuesByTypeExp { get; }
        public ImmutableDictionary<string, QsTypeValue> TypeEnv { get; set; }
        public List<(object obj, string message)> Errors { get; }

        public QsTypeEvalContext(
            ImmutableDictionary<QsTypeIdLocation, QsTypeId> typeIdsByLocation,
            ImmutableDictionary<QsFuncIdLocation, QsFuncId> funcIdsByLocation,
            ImmutableDictionary<QsTypeId, QsTypeSkeleton> typeSkeletons,
            ImmutableDictionary<(string Name, int TypeParamCount), QsTypeSkeleton> globalTypeSkeletons)
        {
            TypeIdsByLocation = typeIdsByLocation;
            FuncIdsByLocation = funcIdsByLocation;
            TypeSkeletons = typeSkeletons;
            GlobalTypeSkeletons = globalTypeSkeletons;
            TypeValuesByTypeExp = new Dictionary<QsTypeExp, QsTypeValue>(QsReferenceComparer<QsTypeExp>.Instance);
            TypeEnv = ImmutableDictionary<string, QsTypeValue>.Empty;
            Errors = new List<(object obj, string message)>();
        }

        public void UpdateTypeVar(string name, QsTypeValue typeValue)
        {
            TypeEnv = TypeEnv.SetItem(name, typeValue);
        }
    }

    // TypeExp를 TypeValue로 바꿔서 저장합니다.
    public class QsTypeExpEvaluator
    {
        public QsTypeExpEvaluator()
        {
        }

        bool EvaluateIdTypeExp(QsIdTypeExp exp, QsTypeEvalContext context, [NotNullWhen(returnValue: true)] out QsTypeValue? typeValue)
        {
            typeValue = null;

            if (exp.Name == "var")
            {
                if( exp.TypeArgs.Length != 0)
                {
                    context.Errors.Add((exp, "var는 타입 인자를 가질 수 없습니다"));
                    return false;
                }

                typeValue = QsVarTypeValue.Instance;
                context.TypeValuesByTypeExp.Add(exp, typeValue);
                return true;
            }

            // 1. TypeVar에서 먼저 검색
            if (context.TypeEnv.TryGetValue(exp.Name, out typeValue))
            {
                context.TypeValuesByTypeExp.Add(exp, typeValue);
                return true;
            }

            // TODO: 2. 현재 This Context에서 검색

            // 3. GlobalSkeleton에서 검색
            if (context.GlobalTypeSkeletons.TryGetValue((exp.Name, exp.TypeArgs.Length), out var skeleton))
            {
                var typeArgsBuilder = ImmutableArray.CreateBuilder<QsTypeValue>(exp.TypeArgs.Length);
                foreach(var typeArgExp in exp.TypeArgs)
                {
                    if (!EvaluateTypeExp(typeArgExp, context, out var typeArg))
                        return false; // 그냥 진행하면 개수가 맞지 않을 것이므로

                    typeArgsBuilder.Add(typeArg);
                }

                // global이니까 
                typeValue = new QsNormalTypeValue(null, skeleton.TypeId, typeArgsBuilder.MoveToImmutable());
                context.TypeValuesByTypeExp.Add(exp, typeValue);
                return true;
            }

            context.Errors.Add((exp, $"{exp}를 찾지 못했습니다"));
            return false;
        }

        bool EvaluateMemberTypeExp(QsMemberTypeExp exp, QsTypeEvalContext context, [NotNullWhen(returnValue: true)] out QsTypeValue? typeValue)
        {
            typeValue = null;

            if (!EvaluateTypeExp(exp.Parent, context, out var parentTypeValue))
                return false;

            var typeArgsBuilder = ImmutableArray.CreateBuilder<QsTypeValue>(exp.TypeArgs.Length);
            foreach (var typeArgExp in exp.TypeArgs)
            {
                if (!EvaluateTypeExp(typeArgExp, context, out var typeArg))
                    return false;

                typeArgsBuilder.Add(typeArg);
            }

            if (!GetMemberTypeValue(context, parentTypeValue, exp.MemberName, typeArgsBuilder.MoveToImmutable(), out typeValue))
            {
                context.Errors.Add((exp, $"{parentTypeValue}에서 {exp.MemberName}을 찾을 수 없습니다"));
                return false;
            }

            context.TypeValuesByTypeExp.Add(exp, typeValue);
            return true;
        }

        // Error를 만들지 않습니다
        private bool GetMemberTypeValue(
            QsTypeEvalContext context, 
            QsTypeValue parent, 
            string memberName, 
            ImmutableArray<QsTypeValue> typeArgs, 
            [NotNullWhen(returnValue: true)] out QsTypeValue? typeValue)
        {
            typeValue = null;

            QsTypeSkeleton childSkeleton;
            if (!(parent is QsNormalTypeValue normalParent))
                return false;
            
            if (!context.TypeSkeletons.TryGetValue(normalParent.TypeId, out var parentSkeleton))
                return false;                

            if (!parentSkeleton.MemberSkeletons.TryGetValue((memberName, typeArgs.Length), out childSkeleton))
                return false;
        
            typeValue = new QsNormalTypeValue(parent, childSkeleton.TypeId, typeArgs);
            return true;
        }

        bool EvaluateTypeExp(QsTypeExp exp, QsTypeEvalContext context, [NotNullWhen(returnValue:true)] out QsTypeValue? typeValue)
        {
            return exp switch
            {
                QsIdTypeExp idExp => EvaluateIdTypeExp(idExp, context, out typeValue),
                QsMemberTypeExp memberExp => EvaluateMemberTypeExp(memberExp, context, out typeValue),
                _ => throw new NotImplementedException()
            };
        }

        void EvaluateEnumDecl(QsEnumDecl enumDecl, QsTypeEvalContext context)
        {
            var prevTypeEnv = context.TypeEnv;

            var typeId = context.TypeIdsByLocation[QsTypeIdLocation.Make(enumDecl)];
            foreach (var typeParam in enumDecl.TypeParams)
            {
                context.UpdateTypeVar(typeParam, new QsTypeVarTypeValue(typeId, typeParam));
            }

            // 
            foreach(var elem in enumDecl.Elems)
            {
                foreach(var param in elem.Params)
                {
                    // 성공여부와 상관없이 계속 진행한다
                    EvaluateTypeExp(param.Type, context, out var _);
                }
            }

            context.TypeEnv = prevTypeEnv;
        }

        void EvaluateFuncDecl(QsFuncDecl funcDecl, QsTypeEvalContext context)
        {
            EvaluateTypeExp(funcDecl.RetType, context, out var _);

            foreach(var param in funcDecl.Params)
                EvaluateTypeExp(param.Type, context, out var _);

            var prevTypeEnv = context.TypeEnv;

            var funcId = context.FuncIdsByLocation[QsFuncIdLocation.Make(funcDecl)];
            foreach (var param in funcDecl.TypeParams)
                context.UpdateTypeVar(param, new QsTypeVarTypeValue(funcId, param));

            EvaluateStmt(funcDecl.Body, context);

            context.TypeEnv = prevTypeEnv;
        }

        void EvaluateVarDecl(QsVarDecl varDecl, QsTypeEvalContext context)
        {
            EvaluateTypeExp(varDecl.Type, context, out var _);

            foreach (var elem in varDecl.Elements)
                if (elem.InitExp != null)
                    EvaluateExp(elem.InitExp, context);
        }

        void EvaluateStringExpElements(ImmutableArray<QsStringExpElement> elems, QsTypeEvalContext context)
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

        void EvaluateTypeExps(ImmutableArray<QsTypeExp> typeExps, QsTypeEvalContext context)
        {
            foreach (var typeExp in typeExps)
                EvaluateTypeExp(typeExp, context, out var _);
        }

        void EvaluateIdExp(QsIdentifierExp idExp, QsTypeEvalContext context) 
        {
            EvaluateTypeExps(idExp.TypeArgs, context);
        }

        void EvaluateBoolLiteralExp(QsBoolLiteralExp boolExp, QsTypeEvalContext context) 
        {

        }

        void EvaluateIntLiteralExp(QsIntLiteralExp intExp, QsTypeEvalContext context) 
        { 

        }

        void EvaluateStringExp(QsStringExp stringExp, QsTypeEvalContext context)
        {
            EvaluateStringExpElements(stringExp.Elements, context);
        }

        void EvaluateUnaryOpExp(QsUnaryOpExp unaryOpExp, QsTypeEvalContext context)
        {
            EvaluateExp(unaryOpExp.Operand, context);
        }

        void EvaluateBinaryOpExp(QsBinaryOpExp binaryOpExp, QsTypeEvalContext context)
        {
            EvaluateExp(binaryOpExp.Operand0, context);
            EvaluateExp(binaryOpExp.Operand1, context);
        }

        void EvaluateCallExp(QsCallExp callExp, QsTypeEvalContext context)
        {
            EvaluateExp(callExp.Callable, context);
            EvaluateTypeExps(callExp.TypeArgs, context);

            foreach (var arg in callExp.Args)
                EvaluateExp(arg, context);
        }

        void EvaluateLambdaExp(QsLambdaExp lambdaExp, QsTypeEvalContext context) 
        {
            foreach (var param in lambdaExp.Params)
                if(param.Type != null)
                    EvaluateTypeExp(param.Type, context, out var _);

            EvaluateStmt(lambdaExp.Body, context);
        }

        void EvaluateMemberCallExp(QsMemberCallExp memberCallExp, QsTypeEvalContext context)
        {
            EvaluateExp(memberCallExp.Object, context);
            EvaluateTypeExps(memberCallExp.MemberTypeArgs, context);

            foreach (var arg in memberCallExp.Args)
                EvaluateExp(arg, context);
        }

        void EvaluateMemberExp(QsMemberExp memberExp, QsTypeEvalContext context)
        {
            EvaluateExp(memberExp.Object, context);

            EvaluateTypeExps(memberExp.MemberTypeArgs, context);
        }

        void EvaluateListExp(QsListExp listExp, QsTypeEvalContext context)
        {
            foreach (var elem in listExp.Elems)
                EvaluateExp(elem, context);
        }

        void EvaluateExp(QsExp exp, QsTypeEvalContext context)
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
                case QsMemberCallExp memberCallExp: EvaluateMemberCallExp(memberCallExp, context); break;
                case QsMemberExp memberExp: EvaluateMemberExp(memberExp, context); break;
                case QsListExp listExp: EvaluateListExp(listExp, context); break;
                default: throw new NotImplementedException();
            }
        }
        
        void EvaluateCommandStmt(QsCommandStmt cmdStmt, QsTypeEvalContext context)
        {
            foreach (var cmd in cmdStmt.Commands)
                EvaluateStringExpElements(cmd.Elements, context);
        }

        void EvaluateVarDeclStmt(QsVarDeclStmt varDeclStmt, QsTypeEvalContext context) 
        {
            EvaluateVarDecl(varDeclStmt.VarDecl, context);
        }

        void EvaluateIfStmt(QsIfStmt ifStmt, QsTypeEvalContext context)
        {
            EvaluateExp(ifStmt.Cond, context);

            if (ifStmt.TestType != null)
                EvaluateTypeExp(ifStmt.TestType, context, out var _);

            EvaluateStmt(ifStmt.Body, context);

            if (ifStmt.ElseBody != null)
                EvaluateStmt(ifStmt.ElseBody, context);
        }

        void EvaluateForStmtInitializer(QsForStmtInitializer initializer, QsTypeEvalContext context)
        {
            switch(initializer)
            {
                case QsExpForStmtInitializer expInit: EvaluateExp(expInit.Exp, context); break;
                case QsVarDeclForStmtInitializer varDeclInit: EvaluateVarDecl(varDeclInit.VarDecl, context); break;
                default: throw new NotImplementedException();
            }
        }

        void EvaluateForStmt(QsForStmt forStmt, QsTypeEvalContext context)
        {
            if (forStmt.Initializer != null)
                EvaluateForStmtInitializer(forStmt.Initializer, context);

            if (forStmt.CondExp != null)
                EvaluateExp(forStmt.CondExp, context);

            if (forStmt.ContinueExp != null)
                EvaluateExp(forStmt.ContinueExp, context);

            EvaluateStmt(forStmt.Body, context);
        }

        void EvaluateContinueStmt(QsContinueStmt continueStmt, QsTypeEvalContext context)
        {
        }

        void EvaluateBreakStmt(QsBreakStmt breakStmt, QsTypeEvalContext context)
        {
        }

        void EvaluateReturnStmt(QsReturnStmt returnStmt, QsTypeEvalContext context) 
        {
            if (returnStmt.Value != null)
                EvaluateExp(returnStmt.Value, context);
        }

        void EvaluateBlockStmt(QsBlockStmt blockStmt, QsTypeEvalContext context)
        {
            foreach (var stmt in blockStmt.Stmts)
                EvaluateStmt(stmt, context);
        }

        void EvaluateExpStmt(QsExpStmt expStmt, QsTypeEvalContext context)
        {
            EvaluateExp(expStmt.Exp, context);
        }

        void EvaluateTaskStmt(QsTaskStmt taskStmt, QsTypeEvalContext context)
        {
            EvaluateStmt(taskStmt.Body, context);
        }

        void EvaluateAwaitStmt(QsAwaitStmt awaitStmt, QsTypeEvalContext context)
        {
            EvaluateStmt(awaitStmt.Body, context);
        }

        void EvaluateAsyncStmt(QsAsyncStmt asyncStmt, QsTypeEvalContext context)
        {
            EvaluateStmt(asyncStmt.Body, context);
        }

        void EvaluateForeachStmt(QsForeachStmt foreachStmt, QsTypeEvalContext context) 
        {
            EvaluateTypeExp(foreachStmt.Type, context, out var _);
            EvaluateExp(foreachStmt.Obj, context);
            EvaluateStmt(foreachStmt.Body, context);
        }

        void EvaluateYieldStmt(QsYieldStmt yieldStmt, QsTypeEvalContext context)
        {
            EvaluateExp(yieldStmt.Value, context);
        }

        void EvaluateStmt(QsStmt stmt, QsTypeEvalContext context)
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
                case QsExpStmt expStmt: EvaluateExpStmt(expStmt, context); break;
                case QsTaskStmt taskStmt: EvaluateTaskStmt(taskStmt, context); break;
                case QsAwaitStmt awaitStmt: EvaluateAwaitStmt(awaitStmt, context); break;
                case QsAsyncStmt asyncStmt: EvaluateAsyncStmt(asyncStmt, context); break;
                case QsForeachStmt foreachStmt: EvaluateForeachStmt(foreachStmt, context); break;
                case QsYieldStmt yieldStmt: EvaluateYieldStmt(yieldStmt, context); break;                        
                default: throw new NotImplementedException();
            };
        }

        public void EvaluateScript(QsScript script, QsTypeEvalContext context)
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
    }
}
