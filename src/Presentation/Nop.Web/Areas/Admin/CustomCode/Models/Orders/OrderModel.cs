using Nop.Web.Framework.Mvc.ModelBinding;

namespace Nop.Web.Areas.Admin.Models.Orders;

/// <summary>
/// Extended Represents an order model
/// </summary>
public partial record OrderModel
{
    #region Properties

    [NopResourceDisplayName("Custom.Admin.Orders.Fields.GiftMessage")]
    public string GiftMessage { get; set; }

    #endregion
}