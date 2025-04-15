using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Routing;
using Nop.Core;
using Nop.Core.Domain.Discounts;
using Nop.Core.Domain.Payments;
using Nop.Services.Configuration;
using Nop.Services.Customers;
using Nop.Services.Discounts;
using Nop.Services.Localization;
using Nop.Services.Orders;
using Nop.Services.Plugins;

namespace Nop.Plugin.Discount.CustomDiscount
{
    /// <summary>
    /// Custom discount plugin
    /// </summary>
    public class CustomDiscountPlugin : BasePlugin, IDiscountRequirementRule
    {
        #region Fields

        protected readonly IActionContextAccessor _actionContextAccessor;
        protected readonly ICustomerService _customerService;
        protected readonly IDiscountService _discountService;
        protected readonly ILocalizationService _localizationService;
        protected readonly ISettingService _settingService;
        protected readonly IUrlHelperFactory _urlHelperFactory;
        protected readonly IWebHelper _webHelper;
        private readonly IOrderService _orderService;

        #endregion

        #region Ctor

        public CustomDiscountPlugin(IActionContextAccessor actionContextAccessor,
            IDiscountService discountService,
            ICustomerService customerService,
            ILocalizationService localizationService,
            ISettingService settingService,
            IUrlHelperFactory urlHelperFactory,
            IWebHelper webHelper,
            IOrderService orderService)
        {
            _actionContextAccessor = actionContextAccessor;
            _customerService = customerService;
            _discountService = discountService;
            _localizationService = localizationService;
            _settingService = settingService;
            _urlHelperFactory = urlHelperFactory;
            _webHelper = webHelper;
            _orderService = orderService;
        }

        #endregion

        #region Utilities

        private async Task AndDiscountRequirnmentsAsync()
        {
            //add discount 
            var discount = (await _discountService.GetAllDiscountsAsync(DiscountType.AssignedToOrderTotal, discountName: CustomDiscountDefaults.DiscountName)).FirstOrDefault();
            if (discount == null)
            {
                //insert new 
                discount = new Core.Domain.Discounts.Discount
                {
                    Name = CustomDiscountDefaults.DiscountName,
                    AdminComment = "Custom discount plugin. Discount will be applied to the customer having 3 or more order.",
                    DiscountTypeId = (int)DiscountType.AssignedToOrderTotal,
                    UsePercentage = true,
                    DiscountPercentage = 10,
                    IsCumulative = true,
                    IsActive = true
                };

                await _discountService.InsertDiscountAsync(discount);
            }

            //add discount requirement
            var discountRequirement = new DiscountRequirement
            {
                DiscountId = discount.Id,
                DiscountRequirementRuleSystemName = CustomDiscountDefaults.SystemName
            };

            await _discountService.InsertDiscountRequirementAsync(discountRequirement);

            //add default group
            var defaultGroupId = (await _discountService.GetAllDiscountRequirementsAsync(discount.Id, true)).FirstOrDefault(requirement => requirement.IsGroup)?.Id ?? 0;
            if (defaultGroupId == 0)
            {
                //add default requirement group
                var defaultGroup = new DiscountRequirement
                {
                    IsGroup = true,
                    DiscountId = discount.Id,
                    InteractionType = RequirementGroupInteractionType.And,
                    DiscountRequirementRuleSystemName = await _localizationService
                        .GetResourceAsync("Admin.Promotions.Discounts.Requirements.DefaultRequirementGroup")
                };

                await _discountService.InsertDiscountRequirementAsync(defaultGroup);

                defaultGroupId = defaultGroup.Id;
            }
            //default group identifier
            discountRequirement.ParentId = defaultGroupId;

            await _discountService.UpdateDiscountRequirementAsync(discountRequirement);
        }

        private async Task DeleteDiscountRequirnmentsAsync()
        {
            //delete discount
            var discount = (await _discountService.GetAllDiscountsAsync(DiscountType.AssignedToOrderTotal, discountName: CustomDiscountDefaults.DiscountName)).FirstOrDefault();
            if (discount != null)
            {
                await _discountService.DeleteDiscountAsync(discount);
            }

            //discount requirements
            var discountRequirements = (await _discountService.GetAllDiscountRequirementsAsync())
            .Where(discountRequirement => discountRequirement.DiscountRequirementRuleSystemName == CustomDiscountDefaults.SystemName);
            foreach (var discountRequirement in discountRequirements)
            {
                await _discountService.DeleteDiscountRequirementAsync(discountRequirement, false);
            }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Check discount requirement
        /// </summary>
        /// <param name="request">Object that contains all information required to check the requirement (Current customer, discount, etc)</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the result
        /// </returns>
        public async Task<DiscountRequirementValidationResult> CheckRequirementAsync(DiscountRequirementValidationRequest request)
        {
            ArgumentNullException.ThrowIfNull(request);

            //invalid by default
            var result = new DiscountRequirementValidationResult();

            if (request.Customer == null)
                return result;

            //fetch customr orders (paid)
            var orders = await _orderService.SearchOrdersAsync(
                customerId: request.Customer.Id,
                psIds: new List<int> { (int)PaymentStatus.Paid },
                pageIndex: 0,
                pageSize: 3);

            result.IsValid = orders.Count() >= 3;

            return result;
        }

        /// <summary>
        /// Get URL for rule configuration
        /// </summary>
        /// <param name="discountId">Discount identifier</param>
        /// <param name="discountRequirementId">Discount requirement identifier (if editing)</param>
        /// <returns>URL</returns>
        public string GetConfigurationUrl(int discountId, int? discountRequirementId)
        {
            var urlHelper = _urlHelperFactory.GetUrlHelper(_actionContextAccessor.ActionContext);

            return urlHelper.Action("Configure", "CustomDiscount",
                new { discountId = discountId, discountRequirementId = discountRequirementId }, _webHelper.GetCurrentRequestProtocol());
        }

        /// <summary>
        /// Install the plugin
        /// </summary>
        /// <returns>A task that represents the asynchronous operation</returns>
        public override async Task InstallAsync()
        {
            //add discount requirements
            await AndDiscountRequirnmentsAsync();

            //locales
            await _localizationService.AddOrUpdateLocaleResourceAsync(new Dictionary<string, string>
            {
                ["Plugins.DiscountRules.CustomDiscount.Configure.Message"] = "Discount will be applied to the customer having 3 or more completed order.",
            });

            await base.InstallAsync();
        }

        /// <summary>
        /// Uninstall the plugin
        /// </summary>
        /// <returns>A task that represents the asynchronous operation</returns>
        public override async Task UninstallAsync()
        {
            //delete discount requirements
            await DeleteDiscountRequirnmentsAsync();

            //locales
            await _localizationService.DeleteLocaleResourcesAsync("Plugins.DiscountRules.CustomDiscount");

            await base.UninstallAsync();
        }

        #endregion
    }
}
