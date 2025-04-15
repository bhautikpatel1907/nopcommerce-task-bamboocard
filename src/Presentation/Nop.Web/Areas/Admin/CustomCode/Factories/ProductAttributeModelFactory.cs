using Nop.Core.Domain.Catalog;
using Nop.Services.Catalog;
using Nop.Services.Localization;
using Nop.Web.Areas.Admin.Infrastructure.Mapper.Extensions;
using Nop.Web.Areas.Admin.Models.Catalog;
using Nop.Web.Framework.Factories;
using Nop.Web.Framework.Models.Extensions;

namespace Nop.Web.Areas.Admin.Factories;

/// <summary>
/// Represents the product attribute model factory implementation
/// </summary>
public partial class ProductAttributeModelFactory : IProductAttributeModelFactory
{
    #region Methods

    /// <summary>
    /// Prepare paged product attribute list model
    /// </summary>
    /// <param name="searchModel">Product attribute search model</param>
    /// <returns>
    /// A task that represents the asynchronous operation
    /// The task result contains the product attribute list model
    /// </returns>
    public virtual async Task<ProductAttributeListModel> PrepareProductAttributeListModelAsync(ProductAttributeSearchModel searchModel)
    {
        ArgumentNullException.ThrowIfNull(searchModel);

        //get product attributes
        var productAttributes = await _productAttributeService
            .GetAllProductAttributesAsync(pageIndex: searchModel.Page - 1, pageSize: searchModel.PageSize, searchModel.SearchAttributeName);

        //prepare list model
        var model = new ProductAttributeListModel().PrepareToGrid(searchModel, productAttributes, () =>
        {
            //fill in model values from the entity
            return productAttributes.Select(attribute => attribute.ToModel<ProductAttributeModel>());

        });

        return model;
    }

    #endregion
}