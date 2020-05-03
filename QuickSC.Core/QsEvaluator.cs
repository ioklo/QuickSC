using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using QuickSC.Runtime;
using QuickSC.Syntax;

namespace QuickSC
{
    public struct QsEvalResult<TValue>
    {
        public static QsEvalResult<TValue> Invalid = new QsEvalResult<TValue>();

        public bool HasValue { get; }
        public TValue Value { get; }
        public QsEvalContext Context { get; }
        public QsEvalResult(TValue value, QsEvalContext context)
        {
            HasValue = true;
            Value = value;
            Context = context;
        }
    }

    // 레퍼런스용 Big Step Evaluator, 
    // TODO: Small Step으로 가야하지 않을까 싶다 (yield로 실행 point 잡는거 해보면 재미있을 것 같다)
    public class QsEvaluator
    {
        IQsCommandProvider commandProvider;
        QsEvalCapturer capturer;        

        public QsEvaluator(IQsCommandProvider commandProvider)
        {
            this.commandProvider = commandProvider;
            this.capturer = new QsEvalCapturer();
        }

        QsEvalResult<QsValue> EvaluateIdExp(QsIdentifierExp idExp, QsEvalContext context)
        {
            var result = context.GetValue(idExp.Value);

            // 없는 경우,
            if (result == null)
                return QsEvalResult<QsValue>.Invalid;

            // 초기화 되지 않은 경우는 QsNullValue를 머금고 리턴될 것이다
            return new QsEvalResult<QsValue>(result, context);
        }

        string? ToString(QsValue value)
        {
            if (value is QsObjectValue objValue && objValue.Object is QsStringObject strObj) return strObj.Data;
            if (value is QsValue<int> intValue) return intValue.Value.ToString();
            if (value is QsValue<bool> boolValue) return boolValue.Value ? "true" : "false";

            // TODO: ObjectValue의 경우 ToString()을 찾는다

            return null;
        }

        QsEvalResult<QsValue> EvaluateBoolLiteralExp(QsBoolLiteralExp boolLiteralExp, QsEvalContext context)
        {
            return new QsEvalResult<QsValue>(new QsValue<bool>(boolLiteralExp.Value), context);
        }

        QsEvalResult<QsValue> EvaluateIntLiteralExp(QsIntLiteralExp intLiteralExp, QsEvalContext context)
        {
            return new QsEvalResult<QsValue>(new QsValue<int>(intLiteralExp.Value), context);
        }

        async ValueTask<QsEvalResult<QsValue>> EvaluateStringExpAsync(QsStringExp stringExp, QsEvalContext context)
        {
            // stringExp는 element들의 concatenation
            var sb = new StringBuilder();
            foreach(var elem in stringExp.Elements)
            {
                switch (elem)
                {
                    case QsTextStringExpElement textElem:
                        sb.Append(textElem.Text);
                        break;

                    case QsExpStringExpElement expElem:
                        var result = await EvaluateExpAsync(expElem.Exp, context);
                        if (!result.HasValue)
                            return QsEvalResult<QsValue>.Invalid;

                        var strValue = ToString(result.Value);

                        if (strValue == null)
                            return QsEvalResult<QsValue>.Invalid;

                        sb.Append(strValue);
                        context = result.Context;
                        break;

                    default:
                        throw new InvalidOperationException();
                }
            }

            return new QsEvalResult<QsValue>(new QsObjectValue(new QsStringObject(sb.ToString())), context);
        }

        async ValueTask<QsEvalResult<QsValue>> EvaluateUnaryOpExpAsync(QsUnaryOpExp exp, QsEvalContext context)
        {
            switch(exp.Kind)
            {
                case QsUnaryOpKind.PostfixInc:  // i++
                    {
                        var operandResult = await EvaluateExpAsync(exp.OperandExp, context);
                        if (!operandResult.HasValue) return QsEvalResult<QsValue>.Invalid;

                        var intValue = operandResult.Value as QsValue<int>;
                        if (intValue == null) return QsEvalResult<QsValue>.Invalid;

                        var retValue = new QsValue<int>(intValue.Value);
                        intValue.Value++;
                        return new QsEvalResult<QsValue>(retValue, operandResult.Context);
                    }

                case QsUnaryOpKind.PostfixDec: 
                    {
                        var operandResult = await EvaluateExpAsync(exp.OperandExp, context);
                        if (!operandResult.HasValue) return QsEvalResult<QsValue>.Invalid;

                        var intValue = operandResult.Value as QsValue<int>;
                        if (intValue == null) return QsEvalResult<QsValue>.Invalid;

                        var retValue = new QsValue<int>(intValue.Value);
                        intValue.Value--;
                        return new QsEvalResult<QsValue>(retValue, operandResult.Context);
                    }

                case QsUnaryOpKind.LogicalNot:
                    {
                        var operandResult = await EvaluateExpAsync(exp.OperandExp, context);
                        if (!operandResult.HasValue) return QsEvalResult<QsValue>.Invalid;

                        var boolValue = operandResult.Value as QsValue<bool>;
                        if (boolValue == null) return QsEvalResult<QsValue>.Invalid;

                        return new QsEvalResult<QsValue>(new QsValue<bool>(!boolValue.Value), operandResult.Context);
                    }

                case QsUnaryOpKind.PrefixInc: 
                    {
                        var operandResult = await EvaluateExpAsync(exp.OperandExp, context);
                        if (!operandResult.HasValue) return QsEvalResult<QsValue>.Invalid;

                        var intValue = operandResult.Value as QsValue<int>;
                        if (intValue == null) return QsEvalResult<QsValue>.Invalid;

                        intValue.Value++;
                        return new QsEvalResult<QsValue>(operandResult.Value, operandResult.Context);
                    }

                case QsUnaryOpKind.PrefixDec:
                    {
                        var operandResult = await EvaluateExpAsync(exp.OperandExp, context);
                        if (!operandResult.HasValue) return QsEvalResult<QsValue>.Invalid;

                        var intValue = operandResult.Value as QsValue<int>;
                        if (intValue == null) return QsEvalResult<QsValue>.Invalid;

                        intValue.Value--;
                        return new QsEvalResult<QsValue>(operandResult.Value, operandResult.Context);
                    }
            }

            throw new NotImplementedException();
        }

