using QuickSC.Syntax;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;

namespace QuickSC.StaticAnalyzer
{
    // Analyzer는 backtracking이 없어서, MutableContext를 쓴다 
    public class QsAnalyzerContext
    {
        public struct PrivateGlobalVarInfo
        {
            public int Index { get; }
            public QsTypeValue TypeValue { get; }
            public PrivateGlobalVarInfo(int index, QsTypeValue typeValue)
            {
                Index = index;
                TypeValue = typeValue;
            }
        }

        public QsMetadataService MetadataService { get; }

        public QsTypeValueService TypeValueService { get; }

        public IQsErrorCollector ErrorCollector { get; }

        // 현재 실행되고 있는 함수
        private QsAnalyzerFuncContext curFunc;

        // CurFunc와 bGlobalScope를 나누는 이유는, globalScope에서 BlockStmt 안으로 들어가면 global이 아니기 때문이다
        private bool bGlobalScope;
        private ImmutableDictionary<QsFuncDecl, QsFuncInfo> funcsByFuncDecl;
        private ImmutableDictionary<QsTypeExp, QsTypeValue> typeValuesByTypeExp;
        private Dictionary<string, PrivateGlobalVarInfo> privateGlobalVarInfos;
        private Dictionary<IQsSyntaxNode, QsSyntaxNodeInfo> infosByNode;
        private Dictionary<QsMetaItemId, QsScriptFuncTemplate> funcTemplatesById;

        public QsAnalyzerContext(
            QsMetadataService metadataService,
            QsTypeValueService typeValueService,
            QsTypeEvalResult evalResult,
            QsTypeAndFuncBuilder.Result buildResult,
            IQsErrorCollector errorCollector)
        {
            MetadataService = metadataService;
            TypeValueService = typeValueService;

            funcsByFuncDecl = buildResult.FuncsByFuncDecl;
            typeValuesByTypeExp = evalResult.TypeValuesByTypeExp;

            ErrorCollector = errorCollector;

            curFunc = new QsAnalyzerFuncContext(new QsMetaItemId(ImmutableArray<QsMetaItemIdElem>.Empty), null, false);
            bGlobalScope = true;
            privateGlobalVarInfos = new Dictionary<string, PrivateGlobalVarInfo>();

            infosByNode = new Dictionary<IQsSyntaxNode, QsSyntaxNodeInfo>(QsRefEqComparer<IQsSyntaxNode>.Instance);
            funcTemplatesById = new Dictionary<QsMetaItemId, QsScriptFuncTemplate>();
        }
        
        public void AddNodeInfo(IQsSyntaxNode node, QsSyntaxNodeInfo info)
        {
            infosByNode.Add(node, info);
        }

        internal void AddOverrideVarInfo(QsStorageInfo storageInfo, QsTypeValue testTypeValue)
        {
            curFunc.AddOverrideVarInfo(storageInfo, testTypeValue);
        }

        public int AddPrivateGlobalVarInfo(string name, QsTypeValue typeValue)
        {
            int index = privateGlobalVarInfos.Count;

            privateGlobalVarInfos.Add(name, new PrivateGlobalVarInfo(index, typeValue));
            return index;
        }

        
        public bool GetPrivateGlobalVarInfo(string value, out PrivateGlobalVarInfo privateGlobalVarInfo)
        {
            return privateGlobalVarInfos.TryGetValue(value, out privateGlobalVarInfo);
        }
        
        public int GetPrivateGlobalVarCount()
        {
            return privateGlobalVarInfos.Count;
        }        

        // TODO: func는 왜 저장하는가
        public void ExecInLocalScope(Action action)
        {
            var bPrevGlobalScope = bGlobalScope;
            bGlobalScope = false;

            curFunc.ExecInLocalScope(action);

            bGlobalScope = bPrevGlobalScope;
        }
        
        // TODO: Exec류 action 예외처리
        public void ExecInFuncScope(QsAnalyzerFuncContext funcContext, Action action)
        {
            var (prevFunc, bPrevGlobalScope) = (curFunc, bGlobalScope);
            bGlobalScope = false;
            curFunc = funcContext;

            try
            {
                action.Invoke();
            }
            finally
            {
                bGlobalScope = bPrevGlobalScope;
                curFunc = prevFunc;
            }
        }


        public bool GetTypeValueByName(string varName, [NotNullWhen(true)]out QsTypeValue? localVarTypeValue)
        {
            throw new NotImplementedException();
        }

        public QsFuncInfo GetFuncInfoByFuncDecl(QsFuncDecl funcDecl)
        {
            return funcsByFuncDecl[funcDecl];
        }
        public bool IsGlobalScope()
        {
            return bGlobalScope;
        }

        public void SetGlobalScope(bool bNewGlobalScope)
        {
            bGlobalScope = bNewGlobalScope;
        }

        public QsTypeValue GetTypeValueByTypeExp(QsTypeExp typeExp)
        {
            return typeValuesByTypeExp[typeExp];
        }

        public ImmutableDictionary<IQsSyntaxNode, QsSyntaxNodeInfo> MakeInfosByNode()
        {
            return infosByNode.ToImmutableWithComparer();
        }

        public void AddFuncTemplate(QsMetaItemId funcId, QsScriptFuncTemplate.FuncDecl funcDecl)
        {
            funcTemplatesById.Add(funcId, funcDecl);
        }

