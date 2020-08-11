using System.Linq;
using Xunit;

namespace QuickSC
{
    public class QsTypeValueServiceTests
    {
        QsTypeValue MakeTypeValue(string name)
                => QsTypeValue.MakeNormal(QsMetaItemId.Make(name), QsTypeArgumentList.Empty);

        // class X<T, U> { class Y<U> : G<T> { 
        //     Dict<T, U> v; 
        //     T F<V>(V v, List<U> u); 
        // } }
        QsTypeValueService MakeTypeValueService()
        {
            var xId = QsMetaItemId.Make("X", 2);
            var yId = xId.Append("Y", 1);
            var vId = yId.Append("v");
            var fId = yId.Append("F", 1);            
            var dictId = QsMetaItemId.Make("Dict", 2);
            var listId = QsMetaItemId.Make("List", 1);

            var xtTypeVar = QsTypeValue.MakeTypeVar(xId, "T");
            var yuTypeVar = QsTypeValue.MakeTypeVar(yId, "U");
            var fvTypeVar = QsTypeValue.MakeTypeVar(fId, "V");

            var gId = QsMetaItemId.Make("G", 1);
            var gtTypeValue = QsTypeValue.MakeNormal(gId, QsTypeArgumentList.Make(xtTypeVar));

            var xInfo = new QsDefaultTypeInfo(null, xId, new[] { "T", "U" }, null, new[] { yId }, Enumerable.Empty<QsMetaItemId>(), Enumerable.Empty<QsMetaItemId>());
            var yInfo = new QsDefaultTypeInfo(xId, yId, new[] { "U" }, gtTypeValue, Enumerable.Empty<QsMetaItemId>(), new[] { fId }, new[] { vId });
            var vInfo = new QsVarInfo(yId, vId, false, QsTypeValue.MakeNormal(dictId, QsTypeArgumentList.Make(xtTypeVar, yuTypeVar)));
            var fInfo = new QsFuncInfo(yId, fId, false, true, new[] { "V" }, xtTypeVar, fvTypeVar, QsTypeValue.MakeNormal(listId, QsTypeArgumentList.Make(yuTypeVar)));

            var metadata = new QsScriptMetadata("Script", new[] { xInfo, yInfo }, new[] { fInfo }, new[] { vInfo });

            var metadataService = new QsMetadataService(new[] { metadata });
            var applier = new QsTypeValueApplier(metadataService);

            return new QsTypeValueService(metadataService, applier);
        }

        [Fact]
        public void GetTypeValue_VarValueTest()
        {
            // GetTypeValue(X<int>.Y<short>, v) => Dict<int, short>
            var typeValueService = MakeTypeValueService();

            var xId = QsMetaItemId.Make("X", 2);
            var yId = xId.Append("Y", 1);
            var vId = yId.Append("v");

            // X<A>.Y<B>
            var yTypeArgList = QsTypeArgumentList.Make(
                new[] { MakeTypeValue("A"), MakeTypeValue("B") },
                new[] { MakeTypeValue("C") });

            var vValue = new QsVarValue(vId, yTypeArgList); // outerTypeArgList가 들어간다
            var result = typeValueService.GetTypeValue(vValue);
            var expected = QsTypeValue.MakeNormal(QsMetaItemId.Make("Dict", 2), QsTypeArgumentList.Make(MakeTypeValue("A"), MakeTypeValue("C")));

            Assert.Equal(expected, result);
        }

