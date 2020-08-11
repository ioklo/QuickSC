using Xunit;
using QuickSC;
using System;
using System.Collections.Generic;
using System.Text;
using Moq;
using System.Linq;

namespace QuickSC
{
    public class QsTypeValueApplierTests
    {
        [Fact()]
        public void Apply_FuncTest()
        {
            QsTypeValue MakeTypeValue(string name)
                => QsTypeValue.MakeNormal(QsMetaItemId.Make(name));

            // class X<T, U, V> { class Y<T, U> { V Func<T>(T t, U u); } }
            var typeInfos = new List<QsTypeInfo>();
            var funcInfos = new List<QsFuncInfo>();

            var xId = QsMetaItemId.Make("X", 3);
            var yId = xId.Append("Y", 2);
            var funcId = yId.Append("Func", 1);

            var xVTypeVar = QsTypeValue.MakeTypeVar(xId, "V");
            var yUTypeVar = QsTypeValue.MakeTypeVar(yId, "U");
            var funcTTypeVar = QsTypeValue.MakeTypeVar(funcId, "T");

            var xInfo = new QsDefaultTypeInfo(null, xId, new string[] { "T", "U", "V" }, null, new[] { yId }, Enumerable.Empty<QsMetaItemId>(), Enumerable.Empty<QsMetaItemId>());
            var yInfo = new QsDefaultTypeInfo(xId, yId, new string[] { "T", "U" }, null, Enumerable.Empty<QsMetaItemId>(), new[] { funcId }, Enumerable.Empty<QsMetaItemId>());
            var funcInfo = new QsFuncInfo(yId, funcId, false, true, new[] { "T" }, xVTypeVar, funcTTypeVar, yUTypeVar);

            typeInfos.Add(xInfo);
            typeInfos.Add(yInfo);

            funcInfos.Add(funcInfo);

            IQsMetadata metadata = new QsScriptMetadata("Script", typeInfos, funcInfos, Enumerable.Empty<QsVarInfo>());

            var metadataService = new QsMetadataService(new[] { metadata });
            var applier = new QsTypeValueApplier(metadataService);

            // X<A, B, C>.Y<D, E>.Func<F>
            var funcTypeArgList = QsTypeArgumentList.Make(
                new[] { MakeTypeValue("A"), MakeTypeValue("B"), MakeTypeValue("C") },
                new[] { MakeTypeValue("D"), MakeTypeValue("E") },
                new[] { MakeTypeValue("F") });

            var funcValue = new QsFuncValue(funcId, funcTypeArgList);
            var funcTypeValue = QsTypeValue.MakeFunc(xVTypeVar, new[] { funcTTypeVar, yUTypeVar });

            var appliedTypeValue = applier.Apply_Func(funcValue, funcTypeValue);

            var expectedTypeValue = QsTypeValue.MakeFunc(MakeTypeValue("C"), new[] { MakeTypeValue("F"), MakeTypeValue("E") });
            Assert.Equal(expectedTypeValue, appliedTypeValue);
        }

        [Fact()]
        public void ApplyTest()
        {
            // class X<T> { class Y<T> { T x; } }
            List<QsTypeInfo> typeInfos = new List<QsTypeInfo>();

            var xId = QsMetaItemId.Make("X", 1);
            var yId = xId.Append("Y", 1);

            var xInfo = new QsDefaultTypeInfo(null, xId, new string[] { "T" }, null, new[] { yId }, Enumerable.Empty<QsMetaItemId>(), Enumerable.Empty<QsMetaItemId>());
            var yInfo = new QsDefaultTypeInfo(xId, yId, new string[] { "T" }, null, Enumerable.Empty<QsMetaItemId>(), Enumerable.Empty<QsMetaItemId>(), Enumerable.Empty<QsMetaItemId>());

            typeInfos.Add(xInfo);
            typeInfos.Add(yInfo);

            IQsMetadata metadata = new QsScriptMetadata("Script", typeInfos, Enumerable.Empty<QsFuncInfo>(), Enumerable.Empty<QsVarInfo>());
                
            var metadataService = new QsMetadataService(new[] { metadata });
            var applier = new QsTypeValueApplier(metadataService);


            // Apply(X<int>.Y<short>, TofX) == int
            var intId = QsMetaItemId.Make("int");
            var shortId = QsMetaItemId.Make("short");
            var intValue = QsTypeValue.MakeNormal(intId, QsTypeArgumentList.Empty);
            var shortValue = QsTypeValue.MakeNormal(shortId, QsTypeArgumentList.Empty);

            var yTypeArgs = QsTypeArgumentList.Make(
                new[] { intValue },
                new[] { shortValue });

            var appliedValue = applier.Apply(QsTypeValue.MakeNormal(yId, yTypeArgs), QsTypeValue.MakeTypeVar(xId, "T"));

            Assert.Equal(intValue, appliedValue);
        }
    }
}