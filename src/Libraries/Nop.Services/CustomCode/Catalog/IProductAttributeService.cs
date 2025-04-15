using Nop.Core;
using Nop.Core.Domain.Catalog;

namespace Nop.Services.Catalog;

/// <summary>
/// Product attribute service interface
/// </summary>
public partial interface IProductAttributeService
{
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
    Task<IPagedList<ProductAttribute>> GetAllProductAttributesAsync(int pageIndex = 0, int pageSize = int.MaxValue, string name = "");

    #endregion
}