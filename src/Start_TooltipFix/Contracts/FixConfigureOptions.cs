﻿namespace Start_TooltipFix.Contracts;

public readonly struct FixConfigureOptions
{
    public string Name { get; init; }
    
    public string ProgramPath { get; init; }
    
    public bool RunAsAdmin { get; init; }
}