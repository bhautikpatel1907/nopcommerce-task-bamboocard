using Microsoft.AspNetCore.Mvc;
using Nop.Services.Discounts;
using Nop.Services.Security;
using Nop.Web.Framework;
using Nop.Web.Framework.Controllers;
using Nop.Web.Framework.Mvc.Filters;

namespace Nop.Plugin.DiscountRules.CustomerRoles.Controllers;

[AuthorizeAdmin]
[Area(AreaNames.ADMIN)]
[AutoValidateAntiforgeryToken]
public class CustomDiscountController : BasePluginController
{
    #region Fields

    protected readonly IDiscountService _discountService;
    protected readonly IPermissionService _permissionService;

    #endregion

    #region Ctor

    public CustomDiscountController(IDiscountService discountService,
        IPermissionService permissionService)
    {
        _discountService = discountService;
        _permissionService = permissionService;
    }

    #endregion

    #region Methods

    [CheckPermission(StandardPermission.Promotions.DISCOUNTS_VIEW)]
    public async Task<IActionResult> Configure(int discountId, int? discountRequirementId)
    {
        //load the discount
        var discount = await _discountService.GetDiscountByIdAsync(discountId)
                       ?? throw new ArgumentException("Discount could not be loaded");

        //check whether the discount requirement exists
        if (discountRequirementId.HasValue && await _discountService.GetDiscountRequirementByIdAsync(discountRequirementId.Value) is null)
            return Content("Failed to load requirement.");

        //nothing on config page.

        return View("~/Plugins/Discount.CustomDiscount/Views/Configure.cshtml");
    }

    #endregion
}