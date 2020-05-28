using QuickSC.Syntax;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;

namespace QuickSC.StaticAnalyzer
{
    public enum QsCaptureKind
    {
        Copy,
        Ref
    }

    public class QsCaptureResult
    {
        public ImmutableArray<(string VarName, QsCaptureKind Kind)> NeedCaptures { get; }
        public QsCaptureResult(ImmutableArray<(string VarName, QsCaptureKind Kind)> needCaptures)
        {
            NeedCaptures = needCaptures;
        }
    }

    class QsCaptureContext
    {
        ImmutableHashSet<string> boundVars;
        Dictionary<string, QsCaptureKind> needCaptures { get; } // bool => ref or copy 

        public QsCaptureContext()
        {
            boundVars = ImmutableHashSet<string>.Empty;
            needCaptures = new Dictionary<string, QsCaptureKind>();
        }

        public void AddBind(string varName)
        {
            boundVars = boundVars.Add(varName);
        }

        public void AddBinds(IEnumerable<string> names)
        {
            boundVars = boundVars.Union(names);
        }

        public bool IsBound(string name)
        {
            return boundVars.Contains(name);
        }

        public void AddCapture(string name, QsCaptureKind kind)
        {
            if (needCaptures.TryGetValue(name, out var prevKind))
                if (prevKind == QsCaptureKind.Ref || kind == prevKind)
                    return;

            needCaptures[name] = kind;
        }

        public ImmutableHashSet<string> GetBoundVars()
        {
            return boundVars;
        }

        public void SetBoundVars(ImmutableHashSet<string> newBoundVars)
        {
            boundVars = newBoundVars;
        }        

        public ImmutableDictionary<string, QsCaptureKind> GetNeedCaptures()
        {
            return needCaptures.ToImmutableDictionary();
        }
    }

    public class QsCapturer
    {
        bool CaptureStringExpElements(ImmutableArray<QsStringExpElement> elems, QsCaptureContext context)
        {
            foreach (var elem in elems)
            {
                if (elem is QsTextStringExpElement)
                {
                    continue;
                }
                else if (elem is QsExpStringExpElement expElem)
                {
                    if (!CaptureExp(expElem.Exp, context))
                        return false;
                    continue;
                }
                else
                {
                    throw new NotImplementedException();
                }
            }

            return true;
        }

        bool CaptureCommandStmt(QsCommandStmt cmdStmt, QsCaptureContext context)
        {
            foreach (var command in cmdStmt.Commands)
            {
                if (!CaptureStringExpElements(command.Elements, context))
                    return false;
            }

            return true;
        }

        bool CaptureVarDecl(QsVarDecl varDecl, QsCaptureContext context)
        {
            context.AddBinds(varDecl.Elems.Select(elem => elem.VarName));
            return true;
        }

        bool CaptureVarDeclStmt(QsVarDeclStmt varDeclStmt, QsCaptureContext context)
        {
            return CaptureVarDecl(varDeclStmt.VarDecl, context);
        }

        bool CaptureIfStmt(QsIfStmt ifStmt, QsCaptureContext context) 
        {
            if (!CaptureExp(ifStmt.Cond, context))
                return false;

            // TestType은 capture할 것이 없다

            if (!CaptureStmt(ifStmt.Body, context))
                return false;

            if (ifStmt.ElseBody != null)
            {
                if (!CaptureStmt(ifStmt.ElseBody, context))
                    return false;
            }

            return true;
        }

        bool CaptureForStmtInitialize(QsForStmtInitializer forInitStmt, QsCaptureContext context)
        {
            return forInitStmt switch
            {
                QsVarDeclForStmtInitializer varDeclInit => CaptureVarDecl(varDeclInit.VarDecl, context),
                QsExpForStmtInitializer expInit => CaptureExp(expInit.Exp, context),
                _ => throw new NotImplementedException()
            };
        }

        bool CaptureForStmt(QsForStmt forStmt, QsCaptureContext context)         
        {
            var prevBoundVars = context.GetBoundVars();

            if (forStmt.Initializer != null)
            {
                if (!CaptureForStmtInitialize(forStmt.Initializer, context))
                    return false;
            }

            if (forStmt.CondExp != null)
            {
                if (!CaptureExp(forStmt.CondExp, context))
                    return false;
            }

            if (forStmt.ContinueExp != null )
            {
                if (!CaptureExp(forStmt.ContinueExp, context))
                    return false;
            }

            if (!CaptureStmt(forStmt.Body, context))
                return false;

            context.SetBoundVars(prevBoundVars);
            return true;
        }

        bool CaptureContinueStmt(QsContinueStmt continueStmt, QsCaptureContext context) { return true; }
        bool CaptureBreakStmt(QsBreakStmt breakStmt, QsCaptureContext context) { return true; }

