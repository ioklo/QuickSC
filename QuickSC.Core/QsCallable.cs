using QuickSC.Syntax;
using System;
using System.Collections.Immutable;
using System.Threading.Tasks;

namespace QuickSC
{   
    public abstract class QsCallable
    {
    }

    public class QsFuncCallable : QsCallable
    {
        // TODO: Syntax직접 쓰지 않고, QsModule에서 정의한 것들을 써야 한다
        public QsFuncDecl FuncDecl { get; }
        public QsFuncCallable(QsFuncDecl funcDecl)
        {
            FuncDecl = funcDecl;
        }
    }

    public class QsLambdaCallable : QsCallable
    {
        // capture는 새로운 QsValue를 만들거나(value), 이전 QsValue를 그대로 가져와서 (ref-capture)
        public ImmutableDictionary<string, QsValue> Captures { get; }

        // TODO: Syntax직접 쓰지 않고, QsModule에서 정의한 것들을 써야 한다
        public QsLambdaExp Exp { get; }

        public QsLambdaCallable(QsLambdaExp exp, ImmutableDictionary<string, QsValue> captures)
        {
            Exp = exp;
            Captures = captures;
        }
    }

    public class QsNativeCallable : QsCallable
    {
        public Func<QsValue, ImmutableArray<QsValue>, QsEvalContext, ValueTask<QsEvalResult<QsValue>>> Invoker { get; }
        public QsNativeCallable(Func<QsValue, ImmutableArray<QsValue>, QsEvalContext, ValueTask<QsEvalResult<QsValue>>> invoker)
        {
            Invoker = invoker;
        }
    }
}

