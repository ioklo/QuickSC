using QuickSC.Syntax;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
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
        
        private int localVarCount;

        // 현재 변수의 타입
        private QsLocalVarInfoDict localVarInfos;

        public QsAnalyzerFuncContext(QsFuncId funcId, QsTypeValue? retTypeValue, bool bSequence)
        {
            this.FuncId = funcId;
            RetTypeValue = retTypeValue;
            this.bSequence = bSequence;
            localVarInfos = QsLocalVarInfoDict.Empty;
            this.LambdaCount = 0;
            this.localVarCount = 0;
        }

        public int AddVarInfo(string name, QsTypeValue typeValue)
        {
            localVarInfos = localVarInfos.SetItem(name, (localVarCount, typeValue));
            return localVarCount++;
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
        public ImmutableArray<IQsMetadata> Metadatas { get; }
        public QsTypeBuildInfo TypeBuildInfo { get; }
        public IQsErrorCollector ErrorCollector { get; }

        // 전역변수의 타입, 
        // TODO: 전역변수는 전역타입과 이름이 겹치면 안된다.
        ImmutableDictionary<string, QsTypeValue> globalVarTypeValues;

        // Exp가 무슨 타입을 갖고 있는지 저장
        public Dictionary<QsExp, QsTypeValue> TypeValuesByExp { get; }
        public Dictionary<QsExp, QsEvalExp> EvalExpsByExp { get; }
        
        // 현재 실행되고 있는 함수
        public QsAnalyzerFuncContext CurFunc { get; set; }

        // CurFunc와 bGlobalScope를 나누는 이유는, globalScope에서 BlockStmt 안으로 들어가면 global이 아니기 때문이다
        public bool bGlobalScope { get; set; }
        
        public Dictionary<QsVarDecl, QsEvalVarDecl> EvalVarDeclsByVarDecl { get; }
        public Dictionary<QsExp, QsFuncValue> FuncValuesByExp { get; set; }
        public Dictionary<QsForeachStmt, QsForeachInfo> ForeachInfosByForEachStmt { get; set; }

        public QsAnalyzerContext(
            ImmutableArray<IQsMetadata> metadatas,
            QsTypeBuildInfo typeBuildInfo,
            IQsErrorCollector errorCollector)
        {
            Metadatas = metadatas;
            TypeBuildInfo = typeBuildInfo;
            ErrorCollector = errorCollector;

            globalVarTypeValues = ImmutableDictionary<string, QsTypeValue>.Empty;
            TypeValuesByExp = new Dictionary<QsExp, QsTypeValue>(QsRefEqComparer<QsExp>.Instance);
            EvalExpsByExp = new Dictionary<QsExp, QsEvalExp>(QsRefEqComparer<QsExp>.Instance);            
            

            CurFunc = new QsAnalyzerFuncContext(new QsFuncId(null), null, false);
            bGlobalScope = true;
            
            EvalVarDeclsByVarDecl = new Dictionary<QsVarDecl, QsEvalVarDecl>(QsRefEqComparer<QsVarDecl>.Instance);
            FuncValuesByExp = new Dictionary<QsExp, QsFuncValue>(QsRefEqComparer<QsExp>.Instance);
            ForeachInfosByForEachStmt = new Dictionary<QsForeachStmt, QsForeachInfo>(QsRefEqComparer<QsForeachStmt>.Instance);
        }       

        // 1. exp가 무슨 타입을 가지는지
        // 2. callExp가 staticFunc을 호출할 경우 무슨 함수를 호출하는지
    }
}
