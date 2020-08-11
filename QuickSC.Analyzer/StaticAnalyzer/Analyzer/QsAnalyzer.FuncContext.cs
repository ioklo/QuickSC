using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

namespace QuickSC.StaticAnalyzer
{
    public partial class QsAnalyzer
    {
        // 현재 함수 정보
        public class FuncContext
        {
            private QsMetaItemId? funcId;
            private QsTypeValue? retTypeValue; // 리턴 타입이 미리 정해져 있다면 이걸 쓴다
            private bool bSequence; // 시퀀스 여부
            private int lambdaCount;

            private List<LocalVarInfo> localVarInfos;
            private ImmutableDictionary<string, LocalVarInfo> localVarsByName;
            private ImmutableDictionary<QsStorageInfo, QsTypeValue> overriddenTypeValues;

            public FuncContext(QsMetaItemId? funcId, QsTypeValue? retTypeValue, bool bSequence)
            {
                this.funcId = funcId;
                this.retTypeValue = retTypeValue;
                this.bSequence = bSequence;
                this.lambdaCount = 0;

                this.localVarInfos = new List<LocalVarInfo>();
                this.localVarsByName = ImmutableDictionary<string, LocalVarInfo>.Empty;
                this.overriddenTypeValues = ImmutableDictionary<QsStorageInfo, QsTypeValue>.Empty;
            }

            public int AddLocalVarInfo(string name, QsTypeValue typeValue)
            {
                var localVarInfo = new LocalVarInfo(localVarInfos.Count, typeValue);
                localVarInfos.Add(localVarInfo);

                localVarsByName = localVarsByName.SetItem(name, localVarInfo);
                return localVarInfo.Index;
            }

            public void AddOverrideVarInfo(QsStorageInfo storageInfo, QsTypeValue typeValue)
            {
                overriddenTypeValues = overriddenTypeValues.SetItem(storageInfo, typeValue);
            }

            public bool GetLocalVarInfo(string varName, [NotNullWhen(returnValue: true)] out LocalVarInfo? localVarInfo)
            {
                return localVarsByName.TryGetValue(varName, out localVarInfo);
            }

            internal QsMetaItemId MakeLambdaFuncId()
            {
                QsMetaItemId id;

                if (funcId == null)
                    id = QsMetaItemId.Make(QsName.MakeAnonymousLambda(lambdaCount.ToString()), 0);
                else
                    id = funcId.Append(QsName.MakeAnonymousLambda(lambdaCount.ToString()), 0);

                lambdaCount++;

                return id;
            }

            public QsTypeValue? GetRetTypeValue()
            {
                return retTypeValue;
            }

            public void SetRetTypeValue(QsTypeValue retTypeValue)
            {
                this.retTypeValue = retTypeValue;
            }

            public bool IsSeqFunc()
            {
                return bSequence;
            }

            public int GetLocalVarCount()
            {
                return localVarInfos.Count;
            }

            public void ExecInLocalScope(Action action)
            {
                var prevLocalVarsByName = localVarsByName;
                var prevOverriddenTypeValues = overriddenTypeValues;

                try
                {
                    action.Invoke();
                }
                finally
                {
                    localVarsByName = prevLocalVarsByName;
                    overriddenTypeValues = prevOverriddenTypeValues;
                }
            }

            public bool ShouldOverrideTypeValue(
                QsStorageInfo storageInfo,
                QsTypeValue typeValue,
                [NotNullWhen(returnValue: true)] out QsTypeValue? overriddenTypeValue)
            {
                return overriddenTypeValues.TryGetValue(storageInfo, out overriddenTypeValue);
            }
        }
    }
}