using QuickSC.Syntax;
using System;
using System.Collections.Generic;
using System.Text;

namespace QuickSC.StaticAnalyzer
{
    public abstract class QsScriptFuncTemplate
    {
        public QsMetaItemId FuncId { get; }

        public class FuncDecl : QsScriptFuncTemplate
        {
            public QsTypeValue? SeqElemTypeValue { get; }
            public bool bThisCall { get; }
            public int LocalVarCount { get; }
            public QsStmt Body { get; }

            internal FuncDecl(QsMetaItemId funcId, QsTypeValue? seqElemTypeValue, bool bThisCall, int localVarCount, QsStmt body)
                : base(funcId)
            {
                SeqElemTypeValue = seqElemTypeValue;
                this.bThisCall = bThisCall;
                LocalVarCount = localVarCount;
                Body = body;
            }
        }

        public QsScriptFuncTemplate(QsMetaItemId funcId)
        {
            FuncId = funcId;
        }

        public static FuncDecl MakeFuncDecl(QsMetaItemId funcId, QsTypeValue? seqElemTypeValue, bool bThisCall, int localVarCount, QsStmt body)
            => new FuncDecl(funcId, seqElemTypeValue, bThisCall, localVarCount, body);

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
