using System;
using System.Collections.Generic;
using System.Text;

namespace QuickSC.Runtime
{
    // Lambda
    public class QsLambdaObject : QsObject
    {
        public QsLambdaCallable Callable { get; }

        public QsLambdaObject(QsLambdaCallable callable)
        {
            this.Callable = callable;
        }
    }
}