        static string? GetStringValue(QsValue value)
        {
            if (value is QsObjectValue objValue && objValue.Object is QsStringObject strObj)
            {
                return strObj.Data;
            }

            return null;
        }        

        async ValueTask<QsEvalResult<QsValue>> EvaluateBinaryOpExpAsync(QsBinaryOpExp exp, QsEvalContext context)
        {
            var operandResult0 = await EvaluateExpAsync(exp.Operand0, context);
            if (!operandResult0.HasValue) return QsEvalResult<QsValue>.Invalid;
            context = operandResult0.Context;

            var operandResult1 = await EvaluateExpAsync(exp.Operand1, context);
            if (!operandResult1.HasValue) return QsEvalResult<QsValue>.Invalid;
            context = operandResult1.Context;

            switch (exp.Kind)
            {
                case QsBinaryOpKind.Multiply:
                    {
                        var intValue0 = operandResult0.Value as QsValue<int>;
                        if (intValue0 == null) return QsEvalResult<QsValue>.Invalid;

                        var intValue1 = operandResult1.Value as QsValue<int>;
                        if (intValue1 == null) return QsEvalResult<QsValue>.Invalid;

                        return new QsEvalResult<QsValue>(new QsValue<int>(intValue0.Value * intValue1.Value), context);
                    }

                case QsBinaryOpKind.Divide:
                    {
                        var intValue0 = operandResult0.Value as QsValue<int>;
                        if (intValue0 == null) return QsEvalResult<QsValue>.Invalid;

                        var intValue1 = operandResult1.Value as QsValue<int>;
                        if (intValue1 == null) return QsEvalResult<QsValue>.Invalid;

                        return new QsEvalResult<QsValue>(new QsValue<int>(intValue0.Value / intValue1.Value), context);
                    }

                case QsBinaryOpKind.Modulo:
                    {
                        var intValue0 = operandResult0.Value as QsValue<int>;
                        if (intValue0 == null) return QsEvalResult<QsValue>.Invalid;

                        var intValue1 = operandResult1.Value as QsValue<int>;
                        if (intValue1 == null) return QsEvalResult<QsValue>.Invalid;

                        return new QsEvalResult<QsValue>(new QsValue<int>(intValue0.Value % intValue1.Value), context);
                    }

                case QsBinaryOpKind.Add:
                    {
                        var intValue0 = operandResult0.Value as QsValue<int>;
                        if (intValue0 != null)
                        {
                            var intValue1 = operandResult1.Value as QsValue<int>;
                            if (intValue1 == null) return QsEvalResult<QsValue>.Invalid;

                            return new QsEvalResult<QsValue>(new QsValue<int>(intValue0.Value + intValue1.Value), context);
                        }

                        var strValue0 = GetStringValue(operandResult0.Value);
                        if( strValue0 != null)
                        {
                            var strValue1 = GetStringValue(operandResult1.Value);
                            if (strValue1 == null) return QsEvalResult<QsValue>.Invalid;

                            return new QsEvalResult<QsValue>(new QsObjectValue(new QsStringObject(strValue0 + strValue1)), context);
                        }

                        return QsEvalResult<QsValue>.Invalid;
                    }

                case QsBinaryOpKind.Subtract:
                    {
                        var intValue0 = operandResult0.Value as QsValue<int>;
                        if (intValue0 == null) return QsEvalResult<QsValue>.Invalid;

                        var intValue1 = operandResult1.Value as QsValue<int>;
                        if (intValue1 == null) return QsEvalResult<QsValue>.Invalid;

                        return new QsEvalResult<QsValue>(new QsValue<int>(intValue0.Value - intValue1.Value), context);
                    }

                case QsBinaryOpKind.LessThan:
                    {
                        var intValue0 = operandResult0.Value as QsValue<int>;
                        if (intValue0 != null)
                        {
                            var intValue1 = operandResult1.Value as QsValue<int>;
                            if (intValue1 == null) return QsEvalResult<QsValue>.Invalid;

                            return new QsEvalResult<QsValue>(new QsValue<bool>(intValue0.Value < intValue1.Value), context);
                        }

                        var strValue0 = GetStringValue(operandResult0.Value);
                        if (strValue0 != null)
                        {
                            var strValue1 = GetStringValue(operandResult1.Value);
                            if (strValue1 == null) return QsEvalResult<QsValue>.Invalid;

                            return new QsEvalResult<QsValue>(new QsValue<bool>(strValue0.CompareTo(strValue1) < 0), context);
                        }

                        return QsEvalResult<QsValue>.Invalid;
                    }

                case QsBinaryOpKind.GreaterThan:
                    {
                        var intValue0 = operandResult0.Value as QsValue<int>;
                        if (intValue0 != null)
                        {
                            var intValue1 = operandResult1.Value as QsValue<int>;
                            if (intValue1 == null) return QsEvalResult<QsValue>.Invalid;

                            return new QsEvalResult<QsValue>(new QsValue<bool>(intValue0.Value > intValue1.Value), context);
                        }

                        var strValue0 = GetStringValue(operandResult0.Value);
                        if (strValue0 != null)
                        {
                            var strValue1 = GetStringValue(operandResult1.Value);
                            if (strValue1 == null) return QsEvalResult<QsValue>.Invalid;

                            return new QsEvalResult<QsValue>(new QsValue<bool>(strValue0.CompareTo(strValue1) > 0), context);
                        }

                        return QsEvalResult<QsValue>.Invalid;
                    }

                case QsBinaryOpKind.LessThanOrEqual:
                    {
                        var intValue0 = operandResult0.Value as QsValue<int>;
                        if (intValue0 != null)
                        {
                            var intValue1 = operandResult1.Value as QsValue<int>;
                            if (intValue1 == null) return QsEvalResult<QsValue>.Invalid;

                            return new QsEvalResult<QsValue>(new QsValue<bool>(intValue0.Value <= intValue1.Value), context);
                        }

                        var strValue0 = GetStringValue(operandResult0.Value);
                        if (strValue0 != null)
                        {
                            var strValue1 = GetStringValue(operandResult1.Value);
                            if (strValue1 == null) return QsEvalResult<QsValue>.Invalid;

                            return new QsEvalResult<QsValue>(new QsValue<bool>(strValue0.CompareTo(strValue1) <= 0), context);
                        }

                        return QsEvalResult<QsValue>.Invalid;
                    }

                case QsBinaryOpKind.GreaterThanOrEqual:
                    {
                        var intValue0 = operandResult0.Value as QsValue<int>;
                        if (intValue0 != null)
                        {
                            var intValue1 = operandResult1.Value as QsValue<int>;
                            if (intValue1 == null) return QsEvalResult<QsValue>.Invalid;

                            return new QsEvalResult<QsValue>(new QsValue<bool>(intValue0.Value >= intValue1.Value), context);
                        }

                        var strValue0 = GetStringValue(operandResult0.Value);
                        if (strValue0 != null)
                        {
                            var strValue1 = GetStringValue(operandResult1.Value);
                            if (strValue1 == null) return QsEvalResult<QsValue>.Invalid;

                            return new QsEvalResult<QsValue>(new QsValue<bool>(strValue0.CompareTo(strValue1) >= 0), context);
                        }

                        return QsEvalResult<QsValue>.Invalid;
                    }

                case QsBinaryOpKind.Equal:
                    return new QsEvalResult<QsValue>(new QsValue<bool>(operandResult0.Value == operandResult1.Value), context);                    

                case QsBinaryOpKind.NotEqual:
                    return new QsEvalResult<QsValue>(new QsValue<bool>(operandResult0.Value != operandResult1.Value), context);

                case QsBinaryOpKind.Assign:
                    {
                        // TODO: 평가 순서가 operand1부터 해야 하지 않나
                        if (operandResult0.Value.SetValue(operandResult1.Value))
                            return new QsEvalResult<QsValue>(operandResult0.Value, context);
                        
                        return QsEvalResult<QsValue>.Invalid;
                    }

            }

            throw new NotImplementedException();
        }

