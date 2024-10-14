namespace FanScript.Documentation
{
    public sealed class ConstantDocAttribute : DocumentationAttribute
    {
        public ConstantDocAttribute() : base()
        {
        }

        public string[]? UsedBy { get; set; }

        public string[]? ValueInfos { get; set; }
    }
}
