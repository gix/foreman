namespace Foreman
{
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;

    public class LocalizedStringDictionary
    {
        private readonly Dictionary<(string, string), string> dictionary = new();

        public void Clear()
        {
            dictionary.Clear();
        }

        [MaybeNull]
        public string this[string section, string key]
        {
            get => dictionary.GetValueOrDefault((section, key));
            set => dictionary[(section, key)] = value;
        }

        public bool TryGetValue(string section, string key, [MaybeNullWhen(false)] out string value)
        {
            return dictionary.TryGetValue((section, key), out value);
        }
    }
}
