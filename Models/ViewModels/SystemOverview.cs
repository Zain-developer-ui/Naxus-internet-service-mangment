namespace NEXUS.Models.ViewModels;

/**
 * The live facts shown beside the settings form. Everything here is a query -
 * nothing is hardcoded, so the panel always describes the running instance.
 */
public sealed class SystemRuntime
{
    public int Customers { get; set; }
    public int ActiveConnections { get; set; }

    public string Environment { get; set; } = string.Empty;
    public string DatabaseName { get; set; } = string.Empty;
    public string ServerTime { get; set; } = string.Empty;
    public string Uptime { get; set; } = string.Empty;
}
