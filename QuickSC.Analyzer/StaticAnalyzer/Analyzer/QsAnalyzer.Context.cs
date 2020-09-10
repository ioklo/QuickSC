using Gum.Syntax;
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
            private ImmutableDictionary<FuncDecl, QsFuncInfo> funcInfosByDecl;
            private ImmutableDictionary<EnumDecl, QsEnumInfo> enumInfosByDecl;
            private QsTypeExpTypeValueService typeExpTypeValueService;
            private Dictionary<string, PrivateGlobalVarInfo> privateGlobalVarInfos;
            private Dictionary<ISyntaxNode, QsSyntaxNodeInfo> infosByNode;
            private List<QsScriptTemplate> templates;

            public Context(
                QsMetadataService metadataService,
                QsTypeValueService typeValueService,
                QsTypeExpTypeValueService typeExpTypeValueService,
                ImmutableDictionary<FuncDecl, QsFuncInfo> funcInfosByDecl,
                ImmutableDictionary<EnumDecl, QsEnumInfo> enumInfosByDecl,
                IQsErrorCollector errorCollector)
            {
                MetadataService = metadataService;
                TypeValueService = typeValueService;

                this.funcInfosByDecl = funcInfosByDecl;
                this.enumInfosByDecl = enumInfosByDecl;
                this.typeExpTypeValueService = typeExpTypeValueService;

                ErrorCollector = errorCollector;

                curFunc = new FuncContext(null, null, false);
                bGlobalScope = true;
                privateGlobalVarInfos = new Dictionary<string, PrivateGlobalVarInfo>();

                infosByNode = new Dictionary<ISyntaxNode, QsSyntaxNodeInfo>(QsRefEqComparer<ISyntaxNode>.Instance);
                templates = new List<QsScriptTemplate>();
            }

            public void AddNodeInfo(ISyntaxNode node, QsSyntaxNodeInfo info)
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

            public QsFuncInfo GetFuncInfoByDecl(FuncDecl funcDecl)
            {
                return funcInfosByDecl[funcDecl];
            }

            public QsEnumInfo GetEnumInfoByDecl(EnumDecl enumDecl)
            {
                return enumInfosByDecl[enumDecl];
            }

            public bool IsGlobalScope()
            {
                return bGlobalScope;
            }

            public void SetGlobalScope(bool bNewGlobalScope)
            {
                bGlobalScope = bNewGlobalScope;
            }

            public QsTypeValue GetTypeValueByTypeExp(TypeExp typeExp)
            {
                return typeExpTypeValueService.GetTypeValue(typeExp);
            }

            public ImmutableDictionary<ISyntaxNode, QsSyntaxNodeInfo> MakeInfosByNode()
            {
                return infosByNode.ToImmutableWithComparer();
            }

            public void AddTemplate(QsScriptTemplate funcTempl)
            {
                templates.Add(funcTempl);
            }

            private IdentifierInfo MakeVarIdentifierInfo(QsStorageInfo storageInfo, QsTypeValue typeValue)
            {
                if (curFunc.ShouldOverrideTypeValue(storageInfo, typeValue, out var overriddenTypeValue))
                    return IdentifierInfo.MakeVar(storageInfo, overriddenTypeValue);

                return IdentifierInfo.MakeVar(storageInfo, typeValue);
            }

            // 지역 스코프에서 
            private bool GetLocalIdentifierInfo(
                string idName, IReadOnlyList<QsTypeValue> typeArgs,
                [NotNullWhen(returnValue: true)] out IdentifierInfo? outIdInfo)
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
                [NotNullWhen(returnValue: true)] out IdentifierInfo? idInfo)
            {
                // TODO: implementation

                idInfo = null;
                return false;
            }

            private bool GetPrivateGlobalVarIdentifierInfo(
                string idName, IReadOnlyList<QsTypeValue> typeArgs,
                [NotNullWhen(returnValue: true)] out IdentifierInfo? outIdInfo)
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
                QsTypeValue? hintTypeValue,
                [NotNullWhen(returnValue: true)] out IdentifierInfo? outIdInfo)
            {
                var itemId = QsMetaItemId.Make(idName, typeArgs.Count);

                var candidates = new List<IdentifierInfo>();

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
                    var idInfo = IdentifierInfo.MakeFunc(new QsFuncValue(funcInfo.FuncId, typeArgList));
                    candidates.Add(idInfo);
                }

                foreach (var typeInfo in MetadataService.GetTypeInfos(itemId))
                {
                    var idInfo = IdentifierInfo.MakeType(QsTypeValue.MakeNormal(typeInfo.TypeId, typeArgList));
                    candidates.Add(idInfo);
                }

                // enum 힌트 사용, typeArgs가 있으면 지나간다
                if (hintTypeValue is QsTypeValue.Normal hintNTV && typeArgs.Count == 0)
                {
                    // hintNTV가 최상위 타입이라는 것을 확인하기 위해 TypeArgList의 Outer를 확인했다.
                    if (hintNTV.TypeArgList.Outer == null)
                    {
                        var hintTypeInfo = MetadataService.GetTypeInfos(hintNTV.TypeId).Single();
                        if( hintTypeInfo is IQsEnumInfo enumTypeInfo)
                        {
                            if (enumTypeInfo.GetElemInfo(idName, out var elemInfo))
                            {
                                var idInfo = IdentifierInfo.MakeEnumElem(hintNTV, elemInfo.Value);
                                candidates.Add(idInfo);
                            }
                        }
                    }
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
                QsTypeValue? hintTypeValue,
                [NotNullWhen(returnValue: true)] out IdentifierInfo? idInfo)
            {
                // 1. local 변수, local 변수에서는 힌트를 쓸 일이 없다
                if (GetLocalIdentifierInfo(idName, typeArgs, out idInfo))
                    return true;

                // 2. thisType의 {{instance, static} * {변수, 함수}}, 타입. 아직 지원 안함
                // 힌트는 오버로딩 함수 선택에 쓰일수도 있고,
                // 힌트가 thisType안의 enum인 경우 elem을 선택할 수도 있다
                if (GetThisIdentifierInfo(idName, typeArgs, out idInfo))
                    return true;

                // 3. private global 'variable', 변수이므로 힌트를 쓸 일이 없다
                if (GetPrivateGlobalVarIdentifierInfo(idName, typeArgs, out idInfo))
                    return true;

                // 4. module global, 변수, 함수, 타입, 
                // 오버로딩 함수 선택, hint가 global enum인 경우, elem선택
                if (GetModuleGlobalIdentifierInfo(idName, typeArgs, hintTypeValue, out idInfo))
                    return true;

                idInfo = null;
                return false;
            }


            internal QsMetaItemId MakeLabmdaFuncId()
            {
                return curFunc.MakeLambdaFuncId();
            }

            internal IEnumerable<QsScriptTemplate> GetTemplates()
            {
                return templates;
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