        // TODO: QsFuncDecl을 직접 사용하지 않고, QsModule에서 정의한 Func을 사용해야 한다
        async ValueTask<QsEvalResult<QsCallable>> EvaluateCallExpCallableAsync(QsCallExpCallable callable, QsEvalContext context)
        {
            // 타입 체커를 통해서 미리 계산된 func가 있는 경우,
            if (callable is QsFuncCallExpCallable funcCallable)
            {
                return new QsEvalResult<QsCallable>(new QsFuncCallable(funcCallable.FuncDecl), context);
            }
            // TODO: 타입체커가 있으면 이 부분은 없어져야 한다
            else if (callable is QsExpCallExpCallable expCallable)            
            {                
                if (expCallable.Exp is QsIdentifierExp idExp)
                {
                    // 일단 idExp가 variable로 존재하는지 봐야 한다
                    if (!context.HasVar(idExp.Value))
                    {
                        var func = context.GetFunc(idExp.Value);
                        if (func == null)
                            return QsEvalResult<QsCallable>.Invalid;

                        return new QsEvalResult<QsCallable>(new QsFuncCallable(func), context);
                    }                    
                }

                var expCallableResult = await EvaluateExpAsync(expCallable.Exp, context);
                if (!expCallableResult.HasValue)
                    return QsEvalResult<QsCallable>.Invalid;
                context = expCallableResult.Context;

                if (expCallableResult.Value is QsObjectValue objValue && objValue.Object is QsLambdaObject lambdaObj)
                    return new QsEvalResult<QsCallable>(lambdaObj.Callable, context);

                return QsEvalResult<QsCallable>.Invalid;
            }

            return QsEvalResult<QsCallable>.Invalid;
        }

