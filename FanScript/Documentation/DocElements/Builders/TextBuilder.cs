using System.Text;
using FanScript.Documentation.DocElements.Links;
using FanScript.Utils;

namespace FanScript.Documentation.DocElements.Builders
{
    public class TextBuilder : DocElementBuilder
    {
        protected override void BuildString(DocString element, StringBuilder builder)
            => builder.Append(element.Text);

        protected override void BuildHeader(DocHeader element, StringBuilder builder)
            => BuildElement(element.Value, builder);

        protected override void BuildUrlLink(UrlLink element, StringBuilder builder)
            => BuildLink(element, builder);

        protected override void BuildParamLink(ParamLink element, StringBuilder builder)
            => BuildLink(element, builder);

        protected override void BuildConstantLink(ConstantLink element, StringBuilder builder)
            => BuildLink(element, builder);

        protected override void BuildConstantValueLink(ConstantValueLink element, StringBuilder builder)
            => BuildLink(element, builder);

        protected override void BuildFunctionLink(FunctionLink element, StringBuilder builder)
            => BuildLink(element, builder);

        protected override void BuildEventLink(EventLink element, StringBuilder builder)
            => BuildLink(element, builder);

        protected override void BuildTypeLink(TypeLink element, StringBuilder builder)
            => BuildLink(element, builder);

        protected override void BuildModifierLink(ModifierLink element, StringBuilder builder)
            => BuildLink(element, builder);

        protected override void BuildBuildCommandLink(BuildCommandLink element, StringBuilder builder)
            => BuildLink(element, builder);

        protected override void BuildCodeBlock(DocCodeBlock element, StringBuilder builder)
            => BuildElement(element.Value, builder);

        protected override void BuildList(DocList element, StringBuilder builder)
        {
            if (element.Value is DocList.Item onlyItem)
            {
                BuildListItem(onlyItem, builder);
            }
            else if (element.Value is DocBlock block)
            {
                foreach (var item in block.Elements)
                {
                    if (item is DocList.Item listItem)
                    {
                        BuildListItem(listItem, builder);
                    }
                }
            }
        }

        protected override void BuildListItem(DocList.Item element, StringBuilder builder)
        {
            if (!builder.IsCurrentLineEmpty())
            {
                builder.AppendLine();
            }

            builder.Append(" - ");
            BuildElement(element.Value, builder);

            if (!builder.IsCurrentLineEmpty())
            {
                builder.AppendLine();
            }
        }

        private static void BuildLink(DocLink link, StringBuilder builder)
        {
            var (displayStr, linkStr) = link.GetStrings();

            builder.Append(displayStr);
            if (linkStr != displayStr)
            {
                builder.Append(" (");
                builder.Append(linkStr);
                builder.Append(')');
            }
        }
    }
}
