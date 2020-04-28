using QuickSC.Syntax;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace QuickSC
{
    public enum QsCaptureContextCaptureKind
    {
        Copy,
        Ref
    }

    public struct QsCaptureContext
    {
        public ImmutableHashSet<string> BoundVars { get; }
        public ImmutableDictionary<string, QsCaptureContextCaptureKind> NeedCaptures { get; } // bool => ref or copy 

        public static QsCaptureContext Make()
        {
            return new QsCaptureContext(ImmutableHashSet<string>.Empty, ImmutableDictionary<string, QsCaptureContextCaptureKind>.Empty);
        }

        public QsCaptureContext(            
            ImmutableHashSet<string> boundVars,
            ImmutableDictionary<string, QsCaptureContextCaptureKind> needCaptures)
        {
            BoundVars = boundVars;
            NeedCaptures = needCaptures;
        }

        public QsCaptureContext AddBinds(IEnumerable<string> names)
        {
            return new QsCaptureContext(BoundVars.Union(names), NeedCaptures);
        }

        public bool IsBound(string name)
        {
            return BoundVars.Contains(name);
        }

        public QsCaptureContext AddCapture(string name, QsCaptureContextCaptureKind kind)
        {
            if (NeedCaptures.TryGetValue(name, out var prevKind))
                if (prevKind == QsCaptureContextCaptureKind.Ref || kind == prevKind)
                    return this;

            return new QsCaptureContext(BoundVars, NeedCaptures.SetItem(name, kind));
        }

        public QsCaptureContext UpdateBoundVars(ImmutableHashSet<string> boundVars)
        {
            return new QsCaptureContext(boundVars, NeedCaptures);
        }        
    }

    class QsEvalCapturer
    {
        QsCaptureContext? CaptureStringExpElements(ImmutableArray<QsStringExpElement> elems, QsCaptureContext context)
        {
            foreach (var elem in elems)
            {
                if (elem is QsTextStringExpElement)
                {
                    continue;
                }
                else if (elem is QsExpStringExpElement expElem)
                {
                    var expResult = CaptureExp(expElem.Exp, context);
                    if (expResult == null) return null;

                    context = expResult.Value;
                    continue;
                }
                else
                {
                    throw new NotImplementedException();
                }
            }

            return context;
        }

        QsCaptureContext? CaptureCommandStmt(QsCommandStmt cmdStmt, QsCaptureContext context)
        {
            foreach (var command in cmdStmt.Commands)
            {
                var elemsResult = CaptureStringExpElements(command.Elements, context);
                if (!elemsResult.HasValue) return null;
                context = elemsResult.Value;                
            }

            return context;
        }

        QsCaptureContext? CaptureVarDecl(QsVarDecl varDecl, QsCaptureContext context)
        {
            return context.AddBinds(varDecl.Elements.Select(elem => elem.VarName));
        }

        QsCaptureContext? CaptureVarDeclStmt(QsVarDeclStmt varDeclStmt, QsCaptureContext context)
        {
            return CaptureVarDecl(varDeclStmt.VarDecl, context);
        }

        QsCaptureContext? CaptureIfStmt(QsIfStmt ifStmt, QsCaptureContext context) 
        {
            var condResult = CaptureExp(ifStmt.CondExp, context);
            if (!condResult.HasValue) return null;
            context = condResult.Value;

            var bodyResult = CaptureStmt(ifStmt.BodyStmt, context);
            if (!bodyResult.HasValue) return null;
            context = bodyResult.Value;

            if (ifStmt.ElseBodyStmt != null)
            {
                var elseBodyResult = CaptureStmt(ifStmt.ElseBodyStmt, context);
                if (!elseBodyResult.HasValue) return null;
                context = elseBodyResult.Value;
            }

            return context;
        }

        QsCaptureContext? CaptureForStmtInitialize(QsForStmtInitializer forInitStmt, QsCaptureContext context)
        {
            return forInitStmt switch
            {
                QsVarDeclForStmtInitializer varDeclInit => CaptureVarDecl(varDeclInit.VarDecl, context),
                QsExpForStmtInitializer expInit => CaptureExp(expInit.Exp, context),
                _ => throw new NotImplementedException()
            };
        }

        QsCaptureContext? CaptureForStmt(QsForStmt forStmt, QsCaptureContext context)         
        {
            var prevBoundVars = context.BoundVars;

            if (forStmt.Initializer != null)
            {
                var initResult = CaptureForStmtInitialize(forStmt.Initializer, context);
                if (!initResult.HasValue) return null;
                context = initResult.Value;
            }

            if (forStmt.CondExp != null)
            {
                var condResult = CaptureExp(forStmt.CondExp, context);
                if (!condResult.HasValue) return null;
                context = condResult.Value;
            }

            if (forStmt.ContinueExp != null )
            {
                var contResult = CaptureExp(forStmt.ContinueExp, context);
                if (!contResult.HasValue) return null;
                context = contResult.Value;
            }

            var bodyResult = CaptureStmt(forStmt.BodyStmt, context);
            if (!bodyResult.HasValue) return null;
            context = bodyResult.Value;

            return context.UpdateBoundVars(prevBoundVars);
        }

        QsCaptureContext? CaptureContinueStmt(QsContinueStmt continueStmt, QsCaptureContext context) { return context; }
        QsCaptureContext? CaptureBreakStmt(QsBreakStmt breakStmt, QsCaptureContext context) { return context; }

        QsCaptureContext? CaptureReturnStmt(QsReturnStmt returnStmt, QsCaptureContext context)
        {
            if (returnStmt.Value != null)
                return CaptureExp(returnStmt.Value, context);
            else
                return context;
        }

        QsCaptureContext? CaptureBlockStmt(QsBlockStmt blockStmt, QsCaptureContext context) 
        {
            var prevBoundVars = context.BoundVars;

            foreach(var stmt in blockStmt.Stmts)
            {
                var stmtResult = CaptureStmt(stmt, context);
                if (!stmtResult.HasValue) return null;
                context = stmtResult.Value;
            }

            return context.UpdateBoundVars(prevBoundVars);
        }

        QsCaptureContext? CaptureExpStmt(QsExpStmt expStmt, QsCaptureContext context)
        {
            return CaptureExp(expStmt.Exp, context);
        }

        public QsCaptureContext? CaptureTaskStmt(QsTaskStmt stmt, QsCaptureContext context)
        {
            var prevBoundVars = context.BoundVars;

            var stmtResult = CaptureStmt(stmt.Body, context);
            if (!stmtResult.HasValue) return null;
            context = stmtResult.Value;

            return context.UpdateBoundVars(prevBoundVars);
        }

        public QsCaptureContext? CaptureStmt(QsStmt stmt, QsCaptureContext context)
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

                _ => throw new NotImplementedException()
            };
        }

        QsCaptureContext? RefCaptureIdExp(QsIdentifierExp idExp, QsCaptureContext context)
        {
            var varName = idExp.Value;

            // 바인드에 있는지 보고 
            if (!context.IsBound(varName))
            {
                // 캡쳐에 추가
                context = context.AddCapture(varName, QsCaptureContextCaptureKind.Ref);
            }

            return context;
        }

        QsCaptureContext? RefCaptureExp(QsExp exp, QsCaptureContext context)
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

                _ => throw new NotImplementedException()
            };
        }

        QsCaptureContext? CaptureIdExp(QsIdentifierExp idExp, QsCaptureContext context) 
        {            
            var varName = idExp.Value;

            // 바인드에 있는지 보고 
            if (!context.IsBound(varName))
            {
                // 캡쳐에 추가
                context = context.AddCapture(varName, QsCaptureContextCaptureKind.Copy);
            }

            return context;            
        }

        QsCaptureContext? CaptureBoolLiteralExp(QsBoolLiteralExp boolExp, QsCaptureContext context) => context;
        QsCaptureContext? CaptureIntLiteralExp(QsIntLiteralExp intExp, QsCaptureContext context) => context;
        QsCaptureContext? CaptureStringExp(QsStringExp stringExp, QsCaptureContext context)
        {
            var elemsResult = CaptureStringExpElements(stringExp.Elements, context);
            if (!elemsResult.HasValue) return null;
            return elemsResult.Value;
        }

        QsCaptureContext? CaptureUnaryOpExp(QsUnaryOpExp unaryOpExp, QsCaptureContext context) 
        {
            // ++i, i++은 ref를 유발한다
            if (unaryOpExp.Kind == QsUnaryOpKind.PostfixInc ||
                unaryOpExp.Kind == QsUnaryOpKind.PostfixDec ||
                unaryOpExp.Kind == QsUnaryOpKind.PrefixInc ||
                unaryOpExp.Kind == QsUnaryOpKind.PrefixDec)
                return RefCaptureExp(unaryOpExp.OperandExp, context);
            else
                return CaptureExp(unaryOpExp.OperandExp, context);
        }

        QsCaptureContext? CaptureBinaryOpExp(QsBinaryOpExp binaryOpExp, QsCaptureContext context) 
        { 
            if (binaryOpExp.Kind == QsBinaryOpKind.Assign)
            {
                var operandResult0 = RefCaptureExp(binaryOpExp.Operand0, context);
                if (!operandResult0.HasValue) return null;
                context = operandResult0.Value;
            }
            else
            {
                var operandResult0 = CaptureExp(binaryOpExp.Operand0, context);
                if (!operandResult0.HasValue) return null;
                context = operandResult0.Value;
            }

            var operandResult1 = CaptureExp(binaryOpExp.Operand1, context);
            if (!operandResult1.HasValue) return null;
            context = operandResult1.Value;

            return context;
        }

        QsCaptureContext? CaptureCallExp(QsCallExp callExp, QsCaptureContext context) 
        {
            var callableResult = callExp.Callable switch
            {
                QsFuncCallExpCallable funcCallable => context,
                QsExpCallExpCallable expCallable => CaptureExp(expCallable.Exp, context),
                _ => throw new NotImplementedException()
            };

            if (!callableResult.HasValue) return null;
            context = callableResult.Value;

            foreach (var arg in callExp.Args)
            {
                var argResult = CaptureExp(arg, context);
                if (!argResult.HasValue) return null;
                context = argResult.Value;
            }

            return context;
        }

        public QsCaptureContext? CaptureLambdaExp(QsLambdaExp exp, QsCaptureContext context)
        {
            var prevBoundVars = context.BoundVars;

            context = context.AddBinds(exp.Params.Select(param => param.Name));

            var stmtResult = CaptureStmt(exp.Body, context);
            if (!stmtResult.HasValue) return null;
            context = stmtResult.Value;

            return context.UpdateBoundVars(prevBoundVars);
        }
        
        QsCaptureContext? CaptureExp(QsExp exp, QsCaptureContext context)
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

                _ => throw new NotImplementedException()
            };
        }
    }
}
