using QuickSC;
using QuickSC.Syntax;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text;
using static QuickSC.StaticAnalyzer.QsAnalyzer.Misc;

namespace QuickSC.StaticAnalyzer
{
    class QsStmtAnalyzer
    {
        QsAnalyzer analyzer;

        public QsStmtAnalyzer(QsAnalyzer analyzer)
        {
            this.analyzer = analyzer;
        }

        // CommandStmt에 있는 expStringElement를 분석한다
        bool AnalyzeCommandStmt(QsCommandStmt cmdStmt, QsAnalyzer.Context context)
        {
            bool bResult = true;

            foreach (var cmd in cmdStmt.Commands)
                foreach (var elem in cmd.Elements)
                    bResult &= analyzer.AnalyzeStringExpElement(elem, context);

            return bResult;
        }

        bool AnalyzeVarDeclStmt(QsVarDeclStmt varDeclStmt, QsAnalyzer.Context context)
        {
            return analyzer.AnalyzeVarDecl(varDeclStmt.VarDecl, context);
        }

        bool AnalyzeIfStmt(QsIfStmt ifStmt, QsAnalyzer.Context context) 
        {
            bool bResult = true;

            var boolTypeValue = analyzer.GetBoolTypeValue();
            
            if (!analyzer.AnalyzeExp(ifStmt.Cond, context, out var condTypeValue))
                return false;            
            
            // 일반적인 경우,
            if (ifStmt.TestType == null)
            {
                if (!analyzer.IsAssignable(boolTypeValue, condTypeValue, context))
                    context.ErrorCollector.Add(ifStmt, "if 조건 식은 항상 bool형식이어야 합니다");

                bResult &= AnalyzeStmt(ifStmt.Body, context);

                if (ifStmt.ElseBody != null)
                    bResult &= AnalyzeStmt(ifStmt.ElseBody, context);

                return bResult;
            }
            else
            {
                // TODO: if (Type v = exp as Type) 구문 추가

                // if (exp is X) 구문은 exp가 identifier일때만 가능하다
                if (ifStmt.Cond is QsIdentifierExp idExpCond)
                {
                    // TestType이 있을때만 넣는다
                    var testTypeValue = context.GetTypeValueByTypeExp(ifStmt.TestType);
                    context.AddNodeInfo(ifStmt, new QsIfStmtInfo(testTypeValue));

                    var typeArgs = GetTypeValues(idExpCond.TypeArgs, context);
                    if (!context.GetIdentifierInfo(idExpCond.Value, typeArgs, out var idInfo))
                    {
                        context.ErrorCollector.Add(ifStmt.Cond, $"{idExpCond.Value}를 찾지 못했습니다");
                        return false;
                    }

                    if (idInfo is QsAnalyzerIdentifierInfo.Var varIdInfo)
                    {
                        // Global일지, Static일지, This일지 모른다
                        context.ExecInLocalScope(() =>
                        {
                            context.AddOverrideVarInfo(varIdInfo.StorageInfo, testTypeValue);

                            if (!AnalyzeStmt(ifStmt.Body, context))
                                bResult = false;

                            if (ifStmt.ElseBody != null)
                                if (!AnalyzeStmt(ifStmt.ElseBody, context))
                                    bResult = false;
                        });
                        return bResult;
                    }
                    else
                    {
                        context.ErrorCollector.Add(ifStmt.Cond, "if (exp is Type) 구문은 exp가 변수여야 합니다");
                        return false;
                    }
                }
                else
                {
                    context.ErrorCollector.Add(ifStmt.Cond, "if (exp is Type) 구문은 exp가 identifier여야 합니다");
                    return false;
                }
            }
        }

        bool AnalyzeForStmtInitializer(QsForStmtInitializer forInit, QsAnalyzer.Context context)
        {
            switch(forInit)
            {
                case QsVarDeclForStmtInitializer varDeclInit: 
                    return analyzer.AnalyzeVarDecl(varDeclInit.VarDecl, context);

                case QsExpForStmtInitializer expInit:
                    {
                        if (analyzer.AnalyzeExp(expInit.Exp, context, out var expTypeValue))
                        {
                            context.AddNodeInfo(expInit, new QsExpForStmtInitializerInfo(expTypeValue));
                            return true;
                        }

                        return false;
                    }

                default: 
                    throw new NotImplementedException();
            }
        }

