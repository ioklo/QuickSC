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
        QsLexer lexer;

        public QsParser(QsLexer lexer)
        {
            this.lexer = lexer;
        }

        //async ValueTask<(QsToken Token, QsBufferPosition NextPos)?> NextTokenAsync(QsParserContext context)
        //{
        //    return await lexer.LexAsync(pos);
        //}

        //async ValueTask<(QsExp Exp, QsBufferPosition NextPos)?> ParseExpAsync(QsBufferPosition pos)
        //{
        //    var stringExpResult = await ParseStringExpAsync(pos);
        //    if (stringExpResult.HasValue) 
        //        return stringExpResult.Value;

        //    var idExpResult = await ParseIdentifierExpAsync(pos);
        //    if (idExpResult.HasValue)
        //        return idExpResult.Value;

        //    return null;
        //}

        //async ValueTask<(QsStringExp Exp, QsBufferPosition NextPos)?> ParseStringExpAsync(QsBufferPosition pos)
        //{
        //    var tokenResult = await lexer.LexAsync(pos);

        //    if(tokenResult.HasValue && tokenResult.Value.Token is QsStringToken stringToken)
        //        return (MakeStringExp(stringToken), tokenResult.Value.NextPos);
            
        //    return null;
        //}

        //async ValueTask<(QsIdentifierExp Exp, QsBufferPosition NextPos)?> ParseIdentifierExpAsync(QsBufferPosition pos)
        //{
        //    var tokenResult = await lexer.LexAsync(pos);

        //    if (tokenResult.HasValue && tokenResult.Value.Token is QsIdentifierToken idToken)
        //        return (new QsIdentifierExp(idToken.Value), tokenResult.Value.NextPos);
            
        //    return null;
        //}
        
        //public async ValueTask<(QsExp Exp, QsBufferPosition NextPos)?> ParseCommandStatementCommandExpAsync(QsBufferPosition pos)
        //{
        //    var cmdTokenResult = await lexer.LexAsync(GetNextCommandTokenAsync(pos);

        //    if (cmdTokenResult.HasValue)
        //    {
        //        if (cmdTokenResult.Value.Token is QsStringCommandToken stringCmdToken) // quoted string 검사
        //            return (MakeStringExp(stringCmdToken.Token), cmdTokenResult.Value.NextPos);
        //        else if (cmdTokenResult.Value.Token is QsIdentifierCommandToken idCmdToken)
        //            return (new QsIdentifierExp(idCmdToken.Token.Value), cmdTokenResult.Value.NextPos);
        //    }

        //    return null;
        //}

        //public async ValueTask<(QsExp Exp, QsBufferPosition NextPos)?> ParseCommandStatementArgExpAsync(QsBufferPosition pos)
        //{
        //    var cmdTokenResult = await commandLexer.GetNextArgTokenAsync(pos);
        //    if (cmdTokenResult.HasValue && cmdTokenResult.Value.Token is QsStringCommandArgToken stringCmdArgToken) // quoted string 검사
        //    {
        //        return (MakeStringExp(stringCmdArgToken.Token), cmdTokenResult.Value.NextPos);
        //    }

        //    return null;
        //}

        //public async ValueTask<(QsCommandStatement Stmt, QsBufferPosition NextPos)?> ParseCommandStatementAsync(QsBufferPosition pos)
        //{
        //    var commandExpResult = await ParseCommandStatementCommandExpAsync(pos);
        //    if (!commandExpResult.HasValue)
        //        return null;

        //    var argExps = new List<QsExp>();

        //    var curPos = commandExpResult.Value.NextPos;

        //    while (!curPos.IsReachEnd())
        //    {
        //        // 끝 체크가 너무 장황하다 ㅋ
        //        var cmdArgTokenResult = await commandLexer.GetNextArgTokenAsync(curPos);
        //        if (cmdArgTokenResult.HasValue && cmdArgTokenResult.Value.Token is QsEndOfCommandArgToken)
        //            break;

        //        var argExpResult = await ParseCommandStatementArgExpAsync(curPos);
        //        if (!argExpResult.HasValue)
        //            throw new InvalidOperationException();

        //        argExps.Add(argExpResult.Value.Exp);
        //        curPos = argExpResult.Value.NextPos;
        //    }

        //    return (new QsCommandStatement(commandExpResult.Value.Exp, argExps), curPos);
        //}

        //public async ValueTask<(QsStatement Stmt, QsBufferPosition NextPos)?> ParseStatementAsync(QsBufferPosition pos)
        //{
        //    var cmdResult = await ParseCommandStatementAsync(pos);
        //    if (cmdResult.HasValue) return cmdResult;

        //    throw new NotImplementedException();
        //}

        //public async ValueTask<(QsScriptElement Elem, QsBufferPosition NextPos)?> ParseScriptElementAsync(QsBufferPosition pos)
        //{
        //    var stmtResult = await ParseStatementAsync(pos);
        //    if (stmtResult.HasValue) return (new QsStatementScriptElement(stmtResult.Value.Stmt), stmtResult.Value.NextPos);

        //    return null;
        //}

        //public async ValueTask<(QsScript Script, QsBufferPosition NextPos)?> ParseScriptAsync(QsBufferPosition pos)
        //{
        //    var elems = new List<QsScriptElement>();
            
        //    while (!pos.IsReachEnd())
        //    {
        //        // 끝인지 검사
        //        var tokenResult = await lexer.LexAsync(pos);
        //        if (tokenResult.HasValue && tokenResult.Value.Token.IsSingleToken(QsSimpleTokenKind.EndOfFile))
        //            break;

        //        var elemResult = await ParseScriptElementAsync(pos);
        //        if (!elemResult.HasValue) return null;

        //        elems.Add(elemResult.Value.Elem);
        //        pos = elemResult.Value.NextPos;
        //    }

        //    return (new QsScript(elems), pos);
        //}
    }
}