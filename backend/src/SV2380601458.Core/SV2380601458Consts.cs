using SV2380601458.Debugging;

namespace SV2380601458;

public class SV2380601458Consts
{
    public const string LocalizationSourceName = "SV2380601458";

    public const string ConnectionStringName = "Default";

    public const bool MultiTenancyEnabled = true;


    /// <summary>
    /// Default pass phrase for SimpleStringCipher decrypt/encrypt operations
    /// </summary>
    public static readonly string DefaultPassPhrase =
        DebugHelper.IsDebug ? "gsKxGZ012HLL3MI5" : "207fca3480fe49a08ea8424f84ecd0fd";
}