        // 
        [Fact]
        public void GetTypeValue_FuncValueTest()
        {
            // GetTypeValue(X<A, B>.Y<C>.F<D>) => ((D, List<C>) => A)

            var typeValueService = MakeTypeValueService();
            
            var fId = QsMetaItemId.Make(new QsMetaItemIdElem("X", 2), new QsMetaItemIdElem("Y", 1), new QsMetaItemIdElem("F", 1));
            var fValue = new QsFuncValue(fId, QsTypeArgumentList.Make(
                new[] { MakeTypeValue("A"), MakeTypeValue("B") },
                new[] { MakeTypeValue("C") },
                new[] { MakeTypeValue("D") }));

            var result = typeValueService.GetTypeValue(fValue);
            var expected = QsTypeValue.MakeFunc(MakeTypeValue("A"), new[] {
                MakeTypeValue("D"),
                QsTypeValue.MakeNormal(QsMetaItemId.Make(new QsMetaItemIdElem("List", 1)), QsTypeArgumentList.Make(MakeTypeValue("C")))});

            Assert.Equal(expected, result);
        }
        
        [Fact]
        public void GetBaseTypeValueTest()
        {
            // GetBaseTypeValue(X<A, B>.Y<C>) => G<A>           

            var typeValueService = MakeTypeValueService();

            var yId = QsMetaItemId.Make(new QsMetaItemIdElem("X", 2), new QsMetaItemIdElem("Y", 1));
            var yValue = QsTypeValue.MakeNormal(yId, QsTypeArgumentList.Make(
                new[] { MakeTypeValue("A"), MakeTypeValue("B") },
                new[] { MakeTypeValue("C") }));

            if (!typeValueService.GetBaseTypeValue(yValue, out var baseTypeValue))
            {
                Assert.True(false, "");
                return;
            }

            var gId = QsMetaItemId.Make(new QsMetaItemIdElem("G", 1));
            var expected = QsTypeValue.MakeNormal(gId, QsTypeArgumentList.Make(MakeTypeValue("A")));
            Assert.Equal(expected, baseTypeValue);
        }

        [Fact]
        public void GetMemberFuncValueTest()
        {
            var typeValueService = MakeTypeValueService();

            // GetMemberFuncValue(X<A, B>.Y<C>, "F", D) => (X<,>.Y<>.F<>, [[A, B], [C], [D]])
            var yId = QsMetaItemId.Make(new QsMetaItemIdElem("X", 2), new QsMetaItemIdElem("Y", 1));
            var yValue = QsTypeValue.MakeNormal(yId, QsTypeArgumentList.Make(
                new[] { MakeTypeValue("A"), MakeTypeValue("B") },
                new[] { MakeTypeValue("C") }));

            if (!typeValueService.GetMemberFuncValue(yValue, QsName.MakeText("F"), new[] { MakeTypeValue("D") }, out var fValue))
            {
                Assert.True(false, "");
                return;
            }

            var fId = yId.Append("F", 1);
            var expected = new QsFuncValue(fId, QsTypeArgumentList.Make(
                new[] { MakeTypeValue("A"), MakeTypeValue("B") },
                new[] { MakeTypeValue("C") },
                new[] { MakeTypeValue("D") }));

            Assert.Equal(expected, fValue);
        }

        [Fact]
        public void GetMemberVarValueTest()
        {
            var typeValueService = MakeTypeValueService();

            // GetMemberVarValue(X<A, B>.Y<C>, "v") => (X<,>.Y<>.v, [[A, B], [C]])
            var yId = QsMetaItemId.Make(new QsMetaItemIdElem("X", 2), new QsMetaItemIdElem("Y", 1));
            var yValue = QsTypeValue.MakeNormal(yId, QsTypeArgumentList.Make(
                new[] { MakeTypeValue("A"), MakeTypeValue("B") },
                new[] { MakeTypeValue("C") }));

            if (!typeValueService.GetMemberVarValue(yValue, QsName.MakeText("v"), out var vValue))
            {
                Assert.True(false, "");
                return;
            }

            var vId = yId.Append("v");
            var expected = new QsVarValue(vId, QsTypeArgumentList.Make(
                new[] { MakeTypeValue("A"), MakeTypeValue("B") },
                new[] { MakeTypeValue("C") }));

            Assert.Equal(expected, vValue);
        }
        
    }
}