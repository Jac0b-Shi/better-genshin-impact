using Microsoft.Extensions.Localization;
using System.Collections;
using System.Globalization;
using System.Resources;

namespace BetterGenshinImpact.Core.Infrastructure;

public sealed class EmbeddedResourceStringLocalizer<T> : IStringLocalizer<T>
{
    private readonly ResourceManager _resources = new(typeof(T));

    public LocalizedString this[string name]
    {
        get
        {
            var value = _resources.GetString(name, CultureInfo.CurrentUICulture);
            return new LocalizedString(name, value ?? name, resourceNotFound: value is null);
        }
    }

    public LocalizedString this[string name, params object[] arguments]
    {
        get
        {
            var localized = this[name];
            return new LocalizedString(name,
                string.Format(CultureInfo.CurrentCulture, localized.Value, arguments),
                localized.ResourceNotFound);
        }
    }

    public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        for (var culture = CultureInfo.CurrentUICulture;
             culture != CultureInfo.InvariantCulture;
             culture = includeParentCultures ? culture.Parent : CultureInfo.InvariantCulture)
        {
            var resourceSet = _resources.GetResourceSet(culture, true, false);
            if (resourceSet is null) continue;
            foreach (DictionaryEntry entry in resourceSet)
            {
                if (entry.Key is string name && seen.Add(name)) yield return this[name];
            }
        }
    }
}
