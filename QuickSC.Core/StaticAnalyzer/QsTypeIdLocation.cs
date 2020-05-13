using QuickSC.Syntax;
using System;
using System.Collections.Generic;

// Decl Syntax -> TypeId 맵을 만들때 사용하는 자료구조
namespace QuickSC.StaticAnalyzer
{
    public abstract class QsTypeIdLocation
    {
        public static QsTypeIdLocation Make(QsEnumDecl enumDecl) => new QsEnumTypeDeclLocation(enumDecl);
        public static QsTypeIdLocation Make(QsEnumDeclElement elem) => new QsEnumElemTypeDeclLocation(elem);
    }

    public class QsEnumTypeDeclLocation : QsTypeIdLocation
    {
        public QsEnumDecl EnumDecl { get; }
        public QsEnumTypeDeclLocation(QsEnumDecl enumDecl) { EnumDecl = enumDecl; }

        public override bool Equals(object? obj)
        {
            return obj is QsEnumTypeDeclLocation decl &&
                   ReferenceEquals(EnumDecl, decl.EnumDecl); // 둘이 같은 레퍼런스이기만 하면 된다
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(EnumDecl);
        }

        public static bool operator ==(QsEnumTypeDeclLocation? left, QsEnumTypeDeclLocation? right)
        {
            return EqualityComparer<QsEnumTypeDeclLocation?>.Default.Equals(left, right);
        }

        public static bool operator !=(QsEnumTypeDeclLocation? left, QsEnumTypeDeclLocation? right)
        {
            return !(left == right);
        }
    }

    public class QsEnumElemTypeDeclLocation : QsTypeIdLocation
    {
        public QsEnumDeclElement EnumDeclElem { get; }
        public QsEnumElemTypeDeclLocation(QsEnumDeclElement enumDeclElem) { EnumDeclElem = enumDeclElem; }

        public override bool Equals(object? obj)
        {
            return obj is QsEnumElemTypeDeclLocation decl &&
                   ReferenceEquals(EnumDeclElem, decl.EnumDeclElem);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(EnumDeclElem);
        }

        public static bool operator ==(QsEnumElemTypeDeclLocation? left, QsEnumElemTypeDeclLocation? right)
        {
            return EqualityComparer<QsEnumElemTypeDeclLocation?>.Default.Equals(left, right);
        }

        public static bool operator !=(QsEnumElemTypeDeclLocation? left, QsEnumElemTypeDeclLocation? right)
        {
            return !(left == right);
        }
    }

}