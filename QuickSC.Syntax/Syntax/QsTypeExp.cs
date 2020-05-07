﻿using System;
using System.Collections.Generic;

namespace QuickSC.Syntax
{
    public abstract class QsTypeExp
    {   
    }
 
    public class QsIdTypeExp : QsTypeExp
    {
        public string Name { get; }
        public QsIdTypeExp(string name) { Name = name; }

        public override bool Equals(object? obj)
        {
            return obj is QsIdTypeExp exp &&
                   Name == exp.Name;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Name);
        }

        public static bool operator ==(QsIdTypeExp? left, QsIdTypeExp? right)
        {
            return EqualityComparer<QsIdTypeExp?>.Default.Equals(left, right);
        }

        public static bool operator !=(QsIdTypeExp? left, QsIdTypeExp? right)
        {
            return !(left == right);
        }
    }

    public class QsMemberTypeExp : QsTypeExp
    {
        public QsTypeExp Parent { get; }
        public string MemberName { get; }

        public QsMemberTypeExp(QsTypeExp parent, string memberName)
        {
            Parent = parent;
            MemberName = memberName;
        }
    }
}