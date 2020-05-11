using QuickSC.Syntax;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection.Emit;
using System.Text;

namespace QuickSC.TypeExpEvaluator
{
    // TypeExp를 TypeValue로 바꿔서 저장합니다.
    public class QsTypeExpEvaluator
    {
        public QsTypeExpEvaluator()
        {
        }

        bool EvaluateIdTypeExp(QsIdTypeExp exp, QsTypeEvalContext context, [NotNullWhen(returnValue: true)] out QsTypeValue? typeValue)
        {
            typeValue = null;

            // 1. TypeVar에서 먼저 검색
            if (context.TypeEnv.TryGetValue(exp.Name, out typeValue))
            {
                context.TypeExpTypeValues.Add(exp, typeValue);
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
                context.TypeExpTypeValues.Add(exp, typeValue);
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

            context.TypeExpTypeValues.Add(exp, typeValue);
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
            
            foreach(var typeParam in enumDecl.TypeParams)
            {
                context.UpdateTypeVar(typeParam, new QsTypeVarTypeValue(enumDecl, typeParam));
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

            foreach (var param in funcDecl.TypeParams)
                context.UpdateTypeVar(param, new QsTypeVarTypeValue(funcDecl, param));

            EvaluateStmt(funcDecl.Body, context);

            context.TypeEnv = prevTypeEnv;
        }

        void EvaluateStmt(QsStmt stmt, QsTypeEvalContext context)
        {

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
