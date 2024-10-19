using FanScript.Documentation.DocElements.Links;
using FanScript.Utils;
using System.Text;

namespace FanScript.Documentation.DocElements.Builders
{
    public class TextBuilder : DocElementBuilder
    {
        protected override void buildString(DocString element, StringBuilder builder)
            => builder.Append(element.Text);
        protected override void buildHeader(DocHeader element, StringBuilder builder)
            => buildElement(element.Value, builder);
        protected override void buildUrlLink(UrlLink element, StringBuilder builder)
            => buildLink(element, builder);
        protected override void buildParamLink(ParamLink element, StringBuilder builder)
            => buildLink(element, builder);
        protected override void buildConstantLink(ConstantLink element, StringBuilder builder)
            => buildLink(element, builder);
        protected override void buildConstantValueLink(ConstantValueLink element, StringBuilder builder)
            => buildLink(element, builder);
        protected override void buildFunctionLink(FunctionLink element, StringBuilder builder)
            => buildLink(element, builder);
        protected override void buildEventLink(EventLink element, StringBuilder builder)
            => buildLink(element, builder);
        protected override void buildTypeLink(TypeLink element, StringBuilder builder)
            => buildLink(element, builder);
        protected override void buildModifierLink(ModifierLink element, StringBuilder builder)
            => buildLink(element, builder);
        protected override void buildBuildCommandLink(BuildCommandLink element, StringBuilder builder)
            => buildLink(element, builder);
        protected override void buildCodeBlock(DocCodeBlock element, StringBuilder builder)
            => buildElement(element.Value, builder);
        protected override void buildList(DocList element, StringBuilder builder)
        {
            if (element.Value is DocList.Item onlyItem)
                buildListItem(onlyItem, builder);
            else if (element.Value is DocBlock block)
            {
                foreach (var item in block.Elements)
                    if (item is DocList.Item listItem)
                        buildListItem(listItem, builder);
            }
        }
        protected override void buildListItem(DocList.Item element, StringBuilder builder)
        {
            if (!builder.IsCurrentLineEmpty())
                builder.AppendLine();

            builder.Append(" - ");
            buildElement(element.Value, builder);

            if (!builder.IsCurrentLineEmpty())
                builder.AppendLine();
        }

        private void buildLink(DocLink link, StringBuilder builder)
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
