namespace FanScript.Documentation.Attributes
{
    public sealed class EventDocAttribute : DocumentationAttribute
    {
        public EventDocAttribute() : base()
        {
        }

        public string?[]? ParamInfos { get; set; }
    }
}
