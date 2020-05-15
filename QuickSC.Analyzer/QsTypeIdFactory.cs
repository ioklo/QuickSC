namespace QuickSC
{
    public class QsTypeIdFactory
    {
        int typeIdCount;

        public QsTypeIdFactory()
        {
            typeIdCount = 0;
        }

        public QsTypeId MakeTypeId()
        {
            typeIdCount++;
            return new QsTypeId(null, typeIdCount);
        }
    }
}