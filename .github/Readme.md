# Note
Windows 11 has begun rolling out updates to their programs that fixes this problem. It's currently fixed in the latest version of explorer in an explorer window, but not the taskbar. Task Manager hasn't been updated yet and as such still experiences this bug.

### Prerequisites
* [.NET 9.0.X Desktop Runtime](https://dotnet.microsoft.com/en-us/download/dotnet/9.0) (Select the ".NET Desktop Runtime 9.0" for `x64`)

### Installing
Given you've installed the `Prerequisites`
To `Install` or `Uninstall` simply run `TooltipFix.exe` from the [releases](https://github.com/JustArion/Win11_TooltipFix/releases) section.

#### Optional
The program can be installed as admin to affect programs that run as admin too but is `not required`

### Requirements (To Build from Source)
* [.NET 9.0.X SDK - x64](https://dotnet.microsoft.com/en-us/download/dotnet/9.0)
* Optional* [Git](https://git-scm.com/downloads)

### Building from Source
`Make sure the proper .NET 9.0.X SDK is installed.`
```ps1
git clone https://github.com/JustArion/Win11_Tooltip_Fix
cd .\Win11_Tooltip_Fix\
Set-ExecutionPolicy Bypass -Scope Process
./Build.ps1
```

### Packages
<p align="center">
The following packages are used in this project:
</p>

```xml
<PackageReference Include="Interop.UIAutomationClient.Signed" Version="10.19041.0" />
<PackageReference Include="Serilog.Enrichers.Process" Version="3.0.0" />
<PackageReference Include="Serilog.Sinks.Console" Version="6.0.0" />
<PackageReference Include="Serilog.Sinks.Console.LogThemes" Version="0.3.0" />
<PackageReference Include="Serilog.Sinks.Seq" Version="9.0.0" />
<PackageReference Include="TaskScheduler" Version="2.12.2" />
<PackageReference Include="Vanara.PInvoke.User32" Version="4.1.6" />
```

Addresses: 
https://linustechtips.com/topic/1466612-windows-11-annoyance-tooltips-blocking-every-other-action/

Images of a tooltip:

![Task Manager Tooltip](./images/5096f326-4e01-47b6-bdac-039aec7da779_30-10-2022.png)

![Taskbar Tooltip](./images/d62c5052-a6c1-461b-bff9-33418f2d2d20_30-10-2022.png)

![Explorer Tooltip](./images/ca74a981-2d73-49da-852d-d42f831d588c_30-10-2022.png)
