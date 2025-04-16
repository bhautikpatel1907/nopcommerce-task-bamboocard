using Nop.Core.Configuration;

namespace Nop.Plugin.Misc.Api;

public record ApiSettings : ISettings
{
    public bool Enable { get; set; }
}