        bool AnalyzeForStmt(QsForStmt forStmt, QsAnalyzer.Context context)
        {
            bool bResult = true;

            var boolTypeValue = analyzer.GetBoolTypeValue();

            context.ExecInLocalScope(() =>
            {
                if (forStmt.Initializer != null)
                    if (!AnalyzeForStmtInitializer(forStmt.Initializer, context))
                        bResult = false;

                if (forStmt.CondExp != null)
                {
                    // 밑에서 쓰이므로 분석실패시 종료
                    if (!analyzer.AnalyzeExp(forStmt.CondExp, context, out var condExpTypeValue))
                    {
                        bResult = false;
                        return;
                    }

                    // 에러가 나면 에러를 추가하고 계속 진행
                    if (!analyzer.IsAssignable(boolTypeValue, condExpTypeValue, context))
                        context.ErrorCollector.Add(forStmt.CondExp, $"{forStmt.CondExp}는 bool 형식이어야 합니다");
                }

                QsTypeValue? contTypeValue = null;
                if (forStmt.ContinueExp != null)
                    if (!analyzer.AnalyzeExp(forStmt.ContinueExp, context, out contTypeValue))
                        bResult = false;

                if (!AnalyzeStmt(forStmt.Body, context))
                    bResult = false;

                context.AddNodeInfo(forStmt, new QsForStmtInfo(contTypeValue));
            });

            return bResult;
        }

        bool AnalyzeContinueStmt(QsContinueStmt continueStmt, QsAnalyzer.Context context)
        {
            // TODO: loop안에 있는지 확인한다
            return true;
        }

        bool AnalyzeBreakStmt(QsBreakStmt breakStmt, QsAnalyzer.Context context)
        {
            // loop안에 있는지 확인해야 한다
            return true;
        }
        
        bool AnalyzeReturnStmt(QsReturnStmt returnStmt, QsAnalyzer.Context context)
        {   
            if (returnStmt.Value != null)
            {
                if (context.IsSeqFunc())
                {
                    context.ErrorCollector.Add(returnStmt, $"seq 함수는 빈 return만 허용됩니다");
                    return false;
                }

                if (!analyzer.AnalyzeExp(returnStmt.Value, context, out var returnValueTypeValue))
                    return false;

                var retTypeValue = context.GetRetTypeValue();
                
                if (retTypeValue != null)
                {
                    // 현재 함수 시그니처랑 맞춰서 같은지 확인한다
                    if (!analyzer.IsAssignable(retTypeValue, returnValueTypeValue, context))
                        context.ErrorCollector.Add(returnStmt.Value, $"반환값의 타입 {returnValueTypeValue}는 이 함수의 반환타입과 맞지 않습니다");
                }
                else // 리턴타입이 정해지지 않았을 경우가 있다
                {
                    context.SetRetTypeValue(returnValueTypeValue);
                }
            }
            else
            {
                if (!context.IsSeqFunc() && context.GetRetTypeValue() != QsTypeValue.MakeVoid())
                    context.ErrorCollector.Add(returnStmt.Value!, $"이 함수는 {context.GetRetTypeValue()}을 반환해야 합니다");
            }

            return true;
        }

        bool AnalyzeBlockStmt(QsBlockStmt blockStmt, QsAnalyzer.Context context)
        {
            bool bResult = true;

            context.ExecInLocalScope(() =>
            {
                foreach (var stmt in blockStmt.Stmts)
                {
                    bResult &= AnalyzeStmt(stmt, context);
                }
            });

            return bResult;
        }

        bool AnalyzeExpStmt(QsExpStmt expStmt, QsAnalyzer.Context context)
        {
            bool bResult = true;

            if ((expStmt.Exp is QsUnaryOpExp unOpExp && (unOpExp.Kind != QsUnaryOpKind.PostfixInc || 
                    unOpExp.Kind != QsUnaryOpKind.PostfixDec ||
                    unOpExp.Kind != QsUnaryOpKind.PrefixInc ||
                    unOpExp.Kind != QsUnaryOpKind.PrefixDec)) && 
                (expStmt.Exp is QsBinaryOpExp binOpExp && binOpExp.Kind != QsBinaryOpKind.Assign) &&
                !(expStmt.Exp is QsCallExp) &&
                !(expStmt.Exp is QsMemberCallExp))
            {
                context.ErrorCollector.Add(expStmt, "대입, 함수 호출만 구문으로 사용할 수 있습니다");
                bResult = false;
            }

            if (analyzer.AnalyzeExp(expStmt.Exp, context, out var expTypeValue))
                context.AddNodeInfo(expStmt, new QsExpStmtInfo(expTypeValue));
            else
                bResult = false;

            return bResult;            
        }