        async ValueTask<QsEvalResult<QsValue>> EvaluateFuncCallableAsync(QsFuncCallable callable, QsValue thisValue, ImmutableArray<QsValue> args, QsEvalContext context)
        {
            if (callable.FuncDecl.Params.Length != args.Length)
                return QsEvalResult<QsValue>.Invalid;

            // captures 
            var vars = ImmutableDictionary.CreateBuilder<string, QsValue>();

            for (int i = 0; i < args.Length; i++)
                vars.Add(callable.FuncDecl.Params[i].Name, args[i]);

            if (callable.FuncDecl.FuncKind == QsFuncKind.Sequence)
            {
                // context.. 여기 들어가 있어도 괜찮은걸까
                var asyncEnum = EvaluateSequenceCallAsync(callable.FuncDecl.Body, thisValue, vars.ToImmutable(), context);
                return new QsEvalResult<QsValue>(new QsObjectValue(new QsAsyncEnumerableObject(ToValue(asyncEnum))), context);
            }
            else
            {
                Debug.Assert(callable.FuncDecl.FuncKind == QsFuncKind.Normal);

                return await EvaluateNormalCallAsync(callable.FuncDecl.Body, thisValue, vars.ToImmutable(), context);                
            }

            static async IAsyncEnumerable<QsValue> ToValue(IAsyncEnumerable<QsEvalResult<QsValue>> e)
            {
                await foreach(var result in e)
                    yield return result.Value;
            }
        }

        async ValueTask<QsEvalResult<QsValue>> EvaluateLambdaCallableAsync(QsLambdaCallable callable, QsValue thisValue, ImmutableArray<QsValue> args, QsEvalContext context)
        {
            if (callable.Exp.Params.Length != args.Length)
                return QsEvalResult<QsValue>.Invalid;

            // captures 
            var vars = callable.Captures.ToBuilder();

            for (int i = 0; i < args.Length; i++)
                vars.Add(callable.Exp.Params[i].Name, args[i]);

            // TODO: 표현이 좀 이상하므로 다시 생각해볼 것
            return await EvaluateNormalCallAsync(callable.Exp.Body, thisValue, vars.ToImmutable(), context);                
        }

        async ValueTask<QsEvalResult<QsValue>> EvaluateCallableAsync(QsCallable callable, QsValue thisValue, ImmutableArray<QsValue> args, QsEvalContext context)
        {
            return callable switch
            {
                QsFuncCallable funcCallable => await EvaluateFuncCallableAsync(funcCallable, thisValue, args, context),
                QsLambdaCallable lambdaCallable => await EvaluateLambdaCallableAsync(lambdaCallable, thisValue, args, context),
                QsNativeCallable nativeCallable => await nativeCallable.Invoker(thisValue, args, context),
                _ => throw new NotImplementedException()
            };
        }

        async IAsyncEnumerable<QsEvalResult<QsValue>> EvaluateSequenceCallAsync(QsStmt body, QsValue thisValue, ImmutableDictionary<string, QsValue> vars, QsEvalContext context)
        {   
            // 프레임 전환 
            var (prevThisValue, prevVars, prevTasks) = (context.ThisValue, context.Vars, context.Tasks);
            
            context = context.SetThisValue(thisValue).SetVars(vars).SetTasks(ImmutableArray<Task>.Empty);

            // 현재 funcContext
            await foreach (var result in EvaluateStmtAsync(body, context))
            {
                if (!result.HasValue) { yield return QsEvalResult<QsValue>.Invalid; yield break; }

                context = result.Value;

                if (context.FlowControl is QsYieldEvalFlowControl yieldFlowControl)
                {
                    yield return new QsEvalResult<QsValue>(yieldFlowControl.Value, context);
                    context = context.SetFlowControl(QsNoneEvalFlowControl.Instance);
                }
            }

            // context = context.SetVars(prevVars).SetTasks(prevTasks).SetThisValue(prevThisValue);
        }

        async ValueTask<QsEvalResult<QsValue>> EvaluateNormalCallAsync(QsStmt body, QsValue thisValue, ImmutableDictionary<string, QsValue> vars, QsEvalContext context)
        {
            // 프레임 전환 
            var (prevThisValue, prevVars, prevTasks) = (context.ThisValue, context.Vars, context.Tasks);

            context = context.SetThisValue(thisValue).SetVars(vars).SetTasks(ImmutableArray<Task>.Empty);

            // 현재 funcContext
            await foreach (var result in EvaluateStmtAsync(body, context))
            {
                if (!result.HasValue) return QsEvalResult<QsValue>.Invalid;

                context = result.Value;

                if (context.FlowControl is QsYieldEvalFlowControl yieldFlowControl)
                    throw new InvalidOperationException();
            }

            context = context.SetVars(prevVars).SetTasks(prevTasks).SetThisValue(prevThisValue);

            if (context.FlowControl is QsReturnEvalFlowControl returnFlowControl)
            {
                context = context.SetFlowControl(QsNoneEvalFlowControl.Instance);
                return new QsEvalResult<QsValue>(returnFlowControl.Value, context);
            }
            else
            {
                context = context.SetFlowControl(QsNoneEvalFlowControl.Instance);
                return new QsEvalResult<QsValue>(QsNullValue.Instance, context);
            }
        }

        async ValueTask<QsEvalResult<QsValue>> EvaluateCallExpAsync(QsCallExp exp, QsEvalContext context)
        {
            var callableResult = await EvaluateCallExpCallableAsync(exp.Callable, context);
            if (!callableResult.HasValue)
                return QsEvalResult<QsValue>.Invalid;
            context = callableResult.Context;

            // 
            var args = ImmutableArray.CreateBuilder<QsValue>(exp.Args.Length);
            foreach (var argExp in exp.Args)
            {
                var argResult = await EvaluateExpAsync(argExp, context);
                if (!argResult.HasValue)
                    return QsEvalResult<QsValue>.Invalid;
                context = argResult.Context;

                args.Add(argResult.Value);
            }

            return await EvaluateCallableAsync(callableResult.Value, QsNullValue.Instance, args.ToImmutable(), context);
        }

