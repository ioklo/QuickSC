namespace QuickSC
{
    public class QsEvaluator
    {
        QsParser parser;
        QsSyntaxEvaluator syntaxEvaluator;

        public QsEvaluator()
        {
            parser = new QsParser();
            syntaxEvaluator = new QsSyntaxEvaluator();
        }

        public void Evaluate(string text)
        {
            var script = parser.ParseScript(text);

            if (script != null)
                syntaxEvaluator.EvaluateScript(script);
        }
    }
}