        bool AnalyzeTaskStmt(QsTaskStmt taskStmt, QsAnalyzer.Context context)
        {
            if (!analyzer.AnalyzeLambda(taskStmt.Body, ImmutableArray<QsLambdaExpParam>.Empty, context, out var captureInfo, out var funcTypeValue, out int localVarCount))
                return false;

            context.AddNodeInfo(taskStmt, new QsTaskStmtInfo(captureInfo, localVarCount));
            return true;
        }

        bool AnalyzeAwaitStmt(QsAwaitStmt awaitStmt, QsAnalyzer.Context context)
        {
            bool bResult = true;

            context.ExecInLocalScope(() =>
            {
                bResult &= AnalyzeStmt(awaitStmt.Body, context);
            });
            
            return bResult;
        }

        bool AnalyzeAsyncStmt(QsAsyncStmt asyncStmt, QsAnalyzer.Context context)
        {
            if (!analyzer.AnalyzeLambda(asyncStmt.Body, ImmutableArray<QsLambdaExpParam>.Empty, context, out var captureInfo, out var funcTypeValue, out int localVarCount))
                return false;

            context.AddNodeInfo(asyncStmt, new QsAsyncStmtInfo(captureInfo, localVarCount));
            return true;
        }
        
        bool AnalyzeForeachStmt(QsForeachStmt foreachStmt, QsAnalyzer.Context context)
        {
            var boolTypeValue = analyzer.GetBoolTypeValue();            

            if (!analyzer.AnalyzeExp(foreachStmt.Obj, context, out var objTypeValue))
                return false;

            var elemTypeValue = context.GetTypeValueByTypeExp(foreachStmt.Type);

            if (!context.TypeValueService.GetMemberFuncValue(
                objTypeValue,
                QsName.MakeText("GetEnumerator"), ImmutableArray<QsTypeValue>.Empty,
                out var getEnumeratorValue))
            {
                context.ErrorCollector.Add(foreachStmt.Obj, "foreach ... in 뒤 객체는 IEnumerator<T> GetEnumerator() 함수가 있어야 합니다.");
                return false;
            }

            // TODO: 일단 인터페이스가 없으므로, bool MoveNext()과 T GetCurrent()가 있는지 본다
            // TODO: 각 함수들이 thiscall인지도 확인해야 한다

            // 1. elemTypeValue가 VarTypeValue이면 GetEnumerator의 리턴값으로 판단한다
            var getEnumeratorTypeValue = context.TypeValueService.GetTypeValue(getEnumeratorValue);

            if (!context.TypeValueService.GetMemberFuncValue(
                getEnumeratorTypeValue.Return,
                QsName.MakeText("MoveNext"), ImmutableArray<QsTypeValue>.Empty, 
                out var moveNextValue))
            {
                context.ErrorCollector.Add(foreachStmt.Obj, "enumerator doesn't have 'bool MoveNext()' function");
                return false;
            }

            var moveNextTypeValue = context.TypeValueService.GetTypeValue(moveNextValue);

            if (!analyzer.IsAssignable(boolTypeValue, moveNextTypeValue.Return, context))
            {
                context.ErrorCollector.Add(foreachStmt.Obj, "enumerator doesn't have 'bool MoveNext()' function");
                return false;
            }

            if (!context.TypeValueService.GetMemberFuncValue(
                getEnumeratorTypeValue.Return, 
                QsName.MakeText("GetCurrent"), ImmutableArray<QsTypeValue>.Empty, 
                out var getCurrentValue))
            {
                context.ErrorCollector.Add(foreachStmt.Obj, "enumerator doesn't have 'GetCurrent()' function");
                return false;
            }

            var getCurrentTypeValue = context.TypeValueService.GetTypeValue(getCurrentValue);
            if (getCurrentTypeValue.Return is QsTypeValue.Void)
            {
                context.ErrorCollector.Add(foreachStmt.Obj, "'GetCurrent()' function cannot return void");
                return false;
            }

            if (elemTypeValue is QsTypeValue.Var)
            {   
                elemTypeValue = getCurrentTypeValue.Return;

                //var interfaces = typeValueService.GetInterfaces("IEnumerator", 1, funcTypeValue.RetTypeValue);

                //if (1 < interfaces.Count)
                //{
                //    context.ErrorCollector.Add(foreachStmt.Obj, "변수 타입으로 var를 사용하였는데, IEnumerator<T>가 여러개라 어느 것을 사용할지 결정할 수 없습니다.");
                //    return;
                //}
            }
            else
            {
                if (!analyzer.IsAssignable(elemTypeValue, getCurrentTypeValue.Return, context))
                    context.ErrorCollector.Add(foreachStmt, $"foreach(T ... in obj) 에서 obj.GetEnumerator().GetCurrent()의 결과를 {elemTypeValue} 타입으로 캐스팅할 수 없습니다");
            }

            bool bResult = true;

            context.ExecInLocalScope(() =>
            {
                int elemLocalIndex = context.AddLocalVarInfo(foreachStmt.VarName, elemTypeValue);
                bResult &= AnalyzeStmt(foreachStmt.Body, context);

                context.AddNodeInfo(foreachStmt, new QsForeachStmtInfo(objTypeValue, getEnumeratorTypeValue.Return, elemTypeValue, elemLocalIndex, getEnumeratorValue, moveNextValue, getCurrentValue));
            });
            
            return bResult;
        }