        QsEvalResult<QsValue> EvaluateLambdaExp(QsLambdaExp exp, QsEvalContext context)
        {
            var captureResult = capturer.CaptureLambdaExp(exp, QsCaptureContext.Make());
            if( !captureResult.HasValue )
                return QsEvalResult<QsValue>.Invalid;

            var captures = ImmutableDictionary.CreateBuilder<string, QsValue>();
            foreach(var needCapture in captureResult.Value.NeedCaptures)            
            {
                var name = needCapture.Key;
                var kind = needCapture.Value;

                var origValue = context.GetValue(name);
                if (origValue == null) return QsEvalResult<QsValue>.Invalid;

                QsValue value;
                if( kind == QsCaptureContextCaptureKind.Copy )
                {   
                    value = origValue.MakeCopy();
                }
                else 
                {
                    Debug.Assert(kind == QsCaptureContextCaptureKind.Ref);
                    value = origValue;
                }

                captures.Add(name, value);
            }

            // QsValue<QsCallable>을 리턴한다
            return new QsEvalResult<QsValue>(
                new QsObjectValue(new QsLambdaObject(new QsLambdaCallable(exp, captures.ToImmutable()))), 
                context);
        }
        
        async ValueTask<QsEvalResult<QsValue>> EvaluateMemberCallExpAsync(QsMemberCallExp exp, QsEvalContext context)
        {
            // a.b (2, 3, 4)
            var objResult = await EvaluateExpAsync(exp.Object, context);
            if (!objResult.HasValue) return QsEvalResult<QsValue>.Invalid;
            context = objResult.Context;

            var callable = objResult.Value.GetMemberFuncs(exp.MemberFuncId);
            if (callable == null) return QsEvalResult<QsValue>.Invalid;

            // 
            var args = ImmutableArray.CreateBuilder<QsValue>(exp.Args.Length);
            foreach (var argExp in exp.Args)
            {
                var argResult = await EvaluateExpAsync(argExp, context);
                if (!argResult.HasValue) return QsEvalResult<QsValue>.Invalid;
                context = argResult.Context;

                args.Add(argResult.Value);
            }

            return await EvaluateCallableAsync(callable, objResult.Value, args.ToImmutable(), context);
        }

        async ValueTask<QsEvalResult<QsValue>> EvaluateMemberExpAsync(QsMemberExp exp, QsEvalContext context)
        {
            var objResult = await EvaluateExpAsync(exp.Object, context);
            if (!objResult.HasValue) return QsEvalResult<QsValue>.Invalid;
            context = objResult.Context;

            var value = objResult.Value.GetMemberValue(exp.MemberName);
            if (value == null) return QsEvalResult<QsValue>.Invalid;

            return new QsEvalResult<QsValue>(value, context);
        }

        async ValueTask<QsEvalResult<QsValue>> EvaluateListExpAsync(QsListExp listExp, QsEvalContext context)
        {
            var elems = new List<QsValue>(listExp.Elems.Length);

            foreach(var elemExp in listExp.Elems)
            {
                var elemResult = await EvaluateExpAsync(elemExp, context);
                if (!elemResult.HasValue) return QsEvalResult<QsValue>.Invalid;
                context = elemResult.Context;

                elems.Add(elemResult.Value);
            }

            return new QsEvalResult<QsValue>(new QsObjectValue(new QsListObject(elems)), context);
        }

        async ValueTask<QsEvalResult<QsValue>> EvaluateExpAsync(QsExp exp, QsEvalContext context)
        {
            return exp switch
            {
                QsIdentifierExp idExp => EvaluateIdExp(idExp, context),
                QsBoolLiteralExp boolExp => EvaluateBoolLiteralExp(boolExp, context),
                QsIntLiteralExp intExp => EvaluateIntLiteralExp(intExp, context),
                QsStringExp stringExp => await EvaluateStringExpAsync(stringExp, context),
                QsUnaryOpExp unaryOpExp => await EvaluateUnaryOpExpAsync(unaryOpExp, context),
                QsBinaryOpExp binaryOpExp => await EvaluateBinaryOpExpAsync(binaryOpExp, context),
                QsCallExp callExp => await EvaluateCallExpAsync(callExp, context),
                QsLambdaExp lambdaExp => EvaluateLambdaExp(lambdaExp, context),
                QsMemberCallExp memberCallExp => await EvaluateMemberCallExpAsync(memberCallExp, context),
                QsMemberExp memberExp => await EvaluateMemberExpAsync(memberExp, context),
                QsListExp listExp => await EvaluateListExpAsync(listExp, context),

                _ => throw new NotImplementedException()
            };
        }

        // TODO: CommandProvider가 Parser도 제공해야 할 것 같다
        internal async ValueTask<QsEvalContext?> EvaluateCommandStmtAsync(QsCommandStmt stmt, QsEvalContext context)
        {
            foreach (var command in stmt.Commands)
            {
                var cmdResult = await EvaluateStringExpAsync(command, context);
                if (!cmdResult.HasValue) return null;
                context = cmdResult.Context;

                var cmdText = ToString(cmdResult.Value);
                if (cmdText == null) return null;

                await commandProvider.ExecuteAsync(cmdText);
            }
            return context;
        }

        internal async ValueTask<QsEvalContext?> EvaluateVarDeclStmtAsync(QsVarDeclStmt stmt, QsEvalContext context)
        {
            return await EvaluateVarDeclAsync(stmt.VarDecl, context);
        }

