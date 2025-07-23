using System.Diagnostics.CodeAnalysis;

namespace Dawn.Apps.TooltipFix;

[SuppressMessage("ReSharper", "InvertIf")]
public struct LaunchArgs
{
    public LaunchArgs(string[] args)
    {
        RawArgs = args;
        CommandLine = string.Join(" ", args);
        
        IsHeadless = args.Contains("--headless");
        ExtendedLogging  = args.Contains("--extended-logging");
        NoFileLogging = args.Contains("--no-file-logging");
        
        CustomSeqUrl = ExtractArgumentValue("--seq-url=", args);
        HasCustomSeqUrl = Uri.TryCreate(CustomSeqUrl, UriKind.Absolute, out _);

        if (int.TryParse(ExtractArgumentValue("--bind-to=", args), out var pid))
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