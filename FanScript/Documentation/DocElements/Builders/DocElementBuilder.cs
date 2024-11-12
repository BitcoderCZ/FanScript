using System.Text;
using FanScript.Documentation.DocElements.Links;

namespace FanScript.Documentation.DocElements.Builders;

/// <summary>
/// Builds <see cref="DocElement"/> into a string
/// </summary>
public abstract class DocElementBuilder
{
    public string Build(DocElement? element)
    {
        StringBuilder builder = new StringBuilder();
        BuildElement(element, builder);
        return builder.ToString();
    }

    protected virtual void BuildElement(DocElement? element, StringBuilder builder)
    {
        if (element is null)
        {
            return;
        }

        switch (element)
        {
            case DocBlock block:
                BuildBlock(block, builder);
                break;
            case DocString str:
                BuildString(str, builder);
                break;
            case DocHeader header:
                BuildHeader(header, builder);
                break;
            case UrlLink urlLink:
                BuildUrlLink(urlLink, builder);
                break;
            case ParamLink paramLink:
                BuildParamLink(paramLink, builder);
                break;
            case ConstantLink constantLink:
                BuildConstantLink(constantLink, builder);
                break;
            case ConstantValueLink constantValueLink:
                BuildConstantValueLink(constantValueLink, builder);
                break;
            case FunctionLink functionLink:
                BuildFunctionLink(functionLink, builder);
                break;
            case EventLink eventLink:
                BuildEventLink(eventLink, builder);
                break;
            case TypeLink typeLink:
                BuildTypeLink(typeLink, builder);
                break;
            case ModifierLink modifierLink:
                BuildModifierLink(modifierLink, builder);
                break;
            case BuildCommandLink buildCommandLink:
                BuildBuildCommandLink(buildCommandLink, builder);
                break;
            case DocCodeBlock codeBlock:
                BuildCodeBlock(codeBlock, builder);
                break;
            case DocList list:
                BuildList(list, builder);
                break;
            case DocList.Item item:
                BuildListItem(item, builder);
                break;
            default:
                BuildUnknownElement(element, builder);
                break;
        }
    }

    protected virtual void BuildUnknownElement(DocElement element, StringBuilder builder)
        => throw new Exception($"Unknown doc element '{element.GetType()}'.");

    protected virtual void BuildBlock(DocBlock block, StringBuilder builder)
    {
        foreach (var element in block.Elements)
        {
            BuildElement(element, builder);
        }
    }

    protected abstract void BuildString(DocString element, StringBuilder builder);

    protected abstract void BuildHeader(DocHeader element, StringBuilder builder);

    protected abstract void BuildUrlLink(UrlLink element, StringBuilder builder);

    protected abstract void BuildParamLink(ParamLink element, StringBuilder builder);

    protected abstract void BuildConstantLink(ConstantLink element, StringBuilder builder);

    protected abstract void BuildConstantValueLink(ConstantValueLink element, StringBuilder builder);

    protected abstract void BuildFunctionLink(FunctionLink element, StringBuilder builder);

    protected abstract void BuildEventLink(EventLink element, StringBuilder builder);

    protected abstract void BuildTypeLink(TypeLink element, StringBuilder builder);

    protected abstract void BuildModifierLink(ModifierLink element, StringBuilder builder);

    protected abstract void BuildBuildCommandLink(BuildCommandLink element, StringBuilder builder);

    protected abstract void BuildCodeBlock(DocCodeBlock element, StringBuilder builder);

    protected abstract void BuildList(DocList element, StringBuilder builder);

    protected abstract void BuildListItem(DocList.Item element, StringBuilder builder);
}
