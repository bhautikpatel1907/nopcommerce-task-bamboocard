using Nop.Web.Framework.Mvc.ModelBinding;

namespace Nop.Web.Areas.Admin.Models.Catalog;

/// <summary>
/// Represents a product attribute search model
/// </summary>
public partial record ProductAttributeSearchModel
{
    #region Properties

    [NopResourceDisplayName("Custom.Admin.Catalog.Categories.List.SearchAttributeName")]
    public string SearchAttributeName { get; set; }

    #endregion
}