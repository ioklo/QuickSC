using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using QuickSC.Syntax;
using QuickSC.Token;

namespace QuickSC
{
    class QsParser
    {   
        async ValueTask<QsExp?> ParseExpAsync(QsParserContext context)
        {
            var stringExp = await ParseStringExpAsync(context);
            if (stringExp != null)
                return stringExp;

            var idExp = await ParseIdentifierExpAsync(context);
            if (idExp != null)
                return idExp;

            return null;
        }

        async ValueTask<QsStringExp?> ParseStringExpAsync(QsParserContext context)
        {
            var savedState = context.GetState();
            var token = await context.NextTokenAsync();

            if( token is QsStringToken stringToken)
                return MakeStringExp(stringToken);

            context.SetState(savedState);
            return null;
        }

        async ValueTask<QsIdentifierExp?> ParseIdentifierExpAsync(QsParserContext context)
        {
            var savedState = context.GetState();

            var token = await context.NextTokenAsync();
            if (token is QsIdentifierToken idToken)
                return new QsIdentifierExp(idToken.Value);

            context.SetState(savedState);
            return null;
        }

        QsStringExp MakeStringExp(QsStringToken stringToken)
        {
            //var elem = new List<QsStringExpElement>();

            //foreach(var tokenElem in stringToken.Elements)
            //{
            //    switch(tokenElem)
            //    {
            //        case QsTextStringTokenElement textTokenElem: 
            //            elem.Add(new QsTextStringExpElement(textTokenElem.Value)); 
            //            break;

            //        case QsTokenStringTokenElement tokenTokenElem:
            //            // 새로운 parserContext를 사용해서 파싱을 진행해야 한다
            //            ParseExp();

            //        default: throw new InvalidOperationException();
            //    }
            //}

            //return new QsStringExp()

            // var a = dir // 실행이어야 한다.
            // var a = "" a b c  // string 집어넣는거랑 어떻게 구분할 수 있는가
            
            // stringToken이

            // "aaa bbb $xxx ccc" => StringExp(TEXT("aaa bbb "), EXP(xxx), TEXT(" ccc"))
            //                    => StringExp(TEXT("aaa bbb "), EXP(xxx), TEXT(" ccc"))

            // return stringToken.Value;

            throw new NotImplementedException();
        }

        public async ValueTask<QsExp?> ParseCommandStatementCommandExpAsync(QsParserContext context)
        {
            var savedState = context.GetState();
            var cmdToken = await context.GetNextCommandTokenAsync();
            if (cmdToken is QsStringCommandToken stringCmdToken) // quoted string 검사
            {   
                return MakeStringExp(stringCmdToken.Token);
            }
            else if (cmdToken is QsIdentifierCommandToken idCmdToken) 
            {
                return new QsIdentifierExp(idCmdToken.Token.Value);
            }
            else
            {
                context.SetState(savedState);
                return null;
            }
        }

        public async ValueTask<QsExp?> ParseCommandStatementArgExpAsync(QsParserContext context)
        {
            var savedState = context.GetState();
            var cmdToken = await context.GetNextArgTokenAsync();
            if (cmdToken is QsStringCommandArgToken stringCmdArgToken) // quoted string 검사
            {
                return MakeStringExp(stringCmdArgToken.Token);
            }
            else
            {
                context.SetState(savedState);
                return null;
            }
        }

        // Parse-류 함수는 실패하면 null을 리턴하고 context는 rewind가 일어나야 한다
        public async ValueTask<QsCommandStatement?> ParseCommandStatementAsync(QsParserContext context)
        {
            var savedState = context.GetState();

            var commandExp = await ParseCommandStatementCommandExpAsync(context);
            if (commandExp == null)
            {
                context.SetState(savedState);
                return null;
            }

            var argExps = new List<QsExp>();

            while (!context.IsReachedEnd())
            {
                var cmdSavedState = context.GetState();
                var cmdArgToken = await context.GetNextArgTokenAsync();
                if (cmdArgToken != null && cmdArgToken is QsEndOfCommandArgToken)
                    break;
                else
                    context.SetState(cmdSavedState);

                var argExp = await ParseCommandStatementArgExpAsync(context); // 끝을 발견하면 null을 리턴한다.. 잘못된 것이 나오면 throw
                if (argExp == null)
                    throw new InvalidOperationException();

                argExps.Add(argExp);
            }

            return new QsCommandStatement(commandExp, argExps);
        }

        public async ValueTask<QsStatement?> ParseStatementAsync(QsParserContext context)
        {
            var cmd = await ParseCommandStatementAsync(context);
            if (cmd != null) return cmd;

            throw new NotImplementedException();
        }

        public async ValueTask<QsScriptElement?> ParseScriptElementAsync(QsParserContext context)
        {
            var stmt = await ParseStatementAsync(context);
            if (stmt != null) return new QsStatementScriptElement(stmt);

            return null;
        }

        public async ValueTask<QsScript?> ParseScriptAsync(QsBufferPosition pos)
        {
            var elems = new List<QsScriptElement>();
            var normalLexer = new QsNormalLexer();
            var commandLexer = new QsCommandLexer(normalLexer);            
            
            var context = new QsParserContext(pos, normalLexer, commandLexer);
            
            while (!context.IsReachedEnd())
            {
                // 끝인지 검사
                var savedState = context.GetState();
                var token = await context.NextTokenAsync();
                if (token != null && token is QsEndOfFileToken)
                {
                    break;
                }
                else
                {
                    // TODO: ugly
                    context.SetState(savedState);
                }

                var elem = await ParseScriptElementAsync(context);
                if (elem == null) return null;

                elems.Add(elem);
            }

            return new QsScript(elems);
        }
    }
}