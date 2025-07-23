﻿using Serilog.Sinks.Console.LogThemes;
using Serilog.Sinks.SystemConsole.Themes;

namespace Dawn.Apps.TooltipFix.Serilog;

public class SerilogBlizzardTheme : AnsiBaseTheme
{
    public static AnsiConsoleTheme GetTheme => LogThemes.UseAnsiTheme<SerilogBlizzardTheme>();

    protected override string Text => LogTheme.Foreground(Color16.BrightBlue);

    protected override string SecondaryText => LogTheme.Unthemed;

    protected override string TertiaryText => LogTheme.Unthemed;

    protected override string Invalid => LogTheme.Foreground(Color16.Red);

    protected override string Null => LogTheme.Foreground(Color16.Blue);

    protected override string Name => LogTheme.Foreground(Color16.Yellow);

    protected override string String => LogTheme.Foreground(Color16.Cyan);

    protected override string Number => LogTheme.Foreground(Color16.Magenta);

    protected override string Boolean => LogTheme.Foreground(Color16.Blue);

    protected override string Scalar => LogTheme.Foreground(Color16.Green);

    protected override string LevelVerbose => LogTheme.Foreground(Color256.Grey102);

    protected override string LevelDebug => LogTheme.Foreground(Color16.Magenta);

    protected override string LevelInformation => LogTheme.Foreground(Color16.BrightBlue);

    protected override string LevelWarning => LogTheme.Foreground(Color16.YellowBold);

    protected override string LevelError => LogTheme.Foreground(Color16.RedBold);

    protected override string LevelFatal => LogTheme.Foreground(Color16.BrightRed);
}