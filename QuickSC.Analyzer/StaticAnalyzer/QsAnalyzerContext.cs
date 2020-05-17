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
        ImmutableDictionary<string, QsTypeValue> varTypeValues;

        public QsAnalyzerFuncContext(QsTypeValue? retTypeValue, bool bSequence)
        {
            RetTypeValue = retTypeValue;
            this.bSequence = bSequence;
            varTypeValues = ImmutableDictionary<string, QsTypeValue>.Empty;
        }

        public void AddVarTypeValue(string varName, QsTypeValue typeValue)
        {
            varTypeValues = varTypeValues.SetItem(varName, typeValue);
        }

        public bool GetVarTypeValue(string varName, out QsTypeValue typeValue)
        {
            return varTypeValues.TryGetValue(varName, out typeValue);
        }

        public void SetVarTypeValues(ImmutableDictionary<string, QsTypeValue> newVarTypeValues)
        {
            varTypeValues = newVarTypeValues;
        }

        public ImmutableDictionary<string, QsTypeValue> GetVarTypeValues()
        {
            return varTypeValues;
        }
    }

    // Analyzer는 backtracking이 없어서, MutableContext를 쓴다 
    public class QsAnalyzerContext
    {
        public ImmutableArray<IQsMetadata> RefMetadatas { get; }

        // TypeExp가 무슨 타입을 갖고 있는지. VarTypeValue를 여기서 교체할 수 있다
        public Dictionary<QsTypeExp, QsTypeValue> TypeValuesByTypeExp { get; }

        // 전역 타입 정보
        public ImmutableDictionary<string, QsType> GlobalTypes { get; }

        // 전역 함수 정보
        public ImmutableDictionary<string, QsFunc> GlobalFuncs { get; }

        // 에러
        public List<(object Obj, string Message)> Errors { get; }

        // 전역변수의 타입, 
        // TODO: 전역변수는 전역타입과 이름이 겹치면 안된다.
        ImmutableDictionary<string, QsTypeValue> globalVarTypeValues;

        // Exp가 무슨 타입을 갖고 있는지 저장
        public Dictionary<QsExp, QsTypeValue> TypeValuesByExp { get; }

        public QsTypeValueServiceContext TypeValueServiceContext { get; }

        // 현재 실행되고 있는 함수, null이면 글로벌        
        public QsAnalyzerFuncContext? CurFunc { get; set; }

        public Dictionary<QsCaptureInfoLocation, ImmutableDictionary<string, QsCaptureContextCaptureKind>> CaptureInfosByLocation { get; }

        public QsAnalyzerContext(
            ImmutableArray<IQsMetadata> refMetadatas,
            ImmutableDictionary<QsTypeId, QsType> typesById,
            ImmutableDictionary<QsFuncId, QsFunc> funcsById,
            Dictionary<QsTypeExp, QsTypeValue> typeValuesByTypeExp,
            ImmutableDictionary<string, QsType> globalTypes,
            ImmutableDictionary<string, QsFunc> globalFuncs)
        {
            RefMetadatas = refMetadatas;
            TypeValuesByTypeExp = typeValuesByTypeExp;
            GlobalTypes = globalTypes;
            GlobalFuncs = globalFuncs;            

            Errors = new List<(object Obj, string Message)>();
            globalVarTypeValues = ImmutableDictionary<string, QsTypeValue>.Empty;
            TypeValuesByExp = new Dictionary<QsExp, QsTypeValue>(QsReferenceComparer<QsExp>.Instance);
            TypeValueServiceContext = new QsTypeValueServiceContext(typesById, funcsById);

            CurFunc = null;
            CaptureInfosByLocation = new Dictionary<QsCaptureInfoLocation, ImmutableDictionary<string, QsCaptureContextCaptureKind>>();
        }
        
        public void AddGlobalVarTypeValue(string varName, QsTypeValue typeValue)
        {
            globalVarTypeValues = globalVarTypeValues.SetItem(varName, typeValue);
        }

        public bool GetGlobalVarTypeValue(string varName, out QsTypeValue typeValue)
        {
            return globalVarTypeValues.TryGetValue(varName, out typeValue);
        }

        public void SetGlobalVarTypeValues(ImmutableDictionary<string, QsTypeValue> newGlobalVarTypeValues)
        {
            globalVarTypeValues = newGlobalVarTypeValues;
        }

        public ImmutableDictionary<string, QsTypeValue> GetGlobalVarTypeValues()
        {
            return globalVarTypeValues;
        }

        // 1. exp가 무슨 타입을 가지는지
        // 2. callExp가 staticFunc을 호출할 경우 무슨 함수를 호출하는지
    }
}
