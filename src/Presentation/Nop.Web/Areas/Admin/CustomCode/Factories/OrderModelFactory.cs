using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.Routing;
using Newtonsoft.Json;
using Nop.Core;
using Nop.Core.CustomCode.Domain;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Common;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Directory;
using Nop.Core.Domain.Orders;
using Nop.Core.Domain.Payments;
using Nop.Core.Domain.Shipping;
using Nop.Core.Domain.Tax;
using Nop.Core.Infrastructure;
using Nop.Services.Affiliates;
using Nop.Services.Catalog;
using Nop.Services.Common;
using Nop.Services.Configuration;
using Nop.Services.Customers;
using Nop.Services.Directory;
using Nop.Services.Discounts;
using Nop.Services.Helpers;
using Nop.Services.Localization;
using Nop.Services.Media;
using Nop.Services.Orders;
using Nop.Services.Payments;
using Nop.Services.Security;
using Nop.Services.Seo;
using Nop.Services.Shipping;
using Nop.Services.Stores;
using Nop.Services.Tax;
using Nop.Services.Vendors;
using Nop.Web.Areas.Admin.Infrastructure.Mapper.Extensions;
using Nop.Web.Areas.Admin.Models.Catalog;
using Nop.Web.Areas.Admin.Models.Common;
using Nop.Web.Areas.Admin.Models.Orders;
using Nop.Web.Areas.Admin.Models.Reports;
using Nop.Web.Framework.Extensions;
using Nop.Web.Framework.Models.Extensions;

namespace Nop.Web.Areas.Admin.Factories;

/// <summary>
/// Represents the order model factory implementation
/// </summary>
public partial class OrderModelFactory
{
    #region Methods

    /// <summary>
    /// Prepare order model
    /// </summary>
    /// <param name="model">Order model</param>
    /// <param name="order">Order</param>
    /// <param name="excludeProperties">Whether to exclude populating of some properties of model</param>
    /// <returns>
    /// A task that represents the asynchronous operation
    /// The task result contains the order model
    /// </returns>
    public virtual async Task<OrderModel> PrepareOrderModelAsync(OrderModel model, Order order, bool excludeProperties = false)
    {
        if (order != null)
        {
            //fill in model values from the entity
            model ??= new OrderModel
            {
                Id = order.Id,
                OrderStatusId = order.OrderStatusId,
                VatNumber = order.VatNumber,
                CheckoutAttributeInfo = order.CheckoutAttributeDescription
            };

            var customer = await _customerService.GetCustomerByIdAsync(order.CustomerId);

            model.OrderGuid = order.OrderGuid;
            model.CustomOrderNumber = order.CustomOrderNumber;
            model.CustomerIp = order.CustomerIp;
            model.CustomerId = customer.Id;
            model.OrderStatus = await _localizationService.GetLocalizedEnumAsync(order.OrderStatus);
            model.StoreName = (await _storeService.GetStoreByIdAsync(order.StoreId))?.Name ?? "Deleted";
            model.CustomerInfo = await _customerService.IsRegisteredAsync(customer) ? customer.Email : await _localizationService.GetResourceAsync("Admin.Customers.Guest");
            model.CreatedOn = await _dateTimeHelper.ConvertToUserTimeAsync(order.CreatedOnUtc, DateTimeKind.Utc);
            model.CustomValues = _paymentService.DeserializeCustomValues(order);

            var affiliate = await _affiliateService.GetAffiliateByIdAsync(order.AffiliateId);
            if (affiliate != null)
            {
                model.AffiliateId = affiliate.Id;
                model.AffiliateName = await _affiliateService.GetAffiliateFullNameAsync(affiliate);
            }

            //prepare order totals
            await PrepareOrderModelTotalsAsync(model, order);

            //prepare order items
            await PrepareOrderItemModelsAsync(model.Items, order);
            model.HasDownloadableProducts = model.Items.Any(item => item.IsDownload);

            //prepare payment info
            await PrepareOrderModelPaymentInfoAsync(model, order);

            //prepare shipping info
            await PrepareOrderModelShippingInfoAsync(model, order);

            //prepare nested search model
            PrepareOrderShipmentSearchModel(model.OrderShipmentSearchModel, order);
            PrepareOrderNoteSearchModel(model.OrderNoteSearchModel, order);
        }

        model.IsLoggedInAsVendor = await _workContext.GetCurrentVendorAsync() != null;
        model.AllowCustomersToSelectTaxDisplayType = _taxSettings.AllowCustomersToSelectTaxDisplayType;
        model.TaxDisplayType = _taxSettings.TaxDisplayType;

        //++Custom code

        var genericAttributeService = EngineContext.Current.Resolve<IGenericAttributeService>();
        model.GiftMessage = await genericAttributeService.GetAttributeAsync<string>(order, CustomOrderDefaults.GiftMessageGenericAttributeKey, order.StoreId);

        //--Custom code

        return model;
    }

    #endregion
}