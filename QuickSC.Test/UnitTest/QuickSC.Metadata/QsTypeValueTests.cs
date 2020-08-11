using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace QuickSC
{
    public class QsTypeValueTests
    {
        // QsTypeValue.Normal테스트
        [Fact]
        public void TestMakeNormal()
        {
            // List<int>만들어 보기 
            var listId = QsMetaItemId.Make("List");
            var intId = QsMetaItemId.Make("int");
            var tv = QsTypeValue.MakeNormal(listId, QsTypeArgumentList.Make(QsTypeValue.MakeNormal(intId)));

            Assert.Equal(tv.TypeId, listId);
            Assert.Equal(tv.TypeArgList.Args[0], QsTypeValue.MakeNormal(intId));
        }

        [Fact]
        public void TestMakeNestedNormal()
        {
            // class X<T> { class Y<T> { class Z<U> { } } }
            // X<int>.Y<short>.Z<int> 만들어 보기            

            var intId = QsMetaItemId.Make("int");
            var shortId = QsMetaItemId.Make("short");
            var zId = QsMetaItemId.Make(new QsMetaItemIdElem("X", 1), new QsMetaItemIdElem("Y", 1), new QsMetaItemIdElem("Z", 1));

            var tv = QsTypeValue.MakeNormal(zId, QsTypeArgumentList.Make(
                QsTypeValue.MakeNormal(intId), QsTypeValue.MakeNormal(intId), QsTypeValue.MakeNormal(shortId)));

            Assert.Equal(tv.TypeId, zId);
            Assert.Equal(tv.TypeArgList.Args[0], QsTypeValue.MakeNormal(intId));
            Assert.Equal(tv.TypeArgList.Args[1], QsTypeValue.MakeNormal(intId));
            Assert.Equal(tv.TypeArgList.Args[2], QsTypeValue.MakeNormal(shortId));
        }

    }
}
