using QuickSC.Syntax;
using System;
using System.Collections.Generic;
using System.Text;

namespace QuickSC.StaticAnalyzer
{
    public abstract class QsScriptTemplate
    {
        public QsMetaItemId Id { get; }

        public class Func : QsScriptTemplate
        {
            public QsTypeValue? SeqElemTypeValue { get; }
            public bool bThisCall { get; }
            public int LocalVarCount { get; }
            public QsStmt Body { get; }

            internal Func(QsMetaItemId funcId, QsTypeValue? seqElemTypeValue, bool bThisCall, int localVarCount, QsStmt body)
                : base(funcId)
            {
                SeqElemTypeValue = seqElemTypeValue;
                this.bThisCall = bThisCall;
                LocalVarCount = localVarCount;
                Body = body;
            }
        }

        public class Enum : QsScriptTemplate
        {
            public string DefaultElemName { get; }
            public Enum(QsMetaItemId enumId, string defaultElemName)
                : base(enumId)
            {
                DefaultElemName = defaultElemName;
            }
        }

        public QsScriptTemplate(QsMetaItemId funcId)
        {
            Id = funcId;
        }

        public static Func MakeFunc(QsMetaItemId funcId, QsTypeValue? seqElemTypeValue, bool bThisCall, int localVarCount, QsStmt body)
            => new Func(funcId, seqElemTypeValue, bThisCall, localVarCount, body);

        public static Enum MakeEnum(QsMetaItemId enumId, string defaultElemName)
            => new Enum(enumId, defaultElemName);
    }

}
