using FanScript.Documentation.DocElements.Links;
using System.Text;

namespace FanScript.Documentation.DocElements.Builders
{
    /// <summary>
    /// Builds <see cref="DocElement"/> into a string
    /// </summary>
    public abstract class DocElementBuilder
    {
        protected readonly StringBuilder Builder = new();

        public string Build(DocElement? element)
        {
            Builder.Clear();
            buildElement(element);
            return Builder.ToString();
        }

        protected virtual void buildElement(DocElement? element)
        {
            if (element is null)
                return;

            switch (element)
            {
                case DocBlock block:
                    buildBlock(block);
                    break;
                case DocString str:
                    buildString(str);
                    break;
                case DocHeader header:
                    buildHeader(header);
                    break;
                case UrlLink urlLink:
                    buildUrlLink(urlLink);
                    break;
                case ParamLink paramLink:
                    buildParamLink(paramLink);
                    break;
                case ConstantLink constantLink:
                    buildConstantLink(constantLink);
                    break;
                case ConstantValueLink constantValueLink:
                    buildConstantValueLink(constantValueLink);
                    break;
                case FunctionLink functionLink:
                    buildFunctionLink(functionLink);
                    break;
                case EventLink eventLink:
                    buildEventLink(eventLink);
                    break;
                case TypeLink typeLink:
                    buildTypeLink(typeLink);
                    break;
                case ModifierLink modifierLink:
                    buildModifierLink(modifierLink);
                    break;
                case BuildCommandLink buildCommandLink:
                    buildBuildCommandLink(buildCommandLink);
                    break;
                case DocCodeBlock codeBlock:
                    buildCodeBlock(codeBlock);
                    break;
                case DocList list:
                    buildList(list);
                    break;
                case DocList.Item item:
                    buildListItem(item);
                    break;
                default:
                    buildUnknownElement(element);
                    break;
            }
        }

        protected virtual void buildUnknownElement(DocElement element)
        {
            throw new Exception($"Unknown doc element '{element.GetType()}'.");
        }

        protected virtual void buildBlock(DocBlock block)
        {
            foreach (var element in block.Elements)
                buildElement(element);
        }

        protected abstract void buildString(DocString element);
        protected abstract void buildHeader(DocHeader element);
        protected abstract void buildUrlLink(UrlLink element);
        protected abstract void buildParamLink(ParamLink element);
        protected abstract void buildConstantLink(ConstantLink element);
        protected abstract void buildConstantValueLink(ConstantValueLink element);
        protected abstract void buildFunctionLink(FunctionLink element);
        protected abstract void buildEventLink(EventLink element);
        protected abstract void buildTypeLink(TypeLink element);
        protected abstract void buildModifierLink(ModifierLink element);
        protected abstract void buildBuildCommandLink(BuildCommandLink element);
        protected abstract void buildCodeBlock(DocCodeBlock element);
        protected abstract void buildList(DocList element);
        protected abstract void buildListItem(DocList.Item element);
    }
}
