﻿using Gum.CompileTime;
using Gum.Syntax;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;

namespace QuickSC.StaticAnalyzer
{
    public abstract class QsScriptTemplate
    {
        public ModuleItemId Id { get; }

        public class Func : QsScriptTemplate
        {
            public TypeValue? SeqElemTypeValue { get; }
            public bool bThisCall { get; }
            public int LocalVarCount { get; }
            public Stmt Body { get; }

            internal Func(ModuleItemId funcId, TypeValue? seqElemTypeValue, bool bThisCall, int localVarCount, Stmt body)
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
            public ImmutableArray<(string Name, TypeValue TypeValue)> DefaultFields { get; }

            public Enum(ModuleItemId enumId, string defaultElemName, IEnumerable<(string Name, TypeValue TypeValue)> defaultFields)
                : base(enumId)
            {
                DefaultElemName = defaultElemName;
                DefaultFields = defaultFields.ToImmutableArray();
            }
        }

        public QsScriptTemplate(ModuleItemId funcId)
        {
            Id = funcId;
        }

        public static Func MakeFunc(ModuleItemId funcId, TypeValue? seqElemTypeValue, bool bThisCall, int localVarCount, Stmt body)
            => new Func(funcId, seqElemTypeValue, bThisCall, localVarCount, body);

        public static Enum MakeEnum(ModuleItemId enumId, string defaultElemName, IEnumerable<(string Name, TypeValue TypeValue)> defaultFields)
            => new Enum(enumId, defaultElemName, defaultFields);
    }

}