        internal async ValueTask<QsEvalContext?> EvaluateVarDeclAsync(QsVarDecl varDecl, QsEvalContext context)
        {
            foreach(var elem in varDecl.Elements)
            {
                QsValue value;
                if (elem.InitExp != null)
                {
                    var expResult = await EvaluateExpAsync(elem.InitExp, context);
                    if (!expResult.HasValue)
                        return null;

                    value = expResult.Value;
                    context = expResult.Context;
                }
                else
                {
                    value = QsNullValue.Instance;
                }

                context = context.SetValue(elem.VarName, value);
            }

            return context;
        }

        internal async IAsyncEnumerable<QsEvalContext?> EvaluateIfStmtAsync(QsIfStmt stmt, QsEvalContext context)
        {
            var condValue = await EvaluateExpAsync(stmt.CondExp, context);
            if (!condValue.HasValue) { yield return null; yield break; }

            var condBoolValue = condValue.Value as QsValue<bool>;
            if (condBoolValue == null) { yield return null; yield break; }

            context = condValue.Context;

            if (condBoolValue.Value)
            {
                await foreach (var result in EvaluateStmtAsync(stmt.BodyStmt, context))
                {
                    if (!result.HasValue)
                    {
                        yield return null;
                        yield break;
                    }

                    context = result.Value;
                    if (context.FlowControl is QsYieldEvalFlowControl)
                    {
                        yield return context;
                        context = context.SetFlowControl(QsNoneEvalFlowControl.Instance);
                    }
                }
            }
            else
            {
                if (stmt.ElseBodyStmt != null)
                {
                    await foreach (var result in EvaluateStmtAsync(stmt.ElseBodyStmt, context))
                    {
                        if (!result.HasValue)
                        {
                            yield return null;
                            yield break;
                        }

                        context = result.Value;
                        if (context.FlowControl is QsYieldEvalFlowControl)
                        {
                            yield return context;
                            context = context.SetFlowControl(QsNoneEvalFlowControl.Instance);
                        }
                    }
                }
            }

            yield return context;
        }

        internal async IAsyncEnumerable<QsEvalContext?> EvaluateForStmtAsync(QsForStmt forStmt, QsEvalContext context)
        {
            var prevVars = context.Vars;

            switch (forStmt.Initializer)
            {
                case QsExpForStmtInitializer expInitializer:
                    {
                        var valueResult = await EvaluateExpAsync(expInitializer.Exp, context);
                        if (!valueResult.HasValue) { yield return null; yield break; }
                        context = valueResult.Context;
                        break;
                    }
                case QsVarDeclForStmtInitializer varDeclInitializer:
                    {
                        var evalResult = await EvaluateVarDeclAsync(varDeclInitializer.VarDecl, context);
                        if (!evalResult.HasValue) { yield return null; yield break; }
                        context = evalResult.Value;
                        break;
                    }

                case null:
                    break;

                default:
                    throw new NotImplementedException();
            }

            while (true)
            {
                if (forStmt.CondExp != null)
                {
                    var condExpResult = await EvaluateExpAsync(forStmt.CondExp, context);
                    if (!condExpResult.HasValue)
                    {
                        yield return null;
                        yield break;
                    }

                    var condExpBoolValue = condExpResult.Value as QsValue<bool>;
                    if (condExpBoolValue == null)
                    {
                        yield return null;
                        yield break;
                    }

                    context = condExpResult.Context;
                    if (!condExpBoolValue.Value)
                        break;
                }
                
                await foreach(var result in EvaluateStmtAsync(forStmt.BodyStmt, context))
                {
                    if (!result.HasValue)
                    {
                        yield return null;
                        yield break;
                    }

                    context = result.Value;
                    if (context.FlowControl is QsYieldEvalFlowControl)
                    {
                        yield return context;
                        context = context.SetFlowControl(QsNoneEvalFlowControl.Instance);
                    }
                }

                if (context.FlowControl == QsBreakEvalFlowControl.Instance)
                {
                    context = context.SetFlowControl(QsNoneEvalFlowControl.Instance);
                    break;
                }
                else if (context.FlowControl == QsContinueEvalFlowControl.Instance)
                {
                    context = context.SetFlowControl(QsNoneEvalFlowControl.Instance);
                }
                else if (context.FlowControl is QsReturnEvalFlowControl)
                {
                    break;
                }
                else
                {
                    Debug.Assert(context.FlowControl == QsNoneEvalFlowControl.Instance);
                }

                if (forStmt.ContinueExp != null)
                {
                    var contExpResult = await EvaluateExpAsync(forStmt.ContinueExp, context);
                    if (!contExpResult.HasValue)
                    {
                        yield return null;
                        yield break;
                    }

                    context = contExpResult.Context;
                }
            }

            yield return context.SetVars(prevVars);
        }

        internal QsEvalContext? EvaluateContinueStmt(QsContinueStmt continueStmt, QsEvalContext context)
        {
            return context.SetFlowControl(QsContinueEvalFlowControl.Instance);
        }

        internal QsEvalContext? EvaluateBreakStmt(QsBreakStmt breakStmt, QsEvalContext context)
        {
            return context.SetFlowControl(QsBreakEvalFlowControl.Instance);
        }

        internal async ValueTask<QsEvalContext?> EvaluateReturnStmtAsync(QsReturnStmt returnStmt, QsEvalContext context)
        {
            QsValue returnValue;
            if (returnStmt.Value != null)
            {
                var returnValueResult = await EvaluateExpAsync(returnStmt.Value, context);
                if (!returnValueResult.HasValue)
                    return null;

                returnValue = returnValueResult.Value;
            }
            else
            {
                returnValue = QsNullValue.Instance;
            }

            return context.SetFlowControl(new QsReturnEvalFlowControl(returnValue));
        }

