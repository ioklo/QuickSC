﻿using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;

namespace QuickSC.Runtime
{
    abstract class QsRuntimeModuleTypeBuildInfo
    {
        QsMetaItemId? outerTypeId;
        QsMetaItemId id;
        IEnumerable<string> typeParams;
        QsTypeValue? baseTypeValue;
        Func<QsValue> defaultValueFactory;

        public QsMetaItemId? GetOuterTypeId() => outerTypeId;
        public QsMetaItemId GetId() => id;
        public IEnumerable<string> GetTypeParams() => typeParams;
        public QsTypeValue? GetBaseTypeValue() => baseTypeValue;
        public Func<QsValue> GetDefaultValueFactory() => defaultValueFactory;

        public QsRuntimeModuleTypeBuildInfo(QsMetaItemId? outerTypeId, QsMetaItemId id, IEnumerable<string> typeParams, QsTypeValue? baseTypeValue, Func<QsValue> defaultValueFactory)
        {
            this.outerTypeId = outerTypeId;
            this.id = id;
            this.typeParams = typeParams;
            this.baseTypeValue = baseTypeValue;
            this.defaultValueFactory = defaultValueFactory;
        }

        public abstract class Class : QsRuntimeModuleTypeBuildInfo
        {   
            public Class(QsMetaItemId? outerTypeId, QsMetaItemId id, IEnumerable<string> typeParams, QsTypeValue? baseTypeValue, Func<QsValue> defaultValueFactory)
                : base(outerTypeId, id, typeParams, baseTypeValue, defaultValueFactory)
            {   
            }
        }

        public abstract class Struct : QsRuntimeModuleTypeBuildInfo
        {
            public Struct(QsMetaItemId? outerTypeId, QsMetaItemId id, IEnumerable<string> typeParams, QsTypeValue? baseTypeValue, Func<QsValue> defaultValueFactory)
                : base(outerTypeId, id, typeParams, baseTypeValue, defaultValueFactory)
            {
            }
        }

        public abstract void Build(QsRuntimeModuleTypeBuilder builder);
    }
}
