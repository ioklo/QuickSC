﻿using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;

namespace QuickSC
{
    public abstract class QsTypeInst
    {
        public abstract QsValue MakeDefaultValue();
    }
    
    // enum E { F, S(int i); } 
    // => type E
    // => type E.F : E (baseType E)
    // => type E.S : E { int i; } (baseType E)
    // Enum의 TypeInst로 가야한다
    //static ValueTask<QsEvalResult<QsValue>> NativeConstructor(
    //    QsEnumDeclElement elem, 
    //    QsEnumElemType elemType, 
    //    QsValue thisValue, ImmutableArray<QsValue> args, QsEvalContext context)
    //{
    //    if (elem.Params.Length != args.Length)
    //        return new ValueTask<QsEvalResult<QsValue>>(QsEvalResult<QsValue>.Invalid);

    //    var values = ImmutableDictionary.CreateBuilder<string, QsValue>();
    //    for (int i = 0; i < elem.Params.Length; i++)
    //        values.Add(elem.Params[i].Name, args[i]);

    //    return new ValueTask<QsEvalResult<QsValue>>(new QsEvalResult<QsValue>(new QsEnumValue(elemType, values.ToImmutable()), context));
    //}
}
