using QuickSC.Syntax;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace QuickSC.StaticAnalyzer
{
    // 현재 함수 정보
    public class QsAnalyzerFuncContext
    {
        public QsTypeValue? RetTypeValue { get; set; } // 리턴 타입이 미리 정해져 있다면 이걸 쓴다
        public bool bSequence { get; } // 시퀀스 여부

        // 현재 변수의 타입
        ImmutableDictionary<string, QsVariable> vars;

        public QsAnalyzerFuncContext(QsTypeValue? retTypeValue, bool bSequence)
        {
            RetTypeValue = retTypeValue;
            this.bSequence = bSequence;
            vars = ImmutableDictionary<string, QsVariable>.Empty;
        }

        public void AddVariable(QsVariable variable)
        {
            vars = vars.SetItem(variable.Name, variable);
        }

        public bool GetVariable(string varName, out QsVariable outVar)
        {
            return vars.TryGetValue(varName, out outVar);
        }

        public void SetVariables(ImmutableDictionary<string, QsVariable> newVars)
        {
            vars = newVars;
        }

        public ImmutableDictionary<string, QsVariable> GetVariables()
        {
            return vars;
        }
    }

    // Analyzer는 backtracking이 없어서, MutableContext를 쓴다 
    public class QsAnalyzerContext
    {
        // TypeExp가 무슨 타입을 갖고 있는지. VarTypeValue를 여기서 교체할 수 있다
        public Dictionary<QsTypeExp, QsTypeValue> TypeValuesByTypeExp { get; }

        // 에러
        public List<(object Obj, string Message)> Errors { get; }

        // 전역변수의 타입, 
        // TODO: 전역변수는 전역타입과 이름이 겹치면 안된다.
        ImmutableDictionary<string, QsTypeValue> globalVarTypeValues;

        // Exp가 무슨 타입을 갖고 있는지 저장
        public Dictionary<QsExp, QsTypeValue> TypeValuesByExp { get; }
        public Dictionary<QsExp, (QsStorage Storage, QsStorageKind Kind)> StoragesByExp { get; }

        public QsTypeValueServiceContext TypeValueServiceContext { get; }

        // 현재 실행되고 있는 함수
        public QsAnalyzerFuncContext CurFunc { get; set; }

        // CurFunc와 bGlobalScope를 나누는 이유는, globalScope에서 BlockStmt 안으로 들어가면 global이 아니기 때문이다
        public bool bGlobalScope { get; set; }

        public Dictionary<QsCaptureInfoLocation, QsCaptureInfo> CaptureInfosByLocation { get; }

        public QsAnalyzerContext(        
            Dictionary<QsTypeExp, QsTypeValue> typeValuesByTypeExp,
            List<(object obj, string msg)> errors,
            QsTypeValueServiceContext typeValueServiceContext)
        {   
            TypeValuesByTypeExp = typeValuesByTypeExp;

            Errors = errors;
            globalVarTypeValues = ImmutableDictionary<string, QsTypeValue>.Empty;
            TypeValuesByExp = new Dictionary<QsExp, QsTypeValue>(QsReferenceComparer<QsExp>.Instance);
            StoragesByExp = new Dictionary<QsExp, (QsStorage Storage, QsStorageKind Kind)>(QsReferenceComparer<QsExp>.Instance);
            TypeValueServiceContext = typeValueServiceContext;

            CurFunc = new QsAnalyzerFuncContext(null, false);
            bGlobalScope = true;
            CaptureInfosByLocation = new Dictionary<QsCaptureInfoLocation, QsCaptureInfo>();
        }        

        // 1. exp가 무슨 타입을 가지는지
        // 2. callExp가 staticFunc을 호출할 경우 무슨 함수를 호출하는지
    }
}
