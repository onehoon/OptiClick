using System;

namespace OptiClick.Wpf.Install.Flow;

public static class InstallFlowLogEntryFactory
{
    public static InstallFlowLogEntry Info(string category, string message)
    {
        return new InstallFlowLogEntry
        {
            Level = "info",
            Category = category,
            Message = message ?? ""
        };
    }

    public static InstallFlowLogEntry Warning(string category, string message)
    {
        return new InstallFlowLogEntry
        {
            Level = "warning",
            Category = category,
            Message = message ?? ""
        };
    }

    public static InstallFlowLogEntry Error(string category, string message, Exception? exception = null)
    {
        return new InstallFlowLogEntry
        {
            Level = "error",
            Category = category,
            Message = message ?? "",
            Exception = exception
        };
    }
}
