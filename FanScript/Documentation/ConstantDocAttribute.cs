namespace FanScript.Documentation
{
    public sealed class ConstantDocAttribute : DocumentationAttribute
    {
        public ConstantDocAttribute() : base()
        {
        }

        public string[]? ValueInfos { get; set; }
    }
}
