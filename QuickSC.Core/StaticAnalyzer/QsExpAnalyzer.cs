using QuickSC;
using QuickSC.Syntax;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;
using QsAnalyzeResult = QuickSC.QsResult<QuickSC.QsType, QuickSC.StaticAnalyzer.QsAnalyzerContext>;
using static QuickSC.QsResult<QuickSC.QsType, QuickSC.StaticAnalyzer.QsAnalyzerContext>;

namespace QuickSC.StaticAnalyzer
{
    // 어떤 Exp에서 타입 정보 등을 알아냅니다
    class QsExpAnalyzer
    {
        IQsTypeManager typeManager;

        public QsExpAnalyzer(IQsTypeManager typeManager)
        {
            this.typeManager = typeManager;
        }

        QsAnalyzeResult ResolveIdExp(QsIdentifierExp idExp, QsAnalyzerContext context) { throw new NotImplementedException(); }
        QsAnalyzeResult ResolveBoolLiteralExp(QsBoolLiteralExp boolExp, QsAnalyzerContext context) 
        {
            return Result(typeManager.GetBoolType(), context);
        }

        QsAnalyzeResult ResolveIntLiteralExp(QsIntLiteralExp intExp, QsAnalyzerContext context)
        {
            return Result(typeManager.GetIntType(), context);
        }

        QsAnalyzeResult ResolveStringExp(QsStringExp stringExp, QsAnalyzerContext context)
        {
            return Result(typeManager.GetStringType(), context);
        }

        QsAnalyzeResult ResolveUnaryOpExp(QsUnaryOpExp unaryOpExp, QsAnalyzerContext context) 
        { 
            switch(unaryOpExp.Kind)
            {
                case QsUnaryOpKind.LogicalNot:

            }
            throw new NotImplementedException(); 
        }

        QsAnalyzeResult ResolveBinaryOpExp(QsBinaryOpExp binaryOpExp, QsAnalyzerContext context) { throw new NotImplementedException(); }
        QsAnalyzeResult ResolveCallExp(QsCallExp callExp, QsAnalyzerContext context) { throw new NotImplementedException(); }
        QsAnalyzeResult ResolveLambdaExp(QsLambdaExp lambdaExp, QsAnalyzerContext context) { throw new NotImplementedException(); }
        QsAnalyzeResult ResolveMemberCallExp(QsMemberCallExp memberCallExp, QsAnalyzerContext context) { throw new NotImplementedException(); }
        QsAnalyzeResult ResolveMemberExp(QsMemberExp memberExp, QsAnalyzerContext context) { throw new NotImplementedException(); }
        QsAnalyzeResult ResolveListExp(QsListExp listExp, QsAnalyzerContext context) { throw new NotImplementedException(); }

        public QsAnalyzeResult ResolveExp(QsExp exp, QsAnalyzerContext context)
        {
            return exp switch
            {
                QsIdentifierExp idExp => ResolveIdExp(idExp, context),
                QsBoolLiteralExp boolExp => ResolveBoolLiteralExp(boolExp, context),
                QsIntLiteralExp intExp => ResolveIntLiteralExp(intExp, context),
                QsStringExp stringExp => ResolveStringExp(stringExp, context),
                QsUnaryOpExp unaryOpExp => ResolveUnaryOpExp(unaryOpExp, context),
                QsBinaryOpExp binaryOpExp => ResolveBinaryOpExp(binaryOpExp, context),
                QsCallExp callExp => ResolveCallExp(callExp, context),
                QsLambdaExp lambdaExp => ResolveLambdaExp(lambdaExp, context),
                QsMemberCallExp memberCallExp => ResolveMemberCallExp(memberCallExp, context),
                QsMemberExp memberExp => ResolveMemberExp(memberExp, context),
                QsListExp listExp => ResolveListExp(listExp, context),

                _ => throw new NotImplementedException()
            };
        }
    }
}
