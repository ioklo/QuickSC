using QuickSC.Syntax;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;

namespace QuickSC.StaticAnalyzer
{
    using QsLocalVarInfoDict = ImmutableDictionary<string, (int Index, QsTypeValue TypeValue)>;

    // 현재 함수 정보
    public class QsAnalyzerFuncContext
    {
        public QsFuncId FuncId { get; }
        public QsTypeValue? RetTypeValue { get; set; } // 리턴 타입이 미리 정해져 있다면 이걸 쓴다
        public bool bSequence { get; } // 시퀀스 여부
        public int LambdaCount { get; set; }
        
        public int LocalVarCount { get; private set; }

        // 현재 변수의 타입
        private QsLocalVarInfoDict localVarInfos;

        public QsAnalyzerFuncContext(QsFuncId funcId, QsTypeValue? retTypeValue, bool bSequence)
        {
            this.FuncId = funcId;
            RetTypeValue = retTypeValue;
            this.bSequence = bSequence;
            localVarInfos = QsLocalVarInfoDict.Empty;
            this.LambdaCount = 0;
            this.LocalVarCount = 0;
        }

        public int AddVarInfo(string name, QsTypeValue typeValue)
        {
            localVarInfos = localVarInfos.SetItem(name, (LocalVarCount, typeValue));
            return LocalVarCount++;
        }

        public bool GetVarInfo(string varName, out (int Index, QsTypeValue TypeValue) outValue)
        {
            return localVarInfos.TryGetValue(varName, out outValue);
        }

        public void SetVariables(QsLocalVarInfoDict newLocalVars)
        {
            localVarInfos = newLocalVars;
        }

        public QsLocalVarInfoDict GetVariables()
        {
            return localVarInfos;
        }
    }

    // Analyzer는 backtracking이 없어서, MutableContext를 쓴다 
    public class QsAnalyzerContext
    {
        public string ModuleName { get; }

        public ImmutableDictionary<QsFuncDecl, QsFunc> FuncsByFuncDecl { get; }
        public ImmutableDictionary<QsTypeExp, QsTypeValue> TypeValuesByTypeExp { get; }

        public QsMetadataService MetadataService { get; }

        public IQsErrorCollector ErrorCollector { get; }
        
        // 현재 실행되고 있는 함수
        public QsAnalyzerFuncContext CurFunc { get; set; }

        // CurFunc와 bGlobalScope를 나누는 이유는, globalScope에서 BlockStmt 안으로 들어가면 global이 아니기 때문이다
        public bool bGlobalScope { get; set; }

        public Dictionary<IQsSyntaxNode, QsSyntaxNodeInfo> InfosByNode { get; }
        public Dictionary<QsFuncId, QsScriptFuncTemplate> FuncTemplatesById { get; }        

        public QsAnalyzerContext(
            string moduleName,
            QsMetadataService metadataService,
            QsTypeEvalResult evalResult,
            QsTypeAndFuncBuildResult buildResult,
            IQsErrorCollector errorCollector)
        {
            ModuleName = moduleName;
            MetadataService = metadataService;
            
            FuncsByFuncDecl = buildResult.FuncsByFuncDecl;
            TypeValuesByTypeExp = evalResult.TypeValuesByTypeExp;

            ErrorCollector = errorCollector;

            CurFunc = new QsAnalyzerFuncContext(new QsFuncId(moduleName), null, false);
            bGlobalScope = true;
            

            InfosByNode = new Dictionary<IQsSyntaxNode, QsSyntaxNodeInfo>(QsRefEqComparer<IQsSyntaxNode>.Instance);
            FuncTemplatesById = new Dictionary<QsFuncId, QsScriptFuncTemplate>();
        }       

        // 1. exp가 무슨 타입을 가지는지
        // 2. callExp가 staticFunc을 호출할 경우 무슨 함수를 호출하는지
    }
}