        bool CaptureReturnStmt(QsReturnStmt returnStmt, QsCaptureContext context)
        {
            if (returnStmt.Value != null)
                return CaptureExp(returnStmt.Value, context);
            else
                return true;
        }

        bool CaptureBlockStmt(QsBlockStmt blockStmt, QsCaptureContext context) 
        {
            var prevBoundVars = context.GetBoundVars();

            foreach(var stmt in blockStmt.Stmts)
            {
                if (!CaptureStmt(stmt, context))
                    return false;
            }

            context.SetBoundVars(prevBoundVars);
            return true;
        }

        bool CaptureExpStmt(QsExpStmt expStmt, QsCaptureContext context)
        {
            return CaptureExp(expStmt.Exp, context);
        }

        bool CaptureTaskStmt(QsTaskStmt stmt, QsCaptureContext context)
        {
            var prevBoundVars = context.GetBoundVars();

            if (!CaptureStmt(stmt.Body, context))
                return false;

            context.SetBoundVars(prevBoundVars);
            return true;
        }

        bool CaptureAwaitStmt(QsAwaitStmt stmt, QsCaptureContext context)
        {
            var prevBoundVars = context.GetBoundVars();

            if (!CaptureStmt(stmt.Body, context))
                return false;

            context.SetBoundVars(prevBoundVars);
            return true;
        }

        bool CaptureAsyncStmt(QsAsyncStmt stmt, QsCaptureContext context)
        {
            var prevBoundVars = context.GetBoundVars();

            if (!CaptureStmt(stmt.Body, context))
                return false;

            context.SetBoundVars(prevBoundVars);
            return true;
        }

        bool CaptureForeachStmt(QsForeachStmt foreachStmt, QsCaptureContext context)
        {
            var prevBoundVars = context.GetBoundVars();

            if (!CaptureExp(foreachStmt.Obj, context))
                return false;

            context.AddBind(foreachStmt.VarName);

            if (!CaptureStmt(foreachStmt.Body, context))
                return false;

            context.SetBoundVars(prevBoundVars);
            return true;
        }

        bool CaptureYieldStmt(QsYieldStmt yieldStmt, QsCaptureContext context)
        {
            return CaptureExp(yieldStmt.Value, context);
        }

        bool CaptureStmt(QsStmt stmt, QsCaptureContext context)
        {
            return stmt switch
            {
                QsCommandStmt cmdStmt => CaptureCommandStmt(cmdStmt, context),
                QsVarDeclStmt varDeclStmt => CaptureVarDeclStmt(varDeclStmt, context),
                QsIfStmt ifStmt => CaptureIfStmt(ifStmt, context),
                QsForStmt forStmt => CaptureForStmt(forStmt, context),
                QsContinueStmt continueStmt => CaptureContinueStmt(continueStmt, context),
                QsBreakStmt breakStmt => CaptureBreakStmt(breakStmt, context),
                QsReturnStmt returnStmt => CaptureReturnStmt(returnStmt, context),
                QsBlockStmt blockStmt => CaptureBlockStmt(blockStmt, context),
                QsExpStmt expStmt => CaptureExpStmt(expStmt, context),
                QsTaskStmt taskStmt => CaptureTaskStmt(taskStmt, context),
                QsAwaitStmt awaitStmt => CaptureAwaitStmt(awaitStmt, context),
                QsAsyncStmt asyncStmt => CaptureAsyncStmt(asyncStmt, context),
                QsForeachStmt foreachStmt => CaptureForeachStmt(foreachStmt, context),
                QsYieldStmt yieldStmt => CaptureYieldStmt(yieldStmt, context),

                _ => throw new NotImplementedException()
            };
        }

        bool RefCaptureIdExp(QsIdentifierExp idExp, QsCaptureContext context)
        {
            var varName = idExp.Value;

            // 바인드에 있는지 보고 
            if (!context.IsBound(varName))
            {
                // 캡쳐에 추가
                context.AddCapture(varName, QsCaptureKind.Ref);
            }

            return true;
        }

        bool RefCaptureExp(QsExp exp, QsCaptureContext context)
        {
            return exp switch
            {
                QsIdentifierExp idExp => RefCaptureIdExp(idExp, context),
                QsBoolLiteralExp boolExp => throw new InvalidOperationException(),
                QsIntLiteralExp intExp => throw new InvalidOperationException(),
                QsStringExp stringExp => throw new InvalidOperationException(),
                QsUnaryOpExp unaryOpExp => throw new InvalidOperationException(),
                QsBinaryOpExp binaryOpExp => throw new InvalidOperationException(),
                QsCallExp callExp => throw new InvalidOperationException(),
                QsLambdaExp lambdaExp => throw new InvalidOperationException(),
                QsMemberCallExp memberCallExp => throw new InvalidOperationException(),
                QsMemberExp memberExp => CaptureMemberExp(memberExp, context),
                QsListExp listExp => throw new InvalidOperationException(),

                _ => throw new NotImplementedException()
            };
        }

