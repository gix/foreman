namespace Foreman
{
    using System.Diagnostics.CodeAnalysis;

    public class Language
    {
        public Language(string name)
        {
            Name = name;
        }

        public string Name { get; }
        private string? localName;

        [AllowNull]
        public string LocalName
        {
            get => !string.IsNullOrWhiteSpace(localName) ? localName : Name;
            set => localName = value;
        }
    }
}