        internal async IAsyncEnumerable<QsEvalContext?> EvaluateBlockStmtAsync(QsBlockStmt blockStmt, QsEvalContext context)
        {
            var prevVars = context.Vars;

            foreach(var stmt in blockStmt.Stmts)
            {
                await foreach (var result in EvaluateStmtAsync(stmt, context))
                {
                    if (!result.HasValue)
                    {
                        yield return null;
                        yield break;
                    }

                    context = result.Value;
                    if (context.FlowControl is QsYieldEvalFlowControl)
                    {
                        yield return context;
                        context = context.SetFlowControl(QsNoneEvalFlowControl.Instance);
                    }
                }

                if (context.FlowControl != QsNoneEvalFlowControl.Instance)
                {
                    yield return context.SetVars(prevVars);
                    yield break;
                }
            }

            yield return context.SetVars(prevVars);
        }

        internal async ValueTask<QsEvalContext?> EvaluateExpStmtAsync(QsExpStmt expStmt, QsEvalContext context)
        {
            var expResult = await EvaluateExpAsync(expStmt.Exp, context);
            if (!expResult.HasValue) return null;

            return expResult.Context;
        }

        internal QsEvalContext? EvaluateTaskStmt(QsTaskStmt taskStmt, QsEvalContext context)
        {
            var captureResult = capturer.CaptureStmt(taskStmt.Body, QsCaptureContext.Make());
            if (!captureResult.HasValue) return null;

            var captures = ImmutableDictionary.CreateBuilder<string, QsValue>();
            foreach (var needCapture in captureResult.Value.NeedCaptures)
            {
                var name = needCapture.Key;
                var kind = needCapture.Value;

                var origValue = context.GetValue(name);
                if (origValue == null) return null;

                QsValue value;
                if (kind == QsCaptureContextCaptureKind.Copy)
                {
                    value = origValue.MakeCopy();
                }
                else
                {
                    Debug.Assert(kind == QsCaptureContextCaptureKind.Ref);
                    value = origValue;
                }

                captures.Add(name, value);
            }

            var newContext = QsEvalContext.Make();
            newContext = newContext.SetVars(captures.ToImmutable());

            var task = Task.Run(async () =>
            {
                await foreach (var result in EvaluateStmtAsync(taskStmt.Body, context))
                {
                    if (!result.HasValue) return;
                    context = result.Value;
                }
            });

            return context.AddTask(task);
        }

        public async IAsyncEnumerable<QsEvalContext?> EvaluateAwaitStmtAsync(QsAwaitStmt stmt, QsEvalContext context)
        {
            var prevTasks = context.Tasks;
            var prevVars = context.Vars;
            context = context.SetTasks(ImmutableArray<Task>.Empty);

            await foreach (var result in EvaluateStmtAsync(stmt.Body, context))
            {
                if (!result.HasValue)
                {
                    yield return null;
                    yield break;
                }

                context = result.Value;
                if (context.FlowControl is QsYieldEvalFlowControl)
                {
                    yield return context;
                    context = context.SetFlowControl(QsNoneEvalFlowControl.Instance);
                }
            }

            await Task.WhenAll(context.Tasks.ToArray());

            yield return context.SetTasks(prevTasks).SetVars(prevVars);
        }

        internal QsEvalContext? EvaluateAsyncStmt(QsAsyncStmt taskStmt, QsEvalContext context)
        {
            var captureResult = capturer.CaptureStmt(taskStmt.Body, QsCaptureContext.Make());
            if (!captureResult.HasValue) return null;

            var captures = ImmutableDictionary.CreateBuilder<string, QsValue>();
            foreach (var needCapture in captureResult.Value.NeedCaptures)
            {
                var name = needCapture.Key;
                var kind = needCapture.Value;

                var origValue = context.GetValue(name);
                if (origValue == null) return null;

                QsValue value;
                if (kind == QsCaptureContextCaptureKind.Copy)
                {
                    value = origValue.MakeCopy();
                }
                else
                {
                    Debug.Assert(kind == QsCaptureContextCaptureKind.Ref);
                    value = origValue;
                }

                captures.Add(name, value);
            }

            var newContext = QsEvalContext.Make();
            newContext = newContext.SetVars(captures.ToImmutable());

            Func<Task> asyncFunc = async () =>
            {
                await foreach (var result in EvaluateStmtAsync(taskStmt.Body, newContext))
                {
                    if (!result.HasValue) return;
                    context = result.Value;
                }
            };

            var task = asyncFunc();
            return context.AddTask(task);
        }

