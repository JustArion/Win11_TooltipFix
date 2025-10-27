using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Dawn.Apps.TooltipFix.Serilog;
using Dawn.Apps.TooltipFix.Serilog.CustomEnrichers;
using Interop.UIAutomationClient;
using NuGet.Versioning;
using Serilog;
using Serilog.Events;
using Velopack;

namespace Dawn.Apps.TooltipFix;

internal static class Program
{
    private static LaunchArgs Arguments;
    
    [SuppressMessage("ReSharper", "SwitchStatementHandlesSomeKnownEnumValuesWithDefault")]
    internal static void Main(string[] args)
    {
        var isWin11 = Environment.OSVersion.Version is { Major: >= 10, Minor: >= 0, Build: >= 22000 };
        if (!isWin11) 
            ExecuteAndExit(() => MessageBox(0, "This program is only compatible with Windows 11.", "Tooltip Fix", MB_FLAGS.MB_ICONERROR | MB_FLAGS.MB_OK), 1);

        Arguments = new(args);

        InitializeLogging();

        InitializeVelopackIfNecessary();
        
        if (Arguments.IsInteractive)
        {
            var sb = new StringBuilder();
            sb.AppendLine("The Tooltip Fix will automatically start when Windows starts.");
            sb.AppendLine();
            sb.AppendLine("If you want to run it as admin, restart the program as admin (Task Manager needs admin to apply Tooltip Fixes to it)");
            sb.AppendLine();
            sb.AppendLine();
            sb.AppendLine("Yes         - Runs the program as admin.");
            sb.AppendLine("No         - Runs the program normally");
            sb.AppendLine("Cancel   - Removes automatic startup & closes.");
            
            var result = MessageBox(0, sb.ToString(), "Tooltip Fix", MB_FLAGS.MB_YESNOCANCEL | MB_FLAGS.MB_TOPMOST);

            switch (result)
            {
                case MB_RESULT.IDYES:
                    ExecuteAndExit(()=> Process.Start(new ProcessStartInfo
                    {
                        FileName = Environment.ProcessPath,
                        ArgumentList =
                        {
                            LaunchArgs.Keys.HEADLESS_KEY,
                            LaunchArgs.Keys.MANUALLY_ELEVATED_KEY
                        },
                        Verb = "runas",
                        UseShellExecute = true
                    }));
                    break;
                case MB_RESULT.IDNO:
                    TooltipTaskScheduler.Update();
                    break;
                case MB_RESULT.IDCANCEL:
                default:
                    ExecuteAndExit(TooltipTaskScheduler.TryRemove);
                    return;
            }
        }
        else if (Arguments.ManuallyElevated) 
            TooltipTaskScheduler.Update();

        try
        {

            Log.Information("Running Tooltip Fix");
            InitializeWinEventHook();

            RunMessageLoop();
        }
        catch (Exception e)
        {
            Log.Fatal(e, "Fatal Error, terminating application");
        }
        finally
        {
            Log.CloseAndFlush();
        }

    }

    private static void InitializeVelopackIfNecessary()
    {
        var app = VelopackApp.Build();
        app.OnBeforeUninstallFastCallback(_ => TooltipTaskScheduler.TryRemove());
        app.Run();
    }

    private static void ExecuteAndExit(Action? act = null, int exitCode = 0)
    {
        act?.Invoke();
        Log.Information("Exiting... Goodbye!");
        Environment.Exit(exitCode);
    }

    private static void RunMessageLoop()
    {
        while (GetMessage(out var msg) > 0)
        {
            TranslateMessage(msg);
            DispatchMessage(msg);
        }
    }

    private static void InitializeConsole()
    {
        AttachConsole(ATTACH_PARENT_PROCESS);
        
        var stdOut = Console.OpenStandardOutput();
        var stdErr = Console.OpenStandardError();

        var outWriter = new StreamWriter(stdOut) { AutoFlush = true };
        var errorWriter = new StreamWriter(stdErr) { AutoFlush = true };
        
        Console.SetOut(outWriter);
        Console.SetError(errorWriter);
        
        Console.WriteLine();
    }
    
