using System.Diagnostics;
using System.Security.Principal;
using Microsoft.Win32.TaskScheduler;
using Serilog;
using Task = Microsoft.Win32.TaskScheduler.Task;

namespace Dawn.Apps.TooltipFix;

internal static class TooltipTaskScheduler
{
    private static bool IsAdmin()=> new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator);
    private const string KEY = "Win11_TooltipFix";
    private static TaskService TaskService => TaskService.Instance;
    
    /// <summary>
    /// Removes Win11_TooltipFix from the Windows Task Scheduler
    /// </summary>
    public static void TryRemove()
    {
        try
        {
            using var task = TaskService.GetTask(KEY);

            if (task == null)
                return;

            TaskService.Instance.RootFolder.DeleteTask(KEY);
        }
        catch (Exception e)
        {
            Log.Error(e, "Unable to remove TooltipFix from Task Scheduler");
        }
    }

    /// <summary>
    /// Ensures that the current process runs on startup on the same execution level (Admin = Runs as Admin)
    /// </summary>
    public static void Update()
    {
        using var task = TaskService.GetTask(KEY);

        if (task == null)
        {
            AddToTaskScheduler();
            return;
        }

        task.Enabled = true;
        var definition = task.Definition;
        var isAdmin = IsAdmin();

        var principal = definition.Principal;
        // The task is admin but we're not (I.E We can't edit this task)
        if (principal.RunLevel == TaskRunLevel.Highest && !isAdmin)
        {
            Log.Warning("We're not admin but the Task is listed as admin. Will need to wait till we're admin again to update task");
            return;
        }

        principal.RunLevel = isAdmin 
            ? TaskRunLevel.Highest 
            : TaskRunLevel.LUA;
        
        var userId = WindowsIdentity.GetCurrent().Name;

        var triggers = definition.Triggers;

        if (triggers.FirstOrDefault(x => x.TriggerType == TaskTriggerType.Logon) is not LogonTrigger logonTrigger)
        {
            triggers.Add(new LogonTrigger
            {
                UserId = isAdmin ? null : userId,
                // We start up later since we're not really a high priority program, we're actually the lowest
                // It's not the end of the world if we start up later than other programs
                Delay = TimeSpan.FromMinutes(1)
            });
        }
        else
        {
            logonTrigger.UserId = isAdmin ? null : userId;
            logonTrigger.Delay = TimeSpan.FromMinutes(1);
        }

        DoTaskSchedulerEditPatch(principal);
        var path = Environment.ProcessPath!;
        
        if (definition.Actions.FirstOrDefault(x => x.ActionType == TaskActionType.Execute) is not ExecAction execAction)
        {
            definition.Actions.Add(new ExecAction(path, "--headless"));
            task.RegisterChanges();
            return;
        }

        execAction.Path = path!;
        
        task.RegisterChanges();
    }

    private static void AddToTaskScheduler()
    {
        try
        {
            var isAdmin = IsAdmin();
            using var taskDefinition = TaskService.NewTask();
            taskDefinition.Actions.Add(new ExecAction(Environment.ProcessPath!, "--headless"));
            taskDefinition.Principal.RunLevel = isAdmin ? TaskRunLevel.Highest : TaskRunLevel.LUA;
            var userId = WindowsIdentity.GetCurrent().Name;
            taskDefinition.Triggers.Add(new LogonTrigger
            {
                UserId = isAdmin ? null : userId,
                Delay = TimeSpan.FromMinutes(1)
            });

            TaskService.RootFolder.RegisterTaskDefinition(KEY, taskDefinition);

            var ourTask = TaskService.GetTask(KEY);

            if (ourTask == null)
            {
                Log.Error("Failed to create new task");
                return;
            }
            
            ourTask.Enabled = true;
            ourTask.RegisterChanges();
        }
        catch (Exception e)
        {
            Log.Error(e, "Failed to add task");
        }
        Log.Information("Added Tooltip Task");
    }

    private static void DoTaskSchedulerEditPatch(TaskPrincipal principal)
    {
        if (principal.UserId == Environment.UserName)
            principal.UserId = principal.Account;
    }
}