using Nop.Core;
using Nop.Core.Domain.Catalog;

namespace Nop.Services.Catalog;

/// <summary>
/// Product attribute service
/// </summary>
public partial class ProductAttributeService
{
    #region Methods

    #region Product attributes

    /// <summary>
    /// Gets all product attributes
    /// </summary>
    /// <param name="pageIndex">Page index</param>
    /// <param name="pageSize">Page size</param>
    /// <returns>
    /// A task that represents the asynchronous operation
    /// The task result contains the product attributes
    /// </returns>
    public virtual async Task<IPagedList<ProductAttribute>> GetAllProductAttributesAsync(int pageIndex = 0,
        int pageSize = int.MaxValue, string name = "")
    {
        var productAttributes = await _productAttributeRepository.GetAllPagedAsync(query =>
        {
            return from pa in query
                   orderby pa.Name
                   where string.IsNullOrEmpty(name) || pa.Name.Contains(name)
                   select pa;
        }, pageIndex, pageSize);

        return productAttributes;
    }

    #endregion

    #endregion
}