        bool CaptureIdExp(QsIdentifierExp idExp, QsCaptureContext context) 
        {            
            var varName = idExp.Value;

            // 바인드에 있는지 보고 
            if (!context.IsBound(varName))
            {
                // 캡쳐에 추가
                context.AddCapture(varName, QsCaptureKind.Copy);
            }

            return true;
        }

        bool CaptureBoolLiteralExp(QsBoolLiteralExp boolExp, QsCaptureContext context) => true;
        bool CaptureIntLiteralExp(QsIntLiteralExp intExp, QsCaptureContext context) => true;
        bool CaptureStringExp(QsStringExp stringExp, QsCaptureContext context)
        {
            return CaptureStringExpElements(stringExp.Elements, context);
        }

        bool CaptureUnaryOpExp(QsUnaryOpExp unaryOpExp, QsCaptureContext context) 
        {
            // ++i, i++은 ref를 유발한다
            if (unaryOpExp.Kind == QsUnaryOpKind.PostfixInc ||
                unaryOpExp.Kind == QsUnaryOpKind.PostfixDec ||
                unaryOpExp.Kind == QsUnaryOpKind.PrefixInc ||
                unaryOpExp.Kind == QsUnaryOpKind.PrefixDec)
                return RefCaptureExp(unaryOpExp.Operand, context);
            else
                return CaptureExp(unaryOpExp.Operand, context);
        }

        bool CaptureBinaryOpExp(QsBinaryOpExp binaryOpExp, QsCaptureContext context) 
        { 
            if (binaryOpExp.Kind == QsBinaryOpKind.Assign)
            {
                if (!RefCaptureExp(binaryOpExp.Operand0, context))
                    return false;
            }
            else
            {
                if (!CaptureExp(binaryOpExp.Operand0, context))
                    return false;
            }

            if (!CaptureExp(binaryOpExp.Operand1, context))
                return false;

            return true;
        }

        bool CaptureCallExp(QsCallExp callExp, QsCaptureContext context) 
        {
            if (!CaptureExp(callExp.Callable, context))
                return false;

            foreach (var arg in callExp.Args)
            {
                if (!CaptureExp(arg, context))
                    return false;
            }

            return true;
        }

        bool CaptureLambdaExp(QsLambdaExp exp, QsCaptureContext context)
        {
            var prevBoundVars = context.GetBoundVars();

            context.AddBinds(exp.Params.Select(param => param.Name));

            if (!CaptureStmt(exp.Body, context))
                return false;            

            context.SetBoundVars(prevBoundVars);
            return true;
        }

        bool CaptureMemberCallExp(QsMemberCallExp exp, QsCaptureContext context)
        {
            // a.b.c(); 라면 a만 캡쳐하면 된다
            return CaptureExp(exp.Object, context);
        }

        bool CaptureMemberExp(QsMemberExp exp, QsCaptureContext context)
        {
            return CaptureExp(exp.Object, context);
        }

        bool CaptureListExp(QsListExp exp, QsCaptureContext context)
        {
            foreach(var elem in exp.Elems)
            {
                if (!CaptureExp(elem, context))
                    return false;
            }

            return true;
        }

        bool CaptureExp(QsExp exp, QsCaptureContext context)
        {
            return exp switch
            {
                QsIdentifierExp idExp => CaptureIdExp(idExp, context),
                QsBoolLiteralExp boolExp => CaptureBoolLiteralExp(boolExp, context),
                QsIntLiteralExp intExp => CaptureIntLiteralExp(intExp, context),
                QsStringExp stringExp => CaptureStringExp(stringExp, context),
                QsUnaryOpExp unaryOpExp => CaptureUnaryOpExp(unaryOpExp, context),
                QsBinaryOpExp binaryOpExp => CaptureBinaryOpExp(binaryOpExp, context),
                QsCallExp callExp => CaptureCallExp(callExp, context),
                QsLambdaExp lambdaExp => CaptureLambdaExp(lambdaExp, context),
                QsMemberCallExp memberCallExp => CaptureMemberCallExp(memberCallExp, context),
                QsMemberExp memberExp => CaptureMemberExp(memberExp, context),
                QsListExp listExp => CaptureListExp(listExp, context),

                _ => throw new NotImplementedException()
            };
        }

        // entry
        public bool Capture(QsStmt stmt, [NotNullWhen(returnValue: true)] out QsCaptureResult? outCaptureResult)
        {
            var context = new QsCaptureContext();

            if (!CaptureStmt(stmt, context))
            {
                outCaptureResult = null;
                return false;
            }

            // TODO: 일단 Capture this는 false이다
            var needCaptures = context.GetNeedCaptures().Select(kv => (kv.Key, kv.Value)).ToImmutableArray();
            outCaptureResult = new QsCaptureResult(needCaptures);
            return true;
        }
    }
}
