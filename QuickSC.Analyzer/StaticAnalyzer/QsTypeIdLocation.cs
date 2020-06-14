using QuickSC.Syntax;
using System;
using System.Collections.Generic;

// Decl Syntax -> MetadataId 맵을 만들때 사용하는 자료구조
namespace QuickSC.StaticAnalyzer
{
    public abstract class QsMetadataIdLocation
    {
        public static QsMetadataIdLocation Make(QsFuncDecl funcDecl) => new QsFuncDeclFuncDeclLocation(funcDecl);
        public static QsMetadataIdLocation Make(QsEnumDecl enumDecl) => new QsEnumTypeDeclLocation(enumDecl);        
        public static QsMetadataIdLocation Make(QsEnumDeclElement elem) => new QsEnumElemTypeDeclLocation(elem);
    }

    public class QsEnumTypeDeclLocation : QsMetadataIdLocation
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

    public class QsEnumElemTypeDeclLocation : QsMetadataIdLocation
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

    public class QsFuncDeclFuncDeclLocation : QsMetadataIdLocation
    {
        public QsFuncDecl FuncDecl { get; }
        public QsFuncDeclFuncDeclLocation(QsFuncDecl funcDecl) { FuncDecl = funcDecl; }

        public override bool Equals(object? obj)
        {
            return obj is QsFuncDeclFuncDeclLocation decl &&
                   ReferenceEquals(FuncDecl, decl.FuncDecl); // 둘이 같은 레퍼런스이기만 하면 된다
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(FuncDecl);
        }

        public static bool operator ==(QsFuncDeclFuncDeclLocation? left, QsFuncDeclFuncDeclLocation? right)
        {
            return EqualityComparer<QsFuncDeclFuncDeclLocation?>.Default.Equals(left, right);
        }

        public static bool operator !=(QsFuncDeclFuncDeclLocation? left, QsFuncDeclFuncDeclLocation? right)
        {
            return !(left == right);
        }
    }


}