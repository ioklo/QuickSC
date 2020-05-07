using QuickSC.Syntax;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;

namespace QuickSC
{
    public abstract class QsType
    {
        public abstract QsType? GetBaseType();

        public abstract QsType? GetMemberType(string name);
        public abstract QsCallable? GetMemberFuncs(string name);
        public abstract QsValue? GetMemberValue(string name);
    }

    public class QsTypeType : QsType
    {
        public static QsTypeType Instance { get; } = new QsTypeType();

        public override QsType? GetBaseType() 
        {
            // TODO: ObjectType
            return null; 
        }

        public override QsCallable? GetMemberFuncs(string name)
        {
            throw new NotImplementedException();
        }

        public override QsType? GetMemberType(string name)
        {
            throw new NotImplementedException();
        }

        public override QsValue? GetMemberValue(string name)
        {
            throw new NotImplementedException();
        }
    }

    public class QsEnumElemType : QsType
    {
        QsEnumType baseType;

        public QsEnumElemType(QsEnumType baseType)
        {
            this.baseType = baseType;
        }

        public override QsType? GetBaseType()
        {
            return baseType;
        }

        public override QsType? GetMemberType(string name)
        {
            return null;
        }

        public override QsCallable? GetMemberFuncs(string name)
        {
            return null;
        }

        public override QsValue? GetMemberValue(string name)
        {
            return null;
        }
    }

    // enum E { F, S(int i); } 
    // => type E
    // => type E.F : E (baseType E)
    // => type E.S : E { int i; } (baseType E)
    public class QsEnumType : QsType
    {
        Dictionary<string, QsEnumElemType> memberTypes;
        Dictionary<string, QsNativeCallable> memberFuncs;
        Dictionary<string, QsValue> memberValues;

        public QsEnumType(QsEnumDecl enumDecl)
        {
            memberTypes = new Dictionary<string, QsEnumElemType>();
            memberFuncs = new Dictionary<string, QsNativeCallable>();
            memberValues = new Dictionary<string, QsValue>();

            foreach (var elem in enumDecl.Elems)
            {
                var memberType = new QsEnumElemType(this);
                memberTypes.Add(elem.Name, memberType);

                if (0 < elem.Params.Length)
                {
                    var callable = new QsNativeCallable((thisValue, args, context) => NativeConstructor(elem, memberType, thisValue, args, context));
                    memberFuncs.Add(elem.Name, callable);
                }
                else
                {
                    var value = new QsEnumValue(memberType, ImmutableDictionary<string, QsValue>.Empty);
                    memberValues.Add(elem.Name, value);
                }
            }
        }

        static ValueTask<QsEvalResult<QsValue>> NativeConstructor(
            QsEnumDeclElement elem, 
            QsEnumElemType elemType, 
            QsValue thisValue, ImmutableArray<QsValue> args, QsEvalContext context)
        {
            if (elem.Params.Length != args.Length)
                return new ValueTask<QsEvalResult<QsValue>>(QsEvalResult<QsValue>.Invalid);

            var values = ImmutableDictionary.CreateBuilder<string, QsValue>();
            for (int i = 0; i < elem.Params.Length; i++)
                values.Add(elem.Params[i].Name, args[i]);

            return new ValueTask<QsEvalResult<QsValue>>(new QsEvalResult<QsValue>(new QsEnumValue(elemType, values.ToImmutable()), context));
        }

        public override QsType? GetMemberType(string name)
        {
            if (memberTypes.TryGetValue(name, out var memberType))
                return memberType;

            return null;
        }

        public override QsCallable? GetMemberFuncs(string funcName)
        {
            if (memberFuncs.TryGetValue(funcName, out var callable))
                return callable;

            return null;
        }

        public override QsValue? GetMemberValue(string varName)
        {
            if (memberValues.TryGetValue(varName, out var value))
                return value;

            if (memberTypes.TryGetValue(varName, out var type))
                return new QsTypeValue(type);

            return null;
        }

        public override QsType? GetBaseType()
        {
            return null;
        }
    }
}

