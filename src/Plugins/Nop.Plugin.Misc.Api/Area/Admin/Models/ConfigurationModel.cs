using Nop.Web.Framework.Models;
using Nop.Web.Framework.Mvc.ModelBinding;

namespace Nop.Plugin.Misc.Api.Area.Admin.Models;

/// <summary>
/// Represents configuration model
/// </summary>
public record ConfigurationModel : BaseNopModel
{
    #region Properties

    [NopResourceDisplayName("Plugins.Misc.NopApi.Fields.Enable")]
    public bool Enable { get; set; }

    #endregion
}
