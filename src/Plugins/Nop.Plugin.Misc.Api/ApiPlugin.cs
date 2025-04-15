using Nop.Core;
using Nop.Services.Common;
using Nop.Services.Configuration;
using Nop.Services.Localization;
using Nop.Services.Plugins;

namespace Nop.Plugin.Misc.Api
{
    /// <summary>
    /// Represents the Nop API plugin
    /// </summary>
    public class ApiPlugin : BasePlugin, IMiscPlugin
    {
        #region Fields

        protected readonly IWebHelper _webHelper;
        protected readonly ISettingService _settingService;
        protected readonly ILocalizationService _localizationService;
        protected readonly ApiSettings _apiSettings;

        #endregion

        #region Ctor

        public ApiPlugin(IWebHelper webHelper,
            ISettingService settingService,
            ApiSettings apiSettings,
            ILocalizationService localizationService)
        {
            _webHelper = webHelper;
            _settingService = settingService;
            _apiSettings = apiSettings;
            _localizationService = localizationService;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Gets a configuration page URL
        /// </summary>
        public override string GetConfigurationPageUrl()
        {
            return $"{_webHelper.GetStoreLocation()}Admin/NopApi/Configure";
        }

        /// <summary>
        /// Install the plugin
        /// </summary>
        /// <returns>A task that represents the asynchronous operation</returns>
        public override async Task InstallAsync()
        {
            //add settings
            _apiSettings.Enable = true;
            await _settingService.SaveSettingAsync(_apiSettings);

            //add locale resources
            await _localizationService.AddOrUpdateLocaleResourceAsync(new Dictionary<string, string>
            {
                ["Plugins.Misc.NopApi.Fields.Enable"] = "Enable",
                ["Plugins.Misc.NopApi.Fields.Enable.Hint"] = "Enable the api",
                ["Plugins.Misc.Api.Configuration"] = "Configuration",
            });

            await base.InstallAsync();
        }

        /// <summary>
        /// Uninstall the plugin
        /// </summary>
        /// <returns>A task that represents the asynchronous operation</returns>
        public override async Task UninstallAsync()
        {
            //remove settings
            await _settingService.DeleteSettingAsync<ApiSettings>();

            //remove locale resources
            await _localizationService.DeleteLocaleResourcesAsync("Plugins.Misc.Api");

            await base.UninstallAsync();
        }

        #endregion
    }
}
