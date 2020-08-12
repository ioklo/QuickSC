using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;

namespace QuickSC
{
    public class QsMetadataService
    {
        IEnumerable<IQsMetadata> metadatas;

        public QsMetadataService(IEnumerable<IQsMetadata> metadatas)
        {
            this.metadatas = metadatas;
        }
        
        public IEnumerable<IQsTypeInfo> GetTypeInfos(QsMetaItemId typeId)
        {
            foreach (var metadata in metadatas)
            {
                if (metadata.GetTypeInfo(typeId, out var typeInfo))
                {
                    yield return typeInfo;
                }
            }
        }
        
        public IEnumerable<QsFuncInfo> GetFuncInfos(QsMetaItemId funcId)
        {
            foreach(var metadata in metadatas)
            {
                if( metadata.GetFuncInfo(funcId, out var funcInfo))
                {
                    yield return funcInfo;
                }
            }
        }

        public IEnumerable<QsVarInfo> GetVarInfos(QsMetaItemId varId)
        {
            foreach(var metadata in metadatas)
            {
                if (metadata.GetVarInfo(varId, out var varInfo))
                {
                    yield return varInfo;
                }
            }
        }
    }
}
