namespace QuickSC.StaticAnalyzer
{
    public partial class QsAnalyzer
    {
        public class IdentifierInfo
        {
            public class Var : IdentifierInfo
            {
                public QsStorageInfo StorageInfo { get; }
                public QsTypeValue TypeValue { get; }

                public Var(QsStorageInfo storageInfo, QsTypeValue typeValue)
                {
                    StorageInfo = storageInfo;
                    TypeValue = typeValue;
                }
            }

            public class Func : IdentifierInfo
            {
                public QsFuncValue FuncValue { get; }
                public Func(QsFuncValue funcValue)
                {
                    FuncValue = funcValue;
                }
            }

            public class Type : IdentifierInfo
            {
                public QsTypeValue.Normal TypeValue { get; }
                public Type(QsTypeValue.Normal typeValue)
                {
                    TypeValue = typeValue;
                }
            }

            public class EnumElem : IdentifierInfo
            {
                public QsTypeValue.Normal EnumTypeValue { get; }
                public QsEnumElemInfo ElemInfo { get; }

                public EnumElem(QsTypeValue.Normal enumTypeValue, QsEnumElemInfo elemInfo)
                {
                    EnumTypeValue = enumTypeValue;
                    ElemInfo = elemInfo;
                }
            }

            public static Var MakeVar(QsStorageInfo storageInfo, QsTypeValue typeValue) => new Var(storageInfo, typeValue);

            public static Func MakeFunc(QsFuncValue funcValue) => new Func(funcValue);

            public static Type MakeType(QsTypeValue.Normal typeValue) => new Type(typeValue);

            public static EnumElem MakeEnumElem(QsTypeValue.Normal enumTypeValue, QsEnumElemInfo elemInfo) => new EnumElem(enumTypeValue, elemInfo);
        }
    }
}