    #if RELEASE
    private const string DEFAULT_SEQ_URL = "http://localhost:9999";
    #endif
    private static void InitializeLogging()
    {
        const string LOGGING_FORMAT = "{Level:u1} {Timestamp:yyyy-MM-dd HH:mm:ss.ffffff}   [{Source}] {Message:lj}{NewLine}{Exception}";
        
        InitializeConsole();

        var config = new LoggerConfiguration()
            .MinimumLevel.Verbose()
            .Enrich.WithClassName()
            .Enrich.WithProcessName()
            .Enrich.FromLogContext()
            .WriteTo.Console(outputTemplate: LOGGING_FORMAT, theme: SerilogBlizzardTheme.GetTheme,
                applyThemeToRedirectedOutput: true, standardErrorFromLevel: LogEventLevel.Error);


        var logPath = Path.Combine(AppContext.BaseDirectory, "TooltipFix.log");
        if (!Arguments.NoFileLogging)
            config.WriteTo.File(logPath,
                outputTemplate: LOGGING_FORMAT,
                restrictedToMinimumLevel: Arguments.ExtendedLogging
                    ? LogEventLevel.Verbose
                    : LogEventLevel.Information,
                buffered: true,
                retainedFileCountLimit: 1,
                rollOnFileSizeLimit: true,
                fileSizeLimitBytes: (long)Math.Pow(1024, 2) * 20, flushToDiskInterval // 20mb
                : TimeSpan.FromSeconds(1));


        #if RELEASE
            // This is personal preference, but you can set your Seq server to catch :9999 too.
            // (Logs to nowhere if there's no Seq server listening on port 9999
            config.WriteTo.Seq(Arguments.HasCustomSeqUrl
                    ? Arguments.CustomSeqUrl
                    : DEFAULT_SEQ_URL,
                restrictedToMinimumLevel: LogEventLevel.Information);
        #endif

        Log.Logger = config.CreateLogger();

        
        AppDomain.CurrentDomain.UnhandledException += (_, eo) => Log.Error(eo.ExceptionObject as Exception, "Unhandled Exception");

        Log.Information("Tooltip Fix Initialized");
    }


    #if DEBUG
    private const int _DebugProcessID = 0;
    #endif
    private static HWINEVENTHOOK _hHook;
    private static void InitializeWinEventHook()
    {
        
        _winEventCallback ??= WinHookCallback;
        
        // If we pass 'WinHookCallback' as an implicit cast to a delegate, the GC may in some cases may collect it.
        // So we need to keep a reference to it.
        GC.KeepAlive(_winEventCallback);

    #if DEBUG
        _hHook = InitializeOnPID(_DebugProcessID);
    #else
        _hHook = InitializeOnPID(0);
    #endif

        if (!_hHook.IsNull)
            return;
        Log.Fatal(GetLastError().GetException(), "Failed to initialize WinEventHook");
        Environment.Exit(1);
    }
    
    private static HWINEVENTHOOK InitializeOnPID(uint pid) =>
        SetWinEventHook(EventConstants.EVENT_OBJECT_SHOW,
            EventConstants.EVENT_OBJECT_SHOW, 
            nint.Zero, 
            _winEventCallback!, pid, 0,
            WINEVENT.WINEVENT_OUTOFCONTEXT);


    private static WinEventProc? _winEventCallback = WinHookCallback;
    private const int Xaml_WindowedPopupClass_StringLength = 23;
    private static readonly StringBuilder _stringBuilder = new(Xaml_WindowedPopupClass_StringLength + 1); // Xaml_WindowedPopupClass + Null Terminator
    private static void WinHookCallback(HWINEVENTHOOK hWinEventHook, uint winEvent, HWND hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime)
    {
        try
        {
            try
            {
                var classNameLength = GetClassName(hwnd, _stringBuilder, _stringBuilder.Capacity);
                if (classNameLength != Xaml_WindowedPopupClass_StringLength)
                    return; // We save some time by comparing the length first. The majority of windows are not tooltips.
                
                if (_stringBuilder.ToString() is not "Xaml_WindowedPopupClass") 
                    return;
            }
            finally
            {
                _stringBuilder.Clear();
            }

            if (!IsTooltip(hwnd))
                return;

            var windowInfo = GetWindowFlags(hwnd);
            if (IsTransparent(windowInfo))
                return;
            
            SetTransparent(hwnd, windowInfo);
        }
        // If the Handle is disposed while we work with it, we don't care.
        catch (Win32Exception) { }
        catch (Exception e) { Log.Error(e, "Unknown Error"); }
    }
    
