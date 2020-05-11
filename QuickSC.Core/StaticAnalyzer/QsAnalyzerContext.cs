using QuickSC.Syntax;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text;

namespace QuickSC.StaticAnalyzer
{
    // Analyzer는 backtracking이 없어서, MutableContext를 쓴다 
    public class QsAnalyzerContext
    {
        // error
        public bool bGlobalScope { get; set; }
        public List<(object Obj, string Message)> Errors { get; }
        public ImmutableDictionary<string, QsTypeValue> GlobalVarTypeValues { get; private set; }
        public ImmutableDictionary<string, QsTypeValue> VarTypeValues { get; private set; }        
        public Dictionary<QsExp, QsTypeValue> ExpTypeValues { get; }
        public Dictionary<QsTypeExp, QsTypeValue> TypeExpTypeValues { get; }

        // ReferenceEqualityComparer는 .net 5부터 지원
        class QsRefComparer<T> : IEqualityComparer<T> where T : class
        {
            public bool Equals(T x, T y)
            {
                return Object.ReferenceEquals(x, y);
            }

            public int GetHashCode(T obj)
            {
                return RuntimeHelpers.GetHashCode(obj);
            }
        }

        public QsAnalyzerContext()
        {
            bGlobalScope = true;
            Errors = new List<(object Obj, string Message)>();
            GlobalVarTypeValues = ImmutableDictionary<string, QsTypeValue>.Empty;
            VarTypeValues = ImmutableDictionary<string, QsTypeValue>.Empty;
            ExpTypeValues = new Dictionary<QsExp, QsTypeValue>(new QsRefComparer<QsExp>());
            TypeExpTypeValues = new Dictionary<QsTypeExp, QsTypeValue>(new QsRefComparer<QsTypeExp>());
        }

        public void AddError(object obj, string message)
        {
            Errors.Add((obj, message));
        }

        public bool HasError() { return Errors.Count != 0; }

        public void AddVarType(string varName, QsTypeValue typeValue)
        {
            VarTypeValues = VarTypeValues.SetItem(varName, typeValue);
        }

        public bool GetVarTypeValue(string varName, [NotNullWhen(returnValue:true)] out QsTypeValue? typeValue)
        {
            return VarTypeValues.TryGetValue(varName, out typeValue);
        }

        public void AddGlobalVarType(string varName, QsTypeValue typeValue)
        {
            GlobalVarTypeValues = GlobalVarTypeValues.SetItem(varName, typeValue);
        }

        public bool GetGlobalVarTypeValue(string varName, [NotNullWhen(returnValue: true)] out QsTypeValue? typeValue)
        {
            return GlobalVarTypeValues.TryGetValue(varName, out typeValue);
        }

        public void AddExpTypeValue(QsExp exp, QsTypeValue typeValue)
        {
            ExpTypeValues.Add(exp, typeValue);
        }

        // 1. exp가 무슨 타입을 가지는지
        // 2. callExp가 staticFunc을 호출할 경우 무슨 함수를 호출하는지
    }
}
