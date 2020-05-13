using QuickSC.Syntax;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace QuickSC.StaticAnalyzer
{
    // Analyzer는 backtracking이 없어서, MutableContext를 쓴다 
    public class QsAnalyzerContext
    {
        // TypeExp가 무슨 타입을 갖고 있는지. VarTypeValue를 여기서 교체할 수 있다
        public Dictionary<QsTypeExp, QsTypeValue> TypeValuesByTypeExp { get; }

        // 전역 타입 정보
        public ImmutableDictionary<string, QsType> GlobalTypes { get; }

        public QsTypeValue BoolTypeValue { get; }
        public QsTypeValue VoidTypeValue { get; }

        // 전역 스코프인지,
        public bool bGlobalScope { get; set; }

        // 에러
        public List<(object Obj, string Message)> Errors { get; }

        // 전역변수의 타입
        public ImmutableDictionary<string, QsTypeValue> GlobalVarTypeValues { get; set; }

        // 현재 변수의 타입
        public ImmutableDictionary<string, QsTypeValue> VarTypeValues { get; set; }

        // Exp가 무슨 타입을 갖고 있는지 저장
        public Dictionary<QsExp, QsTypeValue> TypeValuesByExp { get; }

        public QsTypeValueServiceContext TypeValueServiceContext { get; }

        // 현재 실행되고 있는 함수, null이면 글로벌
        public QsFunc? CurFunc { get; }
        public Dictionary<QsCaptureInfoLocation, ImmutableDictionary<string, QsCaptureContextCaptureKind>> CaptureInfosByLocation { get; }

        public QsAnalyzerContext(
            ImmutableDictionary<QsTypeId, QsType> typesById,
            ImmutableDictionary<QsFuncId, QsFunc> funcsById,
            Dictionary<QsTypeExp, QsTypeValue> typeValuesByTypeExp,
            ImmutableDictionary<string, QsType> globalTypes,            
            QsTypeValue boolTypeValue,
            QsTypeValue voidTypeValue)
        {
            TypeValuesByTypeExp = typeValuesByTypeExp;
            GlobalTypes = globalTypes;

            BoolTypeValue = boolTypeValue;
            VoidTypeValue = voidTypeValue;

            bGlobalScope = true;

            Errors = new List<(object Obj, string Message)>();
            GlobalVarTypeValues = ImmutableDictionary<string, QsTypeValue>.Empty;
            VarTypeValues = ImmutableDictionary<string, QsTypeValue>.Empty;
            TypeValuesByExp = new Dictionary<QsExp, QsTypeValue>(QsReferenceComparer<QsExp>.Instance);
            TypeValueServiceContext = new QsTypeValueServiceContext(typesById, funcsById);

            CurFunc = null;
            CaptureInfosByLocation = new Dictionary<QsCaptureInfoLocation, ImmutableDictionary<string, QsCaptureContextCaptureKind>>();
        }

        public void AddVarType(string varName, QsTypeValue typeValue)
        {
            VarTypeValues = VarTypeValues.SetItem(varName, typeValue);
        }
        
        public void AddGlobalVarType(string varName, QsTypeValue typeValue)
        {
            GlobalVarTypeValues = GlobalVarTypeValues.SetItem(varName, typeValue);
        }
        
        
        // 1. exp가 무슨 타입을 가지는지
        // 2. callExp가 staticFunc을 호출할 경우 무슨 함수를 호출하는지
    }
}
