using QuickSC.Syntax;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

namespace QuickSC.TypeExpEvaluator
{
    // TypeExp를 TypeValue로 바꿉니다
    class QsTypeExpEvaluator
    {
        public QsTypeValue? Evaluate(QsTypeExp exp, QsTypeEvalContext context)
        {
            switch (exp)
            {
                case QsIdTypeExp idExp:
                    return context.GetTypeValue(idExp.Name);

                case QsMemberTypeExp memberExp:
                    {
                        var parentTypeValue = Evaluate(memberExp.Parent, context);
                        if (parentTypeValue == null) return null;

                        var typeArgs = ImmutableArray.CreateBuilder<QsTypeValue>(memberExp.TypeArgs.Length);
                        foreach(var typeArg in memberExp.TypeArgs)
                        {
                            var typeArgTypeValue = Evaluate(typeArg, context);
                            if (typeArgTypeValue == null) return null;

                            typeArgs.Add(typeArgTypeValue);
                        }

                        return parentTypeValue.GetMemberTypeValue(memberExp.MemberName, typeArgs.MoveToImmutable());
                    }

                default:
                    return null;
            }
        }
    }
}
