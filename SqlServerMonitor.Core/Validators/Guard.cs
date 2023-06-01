namespace SqlServerMonitor.Core.Validators;

public static class Guard
{
    public static void AgainstNull(object value, string name)
    {
        if (value is not null) return;
        throw new ArgumentNullException(name);
    }
}