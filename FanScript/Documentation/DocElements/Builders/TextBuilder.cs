using FanScript.Documentation.DocElements.Links;

namespace FanScript.Documentation.DocElements.Builders
{
    public class TextBuilder : DocElementBuilder
    {
        protected override void buildString(DocString element)
            => Builder.Append(element.Text);
        protected override void buildHeader(DocHeader element)
            => buildElement(element.Value);
        protected override void buildUrlLink(UrlLink element)
            => buildLink(element);
        protected override void buildParamLink(ParamLink element)
            => buildLink(element);
        protected override void buildConstantLink(ConstantLink element)
            => buildLink(element);
        protected override void buildConstantValueLink(ConstantValueLink element)
            => buildLink(element);
        protected override void buildFunctionLink(FunctionLink element)
            => buildLink(element);
        protected override void buildEventLink(EventLink element)
            => buildLink(element);
        protected override void buildTypeLink(TypeLink element)
            => buildLink(element);
        protected override void buildModifierLink(ModifierLink element)
            => buildLink(element);
        protected override void buildBuildCommandLink(BuildCommandLink element)
            => buildLink(element);
        protected override void buildCodeBlock(DocCodeBlock element)
            => buildElement(element.Value);
        protected override void buildList(DocList element)
        {
            if (element.Value is DocList.Item onlyItem)
                buildListItem(onlyItem);
            else if (element.Value is DocBlock block)
            {
                foreach (var item in block.Elements)
                    if (item is DocList.Item listItem)
                        buildListItem(listItem);
            }
        }
        protected override void buildListItem(DocList.Item element)
        {
            if (!isOnEmptyLine())
                Builder.AppendLine();

            Builder.Append(" - ");
            buildElement(element.Value);

            if (!isOnEmptyLine())
                Builder.AppendLine();
        }

        private void buildLink(DocLink link)
        {
            var (displayStr, linkStr) = link.GetStrings();

            Builder.Append(displayStr);
            if (linkStr != displayStr)
            {
                Builder.Append(" (");
                Builder.Append(linkStr);
                Builder.Append(')');
            }
        }

        private bool isOnEmptyLine()
        {
            char last = Builder[Builder.Length - 1];

            return last == '\n' || last == '\r';
        }
    }
}
