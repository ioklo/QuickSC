using Gum.Syntax;
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

        public QsCaptureContext(IEnumerable<string> initBoundVars)
        {
            boundVars = ImmutableHashSet.CreateRange<string>(initBoundVars);
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
        bool CaptureStringExpElements(ImmutableArray<StringExpElement> elems, QsCaptureContext context)
        {
            foreach (var elem in elems)
            {
                if (elem is TextStringExpElement)
                {
                    continue;
                }
                else if (elem is ExpStringExpElement expElem)
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

        bool CaptureCommandStmt(CommandStmt cmdStmt, QsCaptureContext context)
        {
            foreach (var command in cmdStmt.Commands)
            {
                if (!CaptureStringExpElements(command.Elements, context))
                    return false;
            }

            return true;
        }

        bool CaptureVarDecl(VarDecl varDecl, QsCaptureContext context)
        {
            context.AddBinds(varDecl.Elems.Select(elem => elem.VarName));
            return true;
        }

        bool CaptureVarDeclStmt(VarDeclStmt varDeclStmt, QsCaptureContext context)
        {
            return CaptureVarDecl(varDeclStmt.VarDecl, context);
        }

        bool CaptureIfStmt(IfStmt ifStmt, QsCaptureContext context) 
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

        bool CaptureForStmtInitialize(ForStmtInitializer forInitStmt, QsCaptureContext context)
        {
            return forInitStmt switch
            {
                VarDeclForStmtInitializer varDeclInit => CaptureVarDecl(varDeclInit.VarDecl, context),
                ExpForStmtInitializer expInit => CaptureExp(expInit.Exp, context),
                _ => throw new NotImplementedException()
            };
        }

        bool CaptureForStmt(ForStmt forStmt, QsCaptureContext context)         
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

        bool CaptureContinueStmt(ContinueStmt continueStmt, QsCaptureContext context) { return true; }
        bool CaptureBreakStmt(BreakStmt breakStmt, QsCaptureContext context) { return true; }

        bool CaptureReturnStmt(ReturnStmt returnStmt, QsCaptureContext context)
        {
            if (returnStmt.Value != null)
                return CaptureExp(returnStmt.Value, context);
            else
                return true;
        }

        bool CaptureBlockStmt(BlockStmt blockStmt, QsCaptureContext context) 
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

        bool CaptureExpStmt(ExpStmt expStmt, QsCaptureContext context)
        {
            return CaptureExp(expStmt.Exp, context);
        }

        bool CaptureTaskStmt(TaskStmt stmt, QsCaptureContext context)
        {
            var prevBoundVars = context.GetBoundVars();

            if (!CaptureStmt(stmt.Body, context))
                return false;

            context.SetBoundVars(prevBoundVars);
            return true;
        }

        bool CaptureAwaitStmt(AwaitStmt stmt, QsCaptureContext context)
        {
            var prevBoundVars = context.GetBoundVars();

            if (!CaptureStmt(stmt.Body, context))
                return false;

            context.SetBoundVars(prevBoundVars);
            return true;
        }

        bool CaptureAsyncStmt(AsyncStmt stmt, QsCaptureContext context)
        {
            var prevBoundVars = context.GetBoundVars();

            if (!CaptureStmt(stmt.Body, context))
                return false;

            context.SetBoundVars(prevBoundVars);
            return true;
        }

        bool CaptureForeachStmt(ForeachStmt foreachStmt, QsCaptureContext context)
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

        bool CaptureYieldStmt(YieldStmt yieldStmt, QsCaptureContext context)
        {
            return CaptureExp(yieldStmt.Value, context);
        }

        bool CaptureStmt(Stmt stmt, QsCaptureContext context)
        {
            return stmt switch
            {
                CommandStmt cmdStmt => CaptureCommandStmt(cmdStmt, context),
                VarDeclStmt varDeclStmt => CaptureVarDeclStmt(varDeclStmt, context),
                IfStmt ifStmt => CaptureIfStmt(ifStmt, context),
                ForStmt forStmt => CaptureForStmt(forStmt, context),
                ContinueStmt continueStmt => CaptureContinueStmt(continueStmt, context),
                BreakStmt breakStmt => CaptureBreakStmt(breakStmt, context),
                ReturnStmt returnStmt => CaptureReturnStmt(returnStmt, context),
                BlockStmt blockStmt => CaptureBlockStmt(blockStmt, context),
                BlankStmt blankStmt => true,
                ExpStmt expStmt => CaptureExpStmt(expStmt, context),
                TaskStmt taskStmt => CaptureTaskStmt(taskStmt, context),
                AwaitStmt awaitStmt => CaptureAwaitStmt(awaitStmt, context),
                AsyncStmt asyncStmt => CaptureAsyncStmt(asyncStmt, context),
                ForeachStmt foreachStmt => CaptureForeachStmt(foreachStmt, context),
                YieldStmt yieldStmt => CaptureYieldStmt(yieldStmt, context),

                _ => throw new NotImplementedException()
            };
        }

        bool RefCaptureIdExp(IdentifierExp idExp, QsCaptureContext context)
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

        bool RefCaptureExp(Exp exp, QsCaptureContext context)
        {
            return exp switch
            {
                IdentifierExp idExp => RefCaptureIdExp(idExp, context),
                BoolLiteralExp boolExp => throw new InvalidOperationException(),
                IntLiteralExp intExp => throw new InvalidOperationException(),
                StringExp stringExp => throw new InvalidOperationException(),
                UnaryOpExp unaryOpExp => throw new InvalidOperationException(),
                BinaryOpExp binaryOpExp => throw new InvalidOperationException(),
                CallExp callExp => throw new InvalidOperationException(),
                LambdaExp lambdaExp => throw new InvalidOperationException(),
                MemberCallExp memberCallExp => throw new InvalidOperationException(),
                MemberExp memberExp => CaptureMemberExp(memberExp, context),
                ListExp listExp => throw new InvalidOperationException(),

                _ => throw new NotImplementedException()
            };
        }

        bool CaptureIdExp(IdentifierExp idExp, QsCaptureContext context) 
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

        bool CaptureBoolLiteralExp(BoolLiteralExp boolExp, QsCaptureContext context) => true;
        bool CaptureIntLiteralExp(IntLiteralExp intExp, QsCaptureContext context) => true;
        bool CaptureStringExp(StringExp stringExp, QsCaptureContext context)
        {
            return CaptureStringExpElements(stringExp.Elements, context);
        }

        bool CaptureUnaryOpExp(UnaryOpExp unaryOpExp, QsCaptureContext context) 
        {
            // ++i, i++은 ref를 유발한다
            if (unaryOpExp.Kind == UnaryOpKind.PostfixInc ||
                unaryOpExp.Kind == UnaryOpKind.PostfixDec ||
                unaryOpExp.Kind == UnaryOpKind.PrefixInc ||
                unaryOpExp.Kind == UnaryOpKind.PrefixDec)
                return RefCaptureExp(unaryOpExp.Operand, context);
            else
                return CaptureExp(unaryOpExp.Operand, context);
        }

        bool CaptureBinaryOpExp(BinaryOpExp binaryOpExp, QsCaptureContext context) 
        { 
            if (binaryOpExp.Kind == BinaryOpKind.Assign)
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

        bool CaptureCallExp(CallExp callExp, QsCaptureContext context) 
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

        bool CaptureLambdaExp(LambdaExp exp, QsCaptureContext context)
        {
            var prevBoundVars = context.GetBoundVars();

            context.AddBinds(exp.Params.Select(param => param.Name));

            if (!CaptureStmt(exp.Body, context))
                return false;            

            context.SetBoundVars(prevBoundVars);
            return true;
        }

        bool CaptureMemberCallExp(MemberCallExp exp, QsCaptureContext context)
        {
            // a.b.c(); 라면 a만 캡쳐하면 된다
            return CaptureExp(exp.Object, context);
        }

        bool CaptureMemberExp(MemberExp exp, QsCaptureContext context)
        {
            return CaptureExp(exp.Object, context);
        }

        bool CaptureListExp(ListExp exp, QsCaptureContext context)
        {
            foreach(var elem in exp.Elems)
            {
                if (!CaptureExp(elem, context))
                    return false;
            }

            return true;
        }

        bool CaptureExp(Exp exp, QsCaptureContext context)
        {
            return exp switch
            {
                IdentifierExp idExp => CaptureIdExp(idExp, context),
                BoolLiteralExp boolExp => CaptureBoolLiteralExp(boolExp, context),
                IntLiteralExp intExp => CaptureIntLiteralExp(intExp, context),
                StringExp stringExp => CaptureStringExp(stringExp, context),
                UnaryOpExp unaryOpExp => CaptureUnaryOpExp(unaryOpExp, context),
                BinaryOpExp binaryOpExp => CaptureBinaryOpExp(binaryOpExp, context),
                CallExp callExp => CaptureCallExp(callExp, context),
                LambdaExp lambdaExp => CaptureLambdaExp(lambdaExp, context),
                MemberCallExp memberCallExp => CaptureMemberCallExp(memberCallExp, context),
                MemberExp memberExp => CaptureMemberExp(memberExp, context),
                ListExp listExp => CaptureListExp(listExp, context),

                _ => throw new NotImplementedException()
            };
        }

        // entry
        public bool Capture(IEnumerable<string> initBoundVars, Stmt stmt, [NotNullWhen(returnValue: true)] out QsCaptureResult? outCaptureResult)
        {
            var context = new QsCaptureContext(initBoundVars);

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
