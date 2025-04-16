using Microsoft.AspNetCore.Mvc;
using Nop.Plugin.Misc.Api.Area.Admin.Models;
using Nop.Services.Configuration;
using Nop.Services.Localization;
using Nop.Services.Messages;
using Nop.Web.Framework;
using Nop.Web.Framework.Controllers;
using Nop.Web.Framework.Mvc.Filters;

namespace Nop.Plugin.Misc.Api.Area.Admin.Controllers;

[Area(AreaNames.ADMIN)]
[AuthorizeAdmin]
[AutoValidateAntiforgeryToken]
public class NopApiController : BasePluginController
{
    #region Fields

    private readonly ISettingService _settingService;
    private readonly ApiSettings _apiSettings;
    private readonly INotificationService _notificationService;
    private readonly ILocalizationService _localizationService;

    #endregion

    #region Ctor

    public NopApiController(ISettingService settingService,
        ApiSettings apiSettings,
        INotificationService notificationService,
        ILocalizationService localizationService)
    {
        _settingService = settingService;
        _apiSettings = apiSettings;
        _notificationService = notificationService;
        _localizationService = localizationService;
    }

    #endregion

    #region Methods

    public async Task<IActionResult> Configure()
    {
        var model = new ConfigurationModel
        {
            Enable = _apiSettings.Enable,
        };

        return View("~/Plugins/Misc.NopApi/Area/Admin/Views/Configure.cshtml", model);
    }

    [HttpPost, ActionName("Configure")]
    [FormValueRequired("save")]
    public async Task<IActionResult> Configure(ConfigurationModel model)
    {
        _apiSettings.Enable = model.Enable;

        await _settingService.SaveSettingAsync(_apiSettings);

        _notificationService.SuccessNotification(await _localizationService.GetResourceAsync("Admin.Plugins.Saved"));

        return await Configure();
    }

    #endregion
}
