namespace QuickSC.Token
{
    public abstract class QsCommandArgToken
    {
    }

    public class QsEndOfCommandArgToken : QsCommandArgToken
    {
    }

    public class QsStringCommandArgToken : QsCommandArgToken
    {
        public QsStringToken Token { get; }
        public QsStringCommandArgToken(QsStringToken token) { Token = token; }
    }
}