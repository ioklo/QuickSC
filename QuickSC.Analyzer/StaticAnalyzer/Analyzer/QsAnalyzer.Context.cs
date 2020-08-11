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
    public partial class QsAnalyzer
    {
        // Analyzer는 backtracking이 없어서, MutableContext를 쓴다 
        public class Context
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
            private FuncContext curFunc;

            // CurFunc와 bGlobalScope를 나누는 이유는, globalScope에서 BlockStmt 안으로 들어가면 global이 아니기 때문이다
            private bool bGlobalScope;
            private ImmutableDictionary<QsFuncDecl, QsFuncInfo> funcsByFuncDecl;
            private QsTypeExpTypeValueService typeExpTypeValueService;
            private Dictionary<string, PrivateGlobalVarInfo> privateGlobalVarInfos;
            private Dictionary<IQsSyntaxNode, QsSyntaxNodeInfo> infosByNode;
            private List<QsScriptFuncTemplate> funcTemplates;

            public Context(
                QsMetadataService metadataService,
                QsTypeValueService typeValueService,
                QsTypeExpTypeValueService typeExpTypeValueService,
                ImmutableDictionary<QsFuncDecl, QsFuncInfo> funcsByFuncDecl,
                IQsErrorCollector errorCollector)
            {
                MetadataService = metadataService;
                TypeValueService = typeValueService;

                this.funcsByFuncDecl = funcsByFuncDecl;
                this.typeExpTypeValueService = typeExpTypeValueService;

                ErrorCollector = errorCollector;

                curFunc = new FuncContext(null, null, false);
                bGlobalScope = true;
                privateGlobalVarInfos = new Dictionary<string, PrivateGlobalVarInfo>();

                infosByNode = new Dictionary<IQsSyntaxNode, QsSyntaxNodeInfo>(QsRefEqComparer<IQsSyntaxNode>.Instance);
                funcTemplates = new List<QsScriptFuncTemplate>();
            }

            public void AddNodeInfo(IQsSyntaxNode node, QsSyntaxNodeInfo info)
            {
                infosByNode.Add(node, info);
            }

            public void AddOverrideVarInfo(QsStorageInfo storageInfo, QsTypeValue testTypeValue)
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

            public void ExecInLocalScope(Action action)
            {
                var bPrevGlobalScope = bGlobalScope;
                bGlobalScope = false;

                curFunc.ExecInLocalScope(action);

                bGlobalScope = bPrevGlobalScope;
            }

            public void ExecInFuncScope(FuncContext funcContext, Action action)
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


            public bool GetTypeValueByName(string varName, [NotNullWhen(true)] out QsTypeValue? localVarTypeValue)
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
                return typeExpTypeValueService.GetTypeValue(typeExp);
            }

            public ImmutableDictionary<IQsSyntaxNode, QsSyntaxNodeInfo> MakeInfosByNode()
            {
                return infosByNode.ToImmutableWithComparer();
            }

            public void AddFuncTemplate(QsScriptFuncTemplate funcTempl)
            {
                funcTemplates.Add(funcTempl);
            }

            private QsAnalyzerIdentifierInfo MakeVarIdentifierInfo(QsStorageInfo storageInfo, QsTypeValue typeValue)
            {
                if (curFunc.ShouldOverrideTypeValue(storageInfo, typeValue, out var overriddenTypeValue))
                    return QsAnalyzerIdentifierInfo.MakeVar(storageInfo, overriddenTypeValue);

                return QsAnalyzerIdentifierInfo.MakeVar(storageInfo, typeValue);
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
                        var storageInfo = QsStorageInfo.MakeLocal(localVarInfo.Index);
                        var typeValue = localVarInfo.TypeValue;

                        outIdInfo = MakeVarIdentifierInfo(storageInfo, typeValue);
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
                        var storageInfo = QsStorageInfo.MakePrivateGlobal(privateGlobalVarInfo.Index);
                        var typeValue = privateGlobalVarInfo.TypeValue;

                        outIdInfo = MakeVarIdentifierInfo(storageInfo, typeValue);
                        return true;
                    }

                outIdInfo = null;
                return false;
            }

            private bool GetModuleGlobalIdentifierInfo(
                string idName, IReadOnlyList<QsTypeValue> typeArgs,
                [NotNullWhen(returnValue: true)] out QsAnalyzerIdentifierInfo? outIdInfo)
            {
                var itemId = QsMetaItemId.Make(idName, typeArgs.Count);

                var candidates = new List<QsAnalyzerIdentifierInfo>();

                // id에 typeCount가 들어가므로 typeArgs.Count검사는 하지 않는다
                foreach (var varInfo in MetadataService.GetVarInfos(itemId))
                {
                    var storageInfo = QsStorageInfo.MakeModuleGlobal(itemId);
                    var typeValue = varInfo.TypeValue;

                    var idInfo = MakeVarIdentifierInfo(storageInfo, typeValue);
                    candidates.Add(idInfo);
                }

                // Global Identifier이므로 typeArgument의 최상위이다 (outer가 없다)
                var typeArgList = QsTypeArgumentList.Make(null, typeArgs);

                foreach (var funcInfo in MetadataService.GetFuncInfos(itemId))
                {
                    var idInfo = QsAnalyzerIdentifierInfo.MakeFunc(new QsFuncValue(funcInfo.FuncId, typeArgList));
                    candidates.Add(idInfo);
                }

                foreach (var typeInfo in MetadataService.GetTypeInfos(itemId))
                {
                    var idInfo = QsAnalyzerIdentifierInfo.MakeType(QsTypeValue.MakeNormal(typeInfo.TypeId, typeArgList));
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

            internal IEnumerable<QsScriptFuncTemplate> GetFuncTemplates()
            {
                return funcTemplates;
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
}
