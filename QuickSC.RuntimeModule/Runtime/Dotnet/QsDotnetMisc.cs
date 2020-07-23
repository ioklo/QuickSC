using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using System.Text.RegularExpressions;

namespace QuickSC.Runtime.Dotnet
{
    static class QsDotnetMisc
    {
        public static QsMetaItemId MakeTypeId(Type type)
        {
            // ([^\s]+(`(\d+))?)([\.+][^\s]+(`(\d+))?)+
            // C20200622_Reflection.Type1`1+Type2`1<T, U>
            // System.Collections.Generic.List`1[[System.Int32, System.Private.CoreLib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]
            // System.Collections.Generic.List`1

            var elems = type.FullName.Split('.', '+');
            var elemsBuilder = ImmutableArray.CreateBuilder<QsMetaItemIdElem>(elems.Length);

            foreach (var elem in elems)
            {
                var match = Regex.Match(elem, @"(?<Name>[^`]+)(`(?<TypeParamCount>\d+))?");

                if (!match.Success) continue;

                var name = match.Groups["Name"].Value;
                var typeParamCountText = match.Groups["TypeParamCount"].Value;

                if (typeParamCountText.Length != 0)
                {
                    var typeParamCount = int.Parse(typeParamCountText);
                    elemsBuilder.Add(new QsMetaItemIdElem(name, typeParamCount));
                }
                else
                {
                    elemsBuilder.Add(new QsMetaItemIdElem(name));
                }
            }

            return new QsMetaItemId(elemsBuilder.ToImmutable());
        }

    }
}
