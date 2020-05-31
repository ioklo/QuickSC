using QuickSC.Syntax;
using System;
using System.Collections.Generic;
using System.Text;

namespace QuickSC.StaticAnalyzer
{
    public abstract class QsScriptFuncTemplate
    {
        public class FuncDecl : QsScriptFuncTemplate
        {
            public QsTypeValue? SeqElemTypeValue { get; }
            public bool bThisCall { get; }
            public int LocalVarCount { get; }
            public QsStmt Body { get; }

            public FuncDecl(QsTypeValue? seqElemTypeValue, bool bThisCall, int localVarCount, QsStmt body)
            {
                SeqElemTypeValue = seqElemTypeValue;
                this.bThisCall = bThisCall;
                LocalVarCount = localVarCount;
                Body = body;
            }
        }

        //public class EnumDeclElem : QsScriptFuncTemplate
        //{
        //    public QsEnumDeclElement DeclElem { get; }
        //    public EnumDeclElem(QsEnumDeclElement declElem)
        //    {
        //        DeclElem = declElem;
        //    }
        //}
    }

}
