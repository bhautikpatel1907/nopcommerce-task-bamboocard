namespace Nop.Plugin.Discount.CustomDiscount;

/// <summary>
/// Represents defaults for the custom discount
/// </summary>
public static class CustomDiscountDefaults
{
    /// <summary>
    /// The system name of the discount requirement rule
    /// </summary>
    public static string SystemName => "DiscountRequirement.MustBeAssignToCustomerHavingThreeOrMoreOrders";

    /// <summary>
    /// The system name of the discount requirement rule
    /// </summary>
    public static string DiscountName => "10% Custom Discount";
}
