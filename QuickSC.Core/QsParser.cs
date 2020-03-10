using System;
using System.Collections.Generic;
using QuickSC.Syntax;
using QuickSC.Token;

namespace QuickSC
{
    class QsParser
    {   
        QsStringExp CreateStringExp(QsStringToken stringToken)
        {
            // var a = dir // 실행이어야 한다.
            // var a = "" a b c  // string 집어넣는거랑 어떻게 구분할 수 있는가
            
            // stringToken이

            // "aaa bbb $xxx ccc" => StringExp(TEXT("aaa bbb "), EXP(xxx), TEXT(" ccc"))
            //                    => StringExp(TEXT("aaa bbb "), EXP(xxx), TEXT(" ccc"))

            // return stringToken.Value;

            throw new NotImplementedException();
        }

        public QsStringExp? ParseCommandStatementCommandText(QsParserContext context)
        {
            // CommandText는 
            // 1. string
            // 2. $expression
            // 2. quoted string "" 세가지가 가능하다

            var savedState = context.GetState();
            var token = context.NextToken();
            if (token is QsStringToken stringToken)
            {   
                return CreateStringExp(stringToken);
            }
            else if (token is QsIdentifierToken idToken) // id를 받으면 안되고.. string을 받아야 한다
            {
                return new QsStringExp(new List<QsStringExpElement> { new QsTextStringExpElement(idToken.Value) });
            }
            else
            {
                context.SetState(savedState);
                return null;
            }
        }

        // Parse-류 함수는 실패하면 null을 리턴하고 context는 rewind가 일어나야 한다
        public QsCommandStatement? ParseCommandStatement(QsParserContext context)
        {
            var savedState = context.GetState();

            var commandText = ParseCommandStatementCommandText(context);
            if (commandText == null)
            {
                context.SetState(savedState);
                return null;
            }

            throw new NotImplementedException();
        }

        public QsStatement? ParseStatement(QsParserContext context)
        {
            var cmd = ParseCommandStatement(context);
            if (cmd != null) return cmd;

            throw new NotImplementedException();
        }

        public QsScriptElement? ParseScriptElement(QsParserContext context)
        {
            var stmt = ParseStatement(context);
            if (stmt != null) return new QsStatementScriptElement(stmt);

            return null;
        }

        public QsScript? ParseScript(string script)
        {
            var elems = new List<QsScriptElement>();
            var lexer = new QsLexer(script);

            var context = new QsParserContext(lexer);
            
            while (!context.IsReachedEnd())
            {
                var elem = ParseScriptElement(context);
                if (elem == null) return null;

                elems.Add(elem);
            }

            return new QsScript(elems);
        }
    }
}