﻿using Gum.Syntax;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace QuickSC
{
#pragma warning disable CS0660, CS0661
    public abstract class QsStorageInfo
    {
        public class ModuleGlobal : QsStorageInfo
        {
            public QsMetaItemId VarId { get; }
            internal ModuleGlobal(QsMetaItemId varId) { VarId = varId; }

            public override bool Equals(object? obj)
            {
                return obj is ModuleGlobal global &&
                       VarId.Equals(global.VarId);
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(VarId);
            }
        }

        public class PrivateGlobal : QsStorageInfo
        {
            public int Index { get; }
            public PrivateGlobal(int index) { Index = index; }

            public override bool Equals(object? obj)
            {
                return obj is PrivateGlobal global &&
                       Index == global.Index;
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(Index);
            }
        }

        public class Local : QsStorageInfo
        {
            public int Index { get; }
            public Local(int localIndex) { Index = localIndex; }

            public override bool Equals(object? obj)
            {
                return obj is Local local &&
                       Index == local.Index;
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(Index);
            }
        }

        public class EnumElem : QsStorageInfo
        {
            public string Name { get; }
            public EnumElem(string name) { Name = name; }

            public override bool Equals(object? obj)
            {
                return obj is EnumElem elem &&
                       Name == elem.Name;
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(Name);
            }
        }

        public class StaticMember : QsStorageInfo
        {
            public (QsTypeValue TypeValue, Exp Exp)? ObjectInfo { get; }
            public QsVarValue VarValue { get; }
            public StaticMember((QsTypeValue TypeValue, Exp Exp)? objectInfo, QsVarValue varValue) { ObjectInfo = objectInfo; VarValue = varValue; }

            public override bool Equals(object? obj)
            {
                return obj is StaticMember member &&
                       EqualityComparer<(QsTypeValue TypeValue, Exp Exp)?>.Default.Equals(ObjectInfo, member.ObjectInfo) &&
                       EqualityComparer<QsVarValue>.Default.Equals(VarValue, member.VarValue);
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(ObjectInfo, VarValue);
            }
        }

        public class InstanceMember : QsStorageInfo
        {
            public Exp ObjectExp { get; }
            public QsTypeValue ObjectTypeValue { get; }
            public QsName VarName { get; }
            public InstanceMember(Exp objectExp, QsTypeValue objectTypeValue, QsName varName)
            {
                ObjectExp = objectExp;
                ObjectTypeValue = objectTypeValue;
                VarName = varName;
            }

            public override bool Equals(object? obj)
            {
                return obj is InstanceMember member &&
                       EqualityComparer<Exp>.Default.Equals(ObjectExp, member.ObjectExp) &&
                       EqualityComparer<QsTypeValue>.Default.Equals(ObjectTypeValue, member.ObjectTypeValue) &&
                       EqualityComparer<QsName>.Default.Equals(VarName, member.VarName);
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(ObjectExp, ObjectTypeValue, VarName);
            }
        }

        public static ModuleGlobal MakeModuleGlobal(QsMetaItemId varId) 
            => new ModuleGlobal(varId);

        public static PrivateGlobal MakePrivateGlobal(int index) 
            => new PrivateGlobal(index);

        public static Local MakeLocal(int index) 
            => new Local(index);

        public static EnumElem MakeEnumElem(string elemName)
            => new EnumElem(elemName);

        public static StaticMember MakeStaticMember((QsTypeValue TypeValue, Exp Exp)? objetInfo, QsVarValue varValue)
            => new StaticMember(objetInfo, varValue);

        public static InstanceMember MakeInstanceMember(Exp objectExp, QsTypeValue objectTypeValue, QsName varName)
            => new InstanceMember(objectExp, objectTypeValue, varName);
        
        public static bool operator ==(QsStorageInfo? left, QsStorageInfo? right)
        {
            return EqualityComparer<QsStorageInfo?>.Default.Equals(left, right);
        }

        public static bool operator !=(QsStorageInfo? left, QsStorageInfo? right)
        {
            return !(left == right);
        }
    }
#pragma warning restore CS0660, CS0661
}
