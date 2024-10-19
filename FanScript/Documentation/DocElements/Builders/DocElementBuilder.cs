using FanScript.Documentation.DocElements.Links;
using System.Text;

namespace FanScript.Documentation.DocElements.Builders
{
    /// <summary>
    /// Builds <see cref="DocElement"/> into a string
    /// </summary>
    public abstract class DocElementBuilder
    {
        public string Build(DocElement? element)
        {
            StringBuilder builder = new StringBuilder();
            buildElement(element, builder);
            return builder.ToString();
        }

        protected virtual void buildElement(DocElement? element, StringBuilder builder)
        {
            if (element is null)
                return;

            switch (element)
            {
                case DocBlock block:
                    buildBlock(block, builder);
                    break;
                case DocString str:
                    buildString(str, builder);
                    break;
                case DocHeader header:
                    buildHeader(header, builder);
                    break;
                case UrlLink urlLink:
                    buildUrlLink(urlLink, builder);
                    break;
                case ParamLink paramLink:
                    buildParamLink(paramLink, builder);
                    break;
                case ConstantLink constantLink:
                    buildConstantLink(constantLink, builder);
                    break;
                case ConstantValueLink constantValueLink:
                    buildConstantValueLink(constantValueLink, builder);
                    break;
                case FunctionLink functionLink:
                    buildFunctionLink(functionLink, builder);
                    break;
                case EventLink eventLink:
                    buildEventLink(eventLink, builder);
                    break;
                case TypeLink typeLink:
                    buildTypeLink(typeLink, builder);
                    break;
                case ModifierLink modifierLink:
                    buildModifierLink(modifierLink, builder);
                    break;
                case BuildCommandLink buildCommandLink:
                    buildBuildCommandLink(buildCommandLink, builder);
                    break;
                case DocCodeBlock codeBlock:
                    buildCodeBlock(codeBlock, builder);
                    break;
                case DocList list:
                    buildList(list, builder);
                    break;
                case DocList.Item item:
                    buildListItem(item, builder);
                    break;
                default:
                    buildUnknownElement(element, builder);
                    break;
            }
        }

        protected virtual void buildUnknownElement(DocElement element, StringBuilder builder)
        {
            throw new Exception($"Unknown doc element '{element.GetType()}'.");
        }

        protected virtual void buildBlock(DocBlock block, StringBuilder builder)
        {
            foreach (var element in block.Elements)
                buildElement(element, builder);
        }

        protected abstract void buildString(DocString element, StringBuilder builder);
        protected abstract void buildHeader(DocHeader element, StringBuilder builder);
        protected abstract void buildUrlLink(UrlLink element, StringBuilder builder);
        protected abstract void buildParamLink(ParamLink element, StringBuilder builder);
        protected abstract void buildConstantLink(ConstantLink element, StringBuilder builder);
        protected abstract void buildConstantValueLink(ConstantValueLink element, StringBuilder builder);
        protected abstract void buildFunctionLink(FunctionLink element, StringBuilder builder);
        protected abstract void buildEventLink(EventLink element, StringBuilder builder);
        protected abstract void buildTypeLink(TypeLink element, StringBuilder builder);
        protected abstract void buildModifierLink(ModifierLink element, StringBuilder builder);
        protected abstract void buildBuildCommandLink(BuildCommandLink element, StringBuilder builder);
        protected abstract void buildCodeBlock(DocCodeBlock element, StringBuilder builder);
        protected abstract void buildList(DocList element, StringBuilder builder);
        protected abstract void buildListItem(DocList.Item element, StringBuilder builder);
    }
}
