namespace FanScript.Documentation
{
    public sealed class TypeDocAttribute : DocumentationAttribute
    {
        public TypeDocAttribute() : base()
        {
        }

        public string? HowToCreate { get; set; }
    }
}
