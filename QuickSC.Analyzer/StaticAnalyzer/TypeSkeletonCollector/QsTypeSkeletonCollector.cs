using QuickSC.Syntax;
using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Net.Http.Headers;
using System.Text;

namespace QuickSC.StaticAnalyzer
{
    public partial class QsTypeSkeletonCollector
    {   
        bool CollectEnumDecl(QsEnumDecl enumDecl, Context context)
        {
            context.AddTypeSkeleton(enumDecl, enumDecl.Name, enumDecl.TypeParams.Length);
            return true;
        }

        bool CollectFuncDecl(QsFuncDecl funcDecl, Context context)
        {
            var funcId = QsMetaItemId.Make(funcDecl.Name, funcDecl.TypeParams.Length);
            context.AddFuncId(funcDecl, funcId);
            return true;
        }

        bool CollectScript(QsScript script, Context context)
        {
            foreach (var scriptElem in script.Elements)
            {
                switch(scriptElem)
                {
                    case QsEnumDeclScriptElement enumElem:
                        if (!CollectEnumDecl(enumElem.EnumDecl, context))
                            return false;
                        break;

                    case QsFuncDeclScriptElement funcElem:
                        if (!CollectFuncDecl(funcElem.FuncDecl, context))
                            return false;
                        break;
                }
            }

            return true;
        }

        public (QsSyntaxNodeMetaItemService SyntaxNodeMetaItemService, ImmutableArray<QsTypeSkeleton> TypeSkeletons)? 
            CollectScript(QsScript script, IQsErrorCollector errorCollector)
        {
            var context = new Context();

            if (!CollectScript(script, context))
            {
                errorCollector.Add(script, $"타입 정보 모으기에 실패했습니다");
                return null;
            }

            var syntaxNodeMetaItemService = new QsSyntaxNodeMetaItemService(
                context.GetTypeIdsByNode(), 
                context.GetFuncIdsByNode());

            return (syntaxNodeMetaItemService, context.GetTypeSkeletons());
        }
    }
}
