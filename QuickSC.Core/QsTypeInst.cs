using System;
using System.Collections.Generic;
using System.Text;

namespace QuickSC
{
    public abstract class QsTypeInst
    {
        public abstract QsTypeInst GetBaseTypeInst();
    }

    // Instantiation이 필요없는 타입용
    public class QsRawTypeInst : QsTypeInst
    {
        QsTypeValue typeValue;

        public QsRawTypeInst(QsTypeValue typeValue)
        {
            this.typeValue = typeValue;
        }

        public override QsTypeInst GetBaseTypeInst()
        {
            typeValue.GetBaseTypeValue();
        }
    }
}
