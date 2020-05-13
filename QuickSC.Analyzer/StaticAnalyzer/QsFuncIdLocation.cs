using QuickSC.Syntax;
using System;
using System.Collections.Generic;

// Decl Syntax -> TypeId 맵을 만들때 사용하는 자료구조
namespace QuickSC.StaticAnalyzer
{
    public abstract class QsFuncIdLocation
    {
        public static QsFuncIdLocation Make(QsFuncDecl funcDecl) => new QsFuncDeclFuncDeclLocation(funcDecl);
        public static QsFuncIdLocation Make(QsEnumDeclElement enumDeclElem) => new QsEnumDeclElemFuncDeclLocation(enumDeclElem);
    }

    class QsFuncDeclFuncDeclLocation : QsFuncIdLocation
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

    class QsEnumDeclElemFuncDeclLocation : QsFuncIdLocation
    {
        public QsEnumDeclElement EnumDeclElem { get; }
        public QsEnumDeclElemFuncDeclLocation(QsEnumDeclElement enumDeclElem) { EnumDeclElem = enumDeclElem; }

        public override bool Equals(object? obj)
        {
            return obj is QsEnumDeclElemFuncDeclLocation decl &&
                   ReferenceEquals(EnumDeclElem, decl.EnumDeclElem);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(EnumDeclElem);
        }

        public static bool operator ==(QsEnumDeclElemFuncDeclLocation? left, QsEnumDeclElemFuncDeclLocation? right)
        {
            return EqualityComparer<QsEnumDeclElemFuncDeclLocation?>.Default.Equals(left, right);
        }

        public static bool operator !=(QsEnumDeclElemFuncDeclLocation? left, QsEnumDeclElemFuncDeclLocation? right)
        {
            return !(left == right);
        }
    }

}