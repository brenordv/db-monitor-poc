using System.Diagnostics;
using System.Text;

namespace SqlServerMonitor.Core.Extensions;

public static class StringBuilderExtensions
{
    
    private static readonly string Line = new ('-', 80);
    
    public static StringBuilder AppendData<T>(this StringBuilder sb, string name, IEnumerable<T> data) where T : notnull
    {
        Debug.Assert(data != null, nameof(data) + " != null");
        var list = data.ToList();

        if (!list.Any())
        {
            sb.AppendLine($"No {name} found.");
            return sb;
        }
        
        sb.AppendLine($"Found {list.Count} {name}.");
        list.ForEach(x =>
        {
            sb.AppendLine(Line);
            sb.AppendLine(x.ToString());
        });
        sb.AppendLine("\n\n");
        return sb;
    }
}