using System.Diagnostics.CodeAnalysis;
using System.Security.Principal;

namespace Dawn.Apps.TooltipFix;

[SuppressMessage("ReSharper", "InvertIf")]
public struct LaunchArgs
{
    internal static bool IsAdmin()=> new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator);

    internal static class Keys
    {
        public const string HEADLESS_KEY = "--headless";
        public const string EXTENDED_LOGGING_KEY = "--extended-logging";
        public const string NO_FILE_LOGGING_KEY = "--no-file-logging";
        public const string MANUALLY_ELEVATED_KEY = "--manual-elevated";
        public const string CUSTOM_SEQ_URL_KEY = "--seq-url";
        public const string PROCESS_BINDING_KEY = "--bind-to";
    }
    
    public LaunchArgs(string[] args)
    {
        RawArgs = args;
        CommandLine = string.Join(" ", args);
        
        IsHeadless = args.Contains(Keys.HEADLESS_KEY);
        ExtendedLogging  = args.Contains(Keys.EXTENDED_LOGGING_KEY);
        NoFileLogging = args.Contains(Keys.NO_FILE_LOGGING_KEY);
        ManuallyElevated = args.Contains(Keys.MANUALLY_ELEVATED_KEY) && IsAdmin();
        
        CustomSeqUrl = ExtractArgumentValue($"{Keys.CUSTOM_SEQ_URL_KEY}=", args);
        HasCustomSeqUrl = Uri.TryCreate(CustomSeqUrl, UriKind.Absolute, out _);

        if (int.TryParse(ExtractArgumentValue($"{Keys.PROCESS_BINDING_KEY}=", args), out var pid))
        {
            ProcessBinding = pid;
            HasProcessBinding = true;
        }
    }
        
    public IReadOnlyList<string> RawArgs { get; }
    public string CommandLine { get; }
    
    // Args
    public bool IsHeadless { get; }
    public bool IsInteractive => !IsHeadless;
    public bool NoFileLogging { get; }
    public bool ExtendedLogging { get; }
    
    public bool HasCustomSeqUrl { get; }
    public string CustomSeqUrl { get; }

    public bool HasProcessBinding { get; }
    public int ProcessBinding { get; }

    public bool ManuallyElevated { get; set; }
    // ---
    
    private static string ExtractArgumentValue(string argumentKey, string[] args)
    {
        var rawrArgument = args.FirstOrDefault(x => x.StartsWith(argumentKey));

        if (string.IsNullOrWhiteSpace(rawrArgument))
            return string.Empty;

        var keyValue = rawrArgument.Split('=');

        return keyValue.Length > 1 ? keyValue[1] : string.Empty;
    }
}