        internal async IAsyncEnumerable<QsEvalContext?> EvaluateForeachStmtAsync(QsForeachStmt foreachStmt, QsEvalContext context)
        {
            var prevVars = context.Vars;
            
            var expResult = await EvaluateExpAsync(foreachStmt.Obj, context);
            if (!expResult.HasValue) { yield return null; yield break; }
            context = expResult.Context;

            var objValue = expResult.Value as QsObjectValue;
            if (objValue == null) { yield return null; yield break; }

            var callable = objValue.GetMemberFuncs(new QsMemberFuncId("GetEnumerator"));
            if (callable == null) { yield return null; yield break; }

            var callableResult = await EvaluateCallableAsync(callable, objValue, ImmutableArray<QsValue>.Empty, context);
            if (!callableResult.HasValue) { yield return null; yield break; }
            context = callableResult.Context;

            var enumeratorValue = callableResult.Value as QsObjectValue;
            if (enumeratorValue == null) { yield return null; yield break; }

            var moveNextFunc = enumeratorValue.GetMemberFuncs(new QsMemberFuncId("MoveNext"));
            if (moveNextFunc == null) { yield return null; yield break; }

            var getCurrentFunc = enumeratorValue.GetMemberFuncs(new QsMemberFuncId("GetCurrent"));
            if (getCurrentFunc == null) { yield return null; yield break; }

            while (true)
            {
                var moveNextResult = await EvaluateCallableAsync(moveNextFunc, enumeratorValue, ImmutableArray<QsValue>.Empty, context);
                if (!moveNextResult.HasValue) { yield return null; yield break; }
                context = moveNextResult.Context;

                if (!(moveNextResult.Value is QsValue<bool> moveNextReturn)) { yield return null; yield break; }

                if (!moveNextReturn.Value) break;

                // GetCurrent
                var getCurrentResult = await EvaluateCallableAsync(getCurrentFunc, enumeratorValue, ImmutableArray<QsValue>.Empty, context);
                if (!getCurrentResult.HasValue) { yield return null; yield break; }
                context = getCurrentResult.Context;

                // NOTICE: COPY
                context = context.SetValue(foreachStmt.VarName, getCurrentResult.Value.MakeCopy());

                await foreach (var result in EvaluateStmtAsync(foreachStmt.Body, context))
                {
                    if (!result.HasValue) { yield return null; yield break; }

                    context = result.Value;
                    if (context.FlowControl is QsYieldEvalFlowControl)
                    {
                        yield return context;
                        context = context.SetFlowControl(QsNoneEvalFlowControl.Instance);
                    }
                }

                if (context.FlowControl == QsBreakEvalFlowControl.Instance)
                {
                    context = context.SetFlowControl(QsNoneEvalFlowControl.Instance);
                    break;
                }
                else if (context.FlowControl == QsContinueEvalFlowControl.Instance)
                {
                    context = context.SetFlowControl(QsNoneEvalFlowControl.Instance);
                }
                else if (context.FlowControl is QsReturnEvalFlowControl)
                {
                    break;
                }
                else
                {
                    Debug.Assert(context.FlowControl == QsNoneEvalFlowControl.Instance);
                }
            }

            yield return context.SetVars(prevVars);
        }

        async IAsyncEnumerable<QsEvalContext?> EvaluateYieldStmtAsync(QsYieldStmt yieldStmt, QsEvalContext context)
        {
            QsValue yieldValue;

            var yieldValueResult = await EvaluateExpAsync(yieldStmt.Value, context);
            if (!yieldValueResult.HasValue) { yield return null; yield break; }

            yieldValue = yieldValueResult.Value;
            
            yield return context.SetFlowControl(new QsYieldEvalFlowControl(yieldValue));
        }

        // TODO: 임시 public, REPL용이 따로 있어야 할 것 같다
        public async IAsyncEnumerable<QsEvalContext?> EvaluateStmtAsync(QsStmt stmt, QsEvalContext context)
        {
            switch(stmt)
            {
                case QsCommandStmt cmdStmt: yield return await EvaluateCommandStmtAsync(cmdStmt, context); break;
                case QsVarDeclStmt varDeclStmt: yield return await EvaluateVarDeclStmtAsync(varDeclStmt, context); break;
                case QsIfStmt ifStmt: 
                    await foreach(var result in EvaluateIfStmtAsync(ifStmt, context))
                        yield return result; 
                    break;

                case QsForStmt forStmt:
                    await foreach (var result in EvaluateForStmtAsync(forStmt, context))
                        yield return result;
                    break;

                case QsContinueStmt continueStmt: EvaluateContinueStmt(continueStmt, context); break;
                case QsBreakStmt breakStmt: EvaluateBreakStmt(breakStmt, context); break;
                case QsReturnStmt returnStmt: await EvaluateReturnStmtAsync(returnStmt, context); break;
                case QsBlockStmt blockStmt:
                    await foreach (var result in EvaluateBlockStmtAsync(blockStmt, context))
                        yield return result;
                    break;

                case QsExpStmt expStmt: await EvaluateExpStmtAsync(expStmt, context); break;
                case QsTaskStmt taskStmt: EvaluateTaskStmt(taskStmt, context); break;
                case QsAwaitStmt awaitStmt:
                    await foreach (var result in EvaluateAwaitStmtAsync(awaitStmt, context))
                        yield return result;
                    break; 
                    
                case QsAsyncStmt asyncStmt: EvaluateAsyncStmt(asyncStmt, context); break;
                case QsForeachStmt foreachStmt:
                    await foreach (var result in EvaluateForeachStmtAsync(foreachStmt, context))
                        yield return result;
                    break;

                case QsYieldStmt yieldStmt:
                    await foreach (var result in EvaluateYieldStmtAsync(yieldStmt, context))
                        yield return result;
                    break;
                    
                default: throw new NotImplementedException();
            };
        }
        
        public async ValueTask<QsEvalContext?> EvaluateScriptAsync(QsScript script, QsEvalContext context)
        {
            // decl 부터 먼저 처리
            foreach (var elem in script.Elements)
            {
                if (elem is QsFuncDeclScriptElement funcDeclElem)
                {
                    context = context.AddFunc(funcDeclElem.FuncDecl);
                }
            }

            foreach(var elem in script.Elements)
            {
                if (elem is QsStmtScriptElement statementElem)
                {
                    await foreach (var result in EvaluateStmtAsync(statementElem.Stmt, context))
                    {
                        if (!result.HasValue) return null;
                        context = result.Value;
                    }
                }
                else if (elem is QsFuncDeclScriptElement funcDeclElem)
                {
                    continue;
                }
                else 
                {
                    return null;
                }
            }

            return context;
        }
    }
}