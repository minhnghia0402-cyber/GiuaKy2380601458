using Abp.Configuration.Startup;
using Abp.Localization.Dictionaries;
using Abp.Localization.Dictionaries.Xml;
using Abp.Reflection.Extensions;

namespace SV2380601458.Localization;

public static class SV2380601458LocalizationConfigurer
{
    public static void Configure(ILocalizationConfiguration localizationConfiguration)
    {
        localizationConfiguration.Sources.Add(
            new DictionaryBasedLocalizationSource(SV2380601458Consts.LocalizationSourceName,
                new XmlEmbeddedFileLocalizationDictionaryProvider(
                    typeof(SV2380601458LocalizationConfigurer).GetAssembly(),
                    "SV2380601458.Localization.SourceFiles"
                )
            )
        );
    }
}