        bool AnalyzeYieldStmt(QsYieldStmt yieldStmt, QsAnalyzer.Context context)
        {
            if (!context.IsSeqFunc())
            {
                context.ErrorCollector.Add(yieldStmt, "seq 함수 내부에서만 yield를 사용할 수 있습니다");
                return false;
            }

            if (!analyzer.AnalyzeExp(yieldStmt.Value, context, out var yieldTypeValue))
                return false;

            // yield에서는 retType이 명시되는 경우만 있을 것이다
            var retTypeValue = context.GetRetTypeValue();
            Debug.Assert(retTypeValue != null);

            if (!analyzer.IsAssignable(retTypeValue, yieldTypeValue, context))
            {
                context.ErrorCollector.Add(yieldStmt.Value, $"반환 값의 {yieldTypeValue} 타입은 이 함수의 반환 타입과 맞지 않습니다");
                return false;
            }

            return true;
        }

        public bool AnalyzeStmt(QsStmt stmt, QsAnalyzer.Context context)
        {
            switch (stmt)
            {
                case QsCommandStmt cmdStmt: return AnalyzeCommandStmt(cmdStmt, context); 
                case QsVarDeclStmt varDeclStmt: return AnalyzeVarDeclStmt(varDeclStmt, context); 
                case QsIfStmt ifStmt: return AnalyzeIfStmt(ifStmt, context); 
                case QsForStmt forStmt: return AnalyzeForStmt(forStmt, context); 
                case QsContinueStmt continueStmt: return AnalyzeContinueStmt(continueStmt, context); 
                case QsBreakStmt breakStmt: return AnalyzeBreakStmt(breakStmt, context); 
                case QsReturnStmt returnStmt: return AnalyzeReturnStmt(returnStmt, context); 
                case QsBlockStmt blockStmt: return AnalyzeBlockStmt(blockStmt, context); 
                case QsBlankStmt _: return true;
                case QsExpStmt expStmt: return AnalyzeExpStmt(expStmt, context); 
                case QsTaskStmt taskStmt: return AnalyzeTaskStmt(taskStmt, context);
                case QsAwaitStmt awaitStmt: return AnalyzeAwaitStmt(awaitStmt, context); 
                case QsAsyncStmt asyncStmt: return AnalyzeAsyncStmt(asyncStmt, context); 
                case QsForeachStmt foreachStmt: return AnalyzeForeachStmt(foreachStmt, context); 
                case QsYieldStmt yieldStmt: return AnalyzeYieldStmt(yieldStmt, context); 
                default: throw new NotImplementedException();
            }
        }
    }
}
