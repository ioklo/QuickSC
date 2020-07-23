using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Dynamic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace QuickSC.Runtime.Dotnet
{
    // 닷넷 어셈블리 하나를 모듈처럼 쓸 수 있게 해주는 부분입니다
    public class QsDotnetModule : IQsModule
    {
        Assembly assembly;

        // 모듈 이름은 어셈블리 이름 그대로 씁니다
        public string ModuleName => assembly.FullName;        
        
        // 최상위 타입 정보를 담고 있는건지 모든 타입 정보를 담고 있는건지 불분명합니다

        // A.B.C        
        public QsDotnetModule(Assembly assembly)
        {
            this.assembly = assembly;
        }
        
        private QsTypeValue MakeTypeValue(Type baseType)
        {
            throw new NotImplementedException();
        }

        string MakeDotnetName(QsMetaItemId typeId)
        {
            var sb = new StringBuilder();

            bool bFirst = true;
            foreach (var elem in typeId.Elems)
            {
                if (bFirst) bFirst = false;
                else sb.Append('.');

                sb.Append(elem.Name);

                if (elem.TypeParamCount != 0)
                {
                    sb.Append('`');
                    sb.Append(elem.TypeParamCount);
                }
            }

            return sb.ToString();
        }

        public bool GetTypeInfo(QsMetaItemId typeId, [NotNullWhen(true)] out QsTypeInfo? outType)
        {
            var dotnetType = assembly.GetType(MakeDotnetName(typeId)); 

            if (dotnetType == null)
            {
                outType = null;
                return false;
            }

            outType = new QsDotnetTypeInfo(typeId, dotnetType.GetTypeInfo());
            return true;
        }

        public bool GetFuncInfo(QsMetaItemId funcId, [NotNullWhen(true)] out QsFuncInfo? outFunc)
        {
            outFunc = null;
            return false;
        }

        public bool GetVarInfo(QsMetaItemId typeId, [NotNullWhen(true)] out QsVarInfo? outVar)
        {
            outVar = null;
            return false;
        }

        public QsFuncInst GetFuncInst(QsDomainService domainService, QsFuncValue fv)
        {
            throw new NotImplementedException();
        }

        public QsTypeInst GetTypeInst(QsDomainService domainService, QsTypeValue_Normal ntv)
        {
            var typeEnv = domainService.MakeTypeEnv(ntv);

            var dotnetType = assembly.GetType(MakeDotnetName(ntv.TypeId));
            var dotnetTypeInfo = dotnetType.GetTypeInfo();

            //if (dotnetTypeInfo.IsGenericType)
            //{
            //    if (0 < ntv.TypeArgs.Length)
            //    {
            //        // List<int> => List<Cont(int)> => List<QsValue<int>>
            //        // 
            //        // List<X<int>> => List<Cont(QsDotnetTypeInfo(X<int>))> => List<X<int>>
            //        // List<Y<int>> => List<Cont(QsTypeInfo(Y<int>))> => 

            //        // dotnetType.MakeGenericType(typeof(int));
            //        // dotnetType.MakeGenericType(typeof(QsValue<int>));

            //        // List<X>.Add(X);
            //        dotnetType.MakeGenericType()
            //    }
            //}

            // return new QsDotnetTypeInst(ntv);

            // 차차 생각하기로 한다
            throw new NotImplementedException();
        }

        public void OnLoad(QsDomainService domainService)
        {

        }
    }
}