    private static readonly CUIAutomationClass _automation = new();
    
    /// <code>
    /// This is the general structure we look for.
    ///     - Xaml_WindowedPopupClass   (hwnd)
    ///         - Popup
    ///             - Tooltip
    ///                 - TextBlock
    /// </code>
    private static bool IsTooltip(HWND hwnd)
    {
        try
        {
            if (hwnd.IsNull) return false;
            var element = _automation.ElementFromHandle(hwnd.DangerousGetHandle());
            
            if (element is not { CurrentFrameworkId: "XAML" }) 
                return false;

            var popup = element.FindFirst(TreeScope.TreeScope_Children, _automation.ControlViewCondition);
            if (popup is null)
                return false;

            //                                Xaml_WindowedPopupClass         PopupHost                   Popup                       Popup
            Log.Debug("'{CurrentElementClassName}' - '{CurrentElementName}' - '{CurrentPopupClassName}' - '{CurrentPopupName}'", 
                element.CurrentClassName, element.CurrentName, popup.CurrentClassName, popup.CurrentName);
            
            var child = popup.FindFirst(TreeScope.TreeScope_Children, _automation.ControlViewCondition);
            if (child is null)
                return false;

            var childName = child.CurrentName;
            if (!string.IsNullOrWhiteSpace(childName))
            {
                //                                  ToolTip               {ToolTipName}
                Log.Verbose("Type: '{CurrentClassName}' - '{CurrentName}'", 
                    child.CurrentClassName, child.CurrentName);
            }


            return child.CurrentClassName == "ToolTip";
        }
        // The user moved over a tooltip so fast that it was disposed before we could access all its information.
        catch (NullReferenceException) {}
        catch (COMException e)
        {
            // (The first constant)
            // https://learn.microsoft.com/en-us/windows/win32/winauto/uiauto-error-codes
            // An event was unable to invoke any of the subscribers (0x80040201)
            // UIA_E_ELEMENTNOTAVAILABLE
            // The element is ( not / no longer ) available on the UI Automation tree.
            // The error wouldn't count as a NullRef as the error occurs when we call '_automation.ElementFromhandle(hwnd);' The method will throw a COM Exception
            // since from the time we got the handle to the time we called the method, the handle was disposed.
            if ((uint)e.ErrorCode is not UIA_E_ELEMENTNOTAVAILABLE)
                HandleError(e);
        }
        catch (Exception e) { HandleError(e); }
        return false;
    }

    private const uint UIA_E_ELEMENTNOTAVAILABLE = 0x80040201;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void HandleError(Exception e) => Log.Error(e, "Tooltip Error Handler");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsTransparent(WindowStylesEx style) => style.HasFlag(WindowStylesEx.WS_EX_TRANSPARENT | WindowStylesEx.WS_EX_LAYERED);

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    private static void SetTransparent(HWND hwnd, WindowStylesEx style)
    {

        style |= WindowStylesEx.WS_EX_TRANSPARENT | WindowStylesEx.WS_EX_LAYERED;
        
        var retVal = SetWindowLong(hwnd, WindowLongFlags.GWL_EXSTYLE, (int)style);
        var lastError = GetLastError();
        
        if (retVal != 0 && lastError.Succeeded) 
            return;

        Log.Error(lastError.GetException(), "Error setting window style");
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    private static WindowStylesEx GetWindowFlags(HWND hwnd) => (WindowStylesEx)GetWindowLong(hwnd, WindowLongFlags.GWL_EXSTYLE);
}