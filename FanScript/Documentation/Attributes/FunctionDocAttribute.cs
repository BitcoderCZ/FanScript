namespace FanScript.Documentation.Attributes
{
    public sealed class FunctionDocAttribute : DocumentationAttribute
    {
        public FunctionDocAttribute()
            : base()
        {
        }

        public string? ReturnValueInfo { get; set; }

        public string?[]? ParameterInfos { get; set; }
    }
}
