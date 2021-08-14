namespace Foreman
{
    using System.Collections.Generic;

    public abstract class LocalizationInfo
    {
        public abstract string? Interpolate(LocalizedStringDictionary localized);

        public static LocalizationInfo Create(
            string section, string name, string placeholderSection, string placeholderName)
        {
            return new SingleLocalizationInfo(section, name, placeholderSection, placeholderName);
        }

        public static LocalizationInfo Create(
            string section, string name, List<string> placeholders)
        {
            return new MultiLocalizationInfo(section, name, placeholders);
        }

        private sealed class SingleLocalizationInfo : LocalizationInfo
        {
            private readonly string section;
            private readonly string name;
            private readonly string placeholderSection;
            private readonly string placeholderName;

            public SingleLocalizationInfo(
                string section, string name, string placeholderSection, string placeholderName)
            {
                this.section = section;
                this.name = name;
                this.placeholderSection = placeholderSection;
                this.placeholderName = placeholderName;
            }

            public override string? Interpolate(LocalizedStringDictionary localized)
            {
                return localized[section, name]?.Replace(
                    "__1__", localized[placeholderSection, placeholderName]);
            }
        }

        private sealed class MultiLocalizationInfo : LocalizationInfo
        {
            private readonly string section;
            private readonly string name;
            private readonly List<string> placeholders;

            public MultiLocalizationInfo(
                string section, string name, List<string> placeholders)
            {
                this.section = section;
                this.name = name;
                this.placeholders = placeholders;
            }

            public override string? Interpolate(LocalizedStringDictionary localized)
            {
                var str = localized[section, name];
                if (str == null)
                    return null;

                for (int p = 1, i = 0, e = placeholders.Count / 2; i < e; ++p, i += 2) {
                    str = str.Replace(
                        $"__{p}__",
                        localized[placeholders[i],
                            placeholders[i + 1]]);
                }

                return str;
            }
        }
    }
}