        // 지역 스코프에서 
        private bool GetLocalIdentifierInfo(
            string idName, IReadOnlyList<QsTypeValue> typeArgs, 
            [NotNullWhen(returnValue: true)] out QsAnalyzerIdentifierInfo? outIdInfo)
        {
            // 지역 스코프에는 변수만 있고, 함수, 타입은 없으므로 이름이 겹치는 것이 있는지 검사하지 않아도 된다
            if (typeArgs.Count == 0)
                if (curFunc.GetLocalVarInfo(idName, out var localVarInfo))
                {
                    var storage = QsStorageInfo.MakeLocal(localVarInfo.Index);
                    var typeValue = localVarInfo.TypeValue;

                    outIdInfo = QsAnalyzerIdentifierInfo.MakeVar(storage, typeValue);
                    return true;
                }

            outIdInfo = null;
            return false;
        }

        private bool GetThisIdentifierInfo(
            string idName, IReadOnlyList<QsTypeValue> typeArgs,
            [NotNullWhen(returnValue: true)] out QsAnalyzerIdentifierInfo? idInfo)
        {
            // TODO: implementation

            idInfo = null;
            return false;
        }        

        private bool GetPrivateGlobalVarIdentifierInfo(
            string idName, IReadOnlyList<QsTypeValue> typeArgs,
            [NotNullWhen(returnValue: true)] out QsAnalyzerIdentifierInfo? outIdInfo)
        {
            if (typeArgs.Count == 0)
                if (privateGlobalVarInfos.TryGetValue(idName, out var privateGlobalVarInfo))
                {
                    var storage = QsStorageInfo.MakePrivateGlobal(privateGlobalVarInfo.Index);
                    var typeValue = privateGlobalVarInfo.TypeValue;

                    outIdInfo = QsAnalyzerIdentifierInfo.MakeVar(storage, typeValue);
                    return true;
                }

            outIdInfo = null;
            return false;
        }

        private bool GetModuleGlobalIdentifierInfo(
            string idName, IReadOnlyList<QsTypeValue> typeArgs,
            [NotNullWhen(returnValue: true)] out QsAnalyzerIdentifierInfo? outIdInfo)
        {
            var itemId = new QsMetaItemId(new QsMetaItemIdElem(idName, typeArgs.Count));

            var candidates = new List<QsAnalyzerIdentifierInfo>();

            // id에 typeCount가 들어가므로 typeArgs.Count검사는 하지 않는다
            foreach (var varInfo in MetadataService.GetVarInfos(itemId))
            {
                var storage = QsStorageInfo.MakeModuleGlobal(itemId);
                var typeValue = varInfo.TypeValue;

                var idInfo = QsAnalyzerIdentifierInfo.MakeVar(storage, typeValue);
                candidates.Add(idInfo);
            }
            
            foreach (var funcInfo in MetadataService.GetFuncInfos(itemId))
            {
                // TODO: outer 취급 주의
                var idInfo = QsAnalyzerIdentifierInfo.MakeFunc(new QsFuncValue(null, funcInfo.FuncId, typeArgs));
                candidates.Add(idInfo);
            }

            foreach(var typeInfo in MetadataService.GetTypeInfos(itemId))
            {
                var idInfo = QsAnalyzerIdentifierInfo.MakeType(new QsTypeValue_Normal(null, typeInfo.TypeId, typeArgs));
                candidates.Add(idInfo);
            }

            if (candidates.Count == 1)
            {
                outIdInfo = candidates[0];
                return true;
            }

            outIdInfo = null;
            return false;            
        }

        public bool GetIdentifierInfo(
            string idName, IReadOnlyList<QsTypeValue> typeArgs,
            [NotNullWhen(returnValue: true)] out QsAnalyzerIdentifierInfo? idInfo)
        {
            // 1. local 변수
            if (GetLocalIdentifierInfo(idName, typeArgs, out idInfo))
                return true;

            // 2. thisType의 {{instance, static} * {변수, 함수}}, 타입. 아직 지원 안함
            if (GetThisIdentifierInfo(idName, typeArgs, out idInfo))
                return true;

            // 3. private global 'variable'
            if (GetPrivateGlobalVarIdentifierInfo(idName, typeArgs, out idInfo))
                return true;

            // 4. module global, 변수, 함수, 타입
            if (GetModuleGlobalIdentifierInfo(idName, typeArgs, out idInfo))
                return true;

            idInfo = null;
            return false;
        }

        
        internal QsMetaItemId MakeLabmdaFuncId()
        {
            return curFunc.MakeLambdaFuncId();
        }

        internal ImmutableDictionary<QsMetaItemId, QsScriptFuncTemplate> MakeFuncTemplatesById()
        {
            return funcTemplatesById.ToImmutableDictionary();
        }

        // curFunc
        public int AddLocalVarInfo(string name, QsTypeValue typeValue)
        {
            return curFunc.AddLocalVarInfo(name, typeValue);
        }

        public bool IsSeqFunc()
        {
            return curFunc.IsSeqFunc();
        }

        public QsTypeValue? GetRetTypeValue()
        {
            return curFunc.GetRetTypeValue();
        }

        internal void SetRetTypeValue(QsTypeValue retTypeValue)
        {
            curFunc.SetRetTypeValue(retTypeValue);
        }
        
        public int GetLocalVarCount()
        {
            return curFunc.GetLocalVarCount();
        }

        // 1. exp가 무슨 타입을 가지는지
        // 2. callExp가 staticFunc을 호출할 경우 무슨 함수를 호출하는지
    }
}
