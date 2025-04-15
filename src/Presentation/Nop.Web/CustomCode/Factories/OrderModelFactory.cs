using Nop.Core;
using Nop.Core.Caching;
using Nop.Core.CustomCode.Domain;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Common;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Directory;
using Nop.Core.Domain.Media;
using Nop.Core.Domain.Orders;
using Nop.Core.Domain.Shipping;
using Nop.Core.Domain.Tax;
using Nop.Core.Domain.Vendors;
using Nop.Core.Infrastructure;
using Nop.Services.Catalog;
using Nop.Services.Common;
using Nop.Services.Customers;
using Nop.Services.Directory;
using Nop.Services.Helpers;
using Nop.Services.Localization;
using Nop.Services.Media;
using Nop.Services.Orders;
using Nop.Services.Payments;
using Nop.Services.Seo;
using Nop.Services.Shipping;
using Nop.Services.Vendors;
using Nop.Web.Infrastructure;
using Nop.Web.Infrastructure.Cache;
using Nop.Web.Models.Common;
using Nop.Web.Models.Media;
using Nop.Web.Models.Order;

namespace Nop.Web.Factories;

/// <summary>
/// Represents the order model factory
/// </summary>
public partial class OrderModelFactory
{
    #region Methods

    /// <summary>
    /// Prepare the order details model
    /// </summary>
    /// <param name="order">Order</param>
    /// <returns>
    /// A task that represents the asynchronous operation
    /// The task result contains the order details model
    /// </returns>
    public virtual async Task<OrderDetailsModel> PrepareOrderDetailsModelAsync(Order order)
    {
        ArgumentNullException.ThrowIfNull(order);
        var model = new OrderDetailsModel
        {
            Id = order.Id,
            CreatedOn = await _dateTimeHelper.ConvertToUserTimeAsync(order.CreatedOnUtc, DateTimeKind.Utc),
            OrderStatus = await _localizationService.GetLocalizedEnumAsync(order.OrderStatus),
            IsReOrderAllowed = _orderSettings.IsReOrderAllowed,
            IsReturnRequestAllowed = await _orderProcessingService.IsReturnRequestAllowedAsync(order),
            PdfInvoiceDisabled = _pdfSettings.DisablePdfInvoicesForPendingOrders && order.OrderStatus == OrderStatus.Pending,
            CustomOrderNumber = order.CustomOrderNumber,

            //shipping info
            ShippingStatus = await _localizationService.GetLocalizedEnumAsync(order.ShippingStatus)
        };

        var languageId = (await _workContext.GetWorkingLanguageAsync()).Id;

        if (order.ShippingStatus != ShippingStatus.ShippingNotRequired)
        {
            model.IsShippable = true;
            model.PickupInStore = order.PickupInStore;
            if (!order.PickupInStore)
            {
                var shippingAddress = await _addressService.GetAddressByIdAsync(order.ShippingAddressId ?? 0);

                await _addressModelFactory.PrepareAddressModelAsync(model.ShippingAddress,
                    address: shippingAddress,
                    excludeProperties: false,
                    addressSettings: _addressSettings);
            }
            else if (order.PickupAddressId.HasValue && await _addressService.GetAddressByIdAsync(order.PickupAddressId.Value) is Address pickupAddress)
            {
                var (addressLine, addressFields) = await _addressService.FormatAddressAsync(pickupAddress, languageId);
                model.PickupAddress = new AddressModel
                {
                    Address1 = pickupAddress.Address1,
                    City = pickupAddress.City,
                    County = pickupAddress.County,
                    StateProvinceName = await _stateProvinceService.GetStateProvinceByAddressAsync(pickupAddress) is StateProvince stateProvince
                        ? await _localizationService.GetLocalizedAsync(stateProvince, entity => entity.Name)
                        : string.Empty,
                    CountryName = await _countryService.GetCountryByAddressAsync(pickupAddress) is Country country
                        ? await _localizationService.GetLocalizedAsync(country, entity => entity.Name)
                        : string.Empty,
                    ZipPostalCode = pickupAddress.ZipPostalCode,
                    AddressFields = addressFields,
                    AddressLine = addressLine
                };
            }

            model.ShippingMethod = order.ShippingMethod;

            //shipments (only already shipped or ready for pickup)
            var shipments = (await _shipmentService.GetShipmentsByOrderIdAsync(order.Id, !order.PickupInStore, order.PickupInStore)).OrderBy(x => x.CreatedOnUtc).ToList();
            foreach (var shipment in shipments)
            {
                var shipmentModel = new OrderDetailsModel.ShipmentBriefModel
                {
                    Id = shipment.Id,
                    TrackingNumber = shipment.TrackingNumber,
                };
                if (shipment.ShippedDateUtc.HasValue)
                    shipmentModel.ShippedDate = await _dateTimeHelper.ConvertToUserTimeAsync(shipment.ShippedDateUtc.Value, DateTimeKind.Utc);
                if (shipment.ReadyForPickupDateUtc.HasValue)
                    shipmentModel.ReadyForPickupDate = await _dateTimeHelper.ConvertToUserTimeAsync(shipment.ReadyForPickupDateUtc.Value, DateTimeKind.Utc);
                if (shipment.DeliveryDateUtc.HasValue)
                    shipmentModel.DeliveryDate = await _dateTimeHelper.ConvertToUserTimeAsync(shipment.DeliveryDateUtc.Value, DateTimeKind.Utc);
                model.Shipments.Add(shipmentModel);
            }
        }

        var billingAddress = await _addressService.GetAddressByIdAsync(order.BillingAddressId);

        //billing info
        await _addressModelFactory.PrepareAddressModelAsync(model.BillingAddress,
            address: billingAddress,
            excludeProperties: false,
            addressSettings: _addressSettings);

        //VAT number
        model.VatNumber = order.VatNumber;

        //payment method
        var customer = await _customerService.GetCustomerByIdAsync(order.CustomerId);
        var paymentMethod = await _paymentPluginManager
            .LoadPluginBySystemNameAsync(order.PaymentMethodSystemName, customer, order.StoreId);
        model.PaymentMethod = paymentMethod != null ? await _localizationService.GetLocalizedFriendlyNameAsync(paymentMethod, languageId) : order.PaymentMethodSystemName;
        model.PaymentMethodStatus = await _localizationService.GetLocalizedEnumAsync(order.PaymentStatus);
        model.CanRePostProcessPayment = await _paymentService.CanRePostProcessPaymentAsync(order);
        //custom values
        model.CustomValues = _paymentService.DeserializeCustomValues(order);

        //order subtotal
        if (order.CustomerTaxDisplayType == TaxDisplayType.IncludingTax && !_taxSettings.ForceTaxExclusionFromOrderSubtotal)
        {
            //including tax

            //order subtotal
            var orderSubtotalInclTaxInCustomerCurrency = _currencyService.ConvertCurrency(order.OrderSubtotalInclTax, order.CurrencyRate);
            model.OrderSubtotal = await _priceFormatter.FormatPriceAsync(orderSubtotalInclTaxInCustomerCurrency, true, order.CustomerCurrencyCode, languageId, true);
            model.OrderSubtotalValue = orderSubtotalInclTaxInCustomerCurrency;
            //discount (applied to order subtotal)
            var orderSubTotalDiscountInclTaxInCustomerCurrency = _currencyService.ConvertCurrency(order.OrderSubTotalDiscountInclTax, order.CurrencyRate);
            if (orderSubTotalDiscountInclTaxInCustomerCurrency > decimal.Zero)
            {
                model.OrderSubTotalDiscount = await _priceFormatter.FormatPriceAsync(-orderSubTotalDiscountInclTaxInCustomerCurrency, true, order.CustomerCurrencyCode, languageId, true);
                model.OrderSubTotalDiscountValue = orderSubTotalDiscountInclTaxInCustomerCurrency;
            }
        }
        else
        {
            //excluding tax

            //order subtotal
            var orderSubtotalExclTaxInCustomerCurrency = _currencyService.ConvertCurrency(order.OrderSubtotalExclTax, order.CurrencyRate);
            model.OrderSubtotal = await _priceFormatter.FormatPriceAsync(orderSubtotalExclTaxInCustomerCurrency, true, order.CustomerCurrencyCode, languageId, false);
            model.OrderSubtotalValue = orderSubtotalExclTaxInCustomerCurrency;
            //discount (applied to order subtotal)
            var orderSubTotalDiscountExclTaxInCustomerCurrency = _currencyService.ConvertCurrency(order.OrderSubTotalDiscountExclTax, order.CurrencyRate);
            if (orderSubTotalDiscountExclTaxInCustomerCurrency > decimal.Zero)
            {
                model.OrderSubTotalDiscount = await _priceFormatter.FormatPriceAsync(-orderSubTotalDiscountExclTaxInCustomerCurrency, true, order.CustomerCurrencyCode, languageId, false);
                model.OrderSubTotalDiscountValue = orderSubTotalDiscountExclTaxInCustomerCurrency;
            }
        }

        if (order.CustomerTaxDisplayType == TaxDisplayType.IncludingTax)
        {
            //including tax

            //order shipping
            var orderShippingInclTaxInCustomerCurrency = _currencyService.ConvertCurrency(order.OrderShippingInclTax, order.CurrencyRate);
            model.OrderShipping = await _priceFormatter.FormatShippingPriceAsync(orderShippingInclTaxInCustomerCurrency, true, order.CustomerCurrencyCode, languageId, true);
            model.OrderShippingValue = orderShippingInclTaxInCustomerCurrency;
            //payment method additional fee
            var paymentMethodAdditionalFeeInclTaxInCustomerCurrency = _currencyService.ConvertCurrency(order.PaymentMethodAdditionalFeeInclTax, order.CurrencyRate);
            if (paymentMethodAdditionalFeeInclTaxInCustomerCurrency > decimal.Zero)
            {
                model.PaymentMethodAdditionalFee = await _priceFormatter.FormatPaymentMethodAdditionalFeeAsync(paymentMethodAdditionalFeeInclTaxInCustomerCurrency, true, order.CustomerCurrencyCode, languageId, true);
                model.PaymentMethodAdditionalFeeValue = paymentMethodAdditionalFeeInclTaxInCustomerCurrency;
            }
        }
        else
        {
            //excluding tax

            //order shipping
            var orderShippingExclTaxInCustomerCurrency = _currencyService.ConvertCurrency(order.OrderShippingExclTax, order.CurrencyRate);
            model.OrderShipping = await _priceFormatter.FormatShippingPriceAsync(orderShippingExclTaxInCustomerCurrency, true, order.CustomerCurrencyCode, languageId, false);
            model.OrderShippingValue = orderShippingExclTaxInCustomerCurrency;
            //payment method additional fee
            var paymentMethodAdditionalFeeExclTaxInCustomerCurrency = _currencyService.ConvertCurrency(order.PaymentMethodAdditionalFeeExclTax, order.CurrencyRate);
            if (paymentMethodAdditionalFeeExclTaxInCustomerCurrency > decimal.Zero)
            {
                model.PaymentMethodAdditionalFee = await _priceFormatter.FormatPaymentMethodAdditionalFeeAsync(paymentMethodAdditionalFeeExclTaxInCustomerCurrency, true, order.CustomerCurrencyCode, languageId, false);
                model.PaymentMethodAdditionalFeeValue = paymentMethodAdditionalFeeExclTaxInCustomerCurrency;
            }
        }

        //tax
        var displayTax = true;
        var displayTaxRates = true;
        if (_taxSettings.HideTaxInOrderSummary && order.CustomerTaxDisplayType == TaxDisplayType.IncludingTax)
        {
            displayTax = false;
            displayTaxRates = false;
        }
        else
        {
            if (order.OrderTax == 0 && _taxSettings.HideZeroTax)
            {
                displayTax = false;
                displayTaxRates = false;
            }
            else
            {
                var taxRates = _orderService.ParseTaxRates(order, order.TaxRates);
                displayTaxRates = _taxSettings.DisplayTaxRates && taxRates.Any();
                displayTax = !displayTaxRates;

                var orderTaxInCustomerCurrency = _currencyService.ConvertCurrency(order.OrderTax, order.CurrencyRate);
                model.Tax = await _priceFormatter.FormatPriceAsync(orderTaxInCustomerCurrency, true, order.CustomerCurrencyCode, false, languageId);

                foreach (var tr in taxRates)
                {
                    model.TaxRates.Add(new OrderDetailsModel.TaxRate
                    {
                        Rate = _priceFormatter.FormatTaxRate(tr.Key),
                        Value = await _priceFormatter.FormatPriceAsync(_currencyService.ConvertCurrency(tr.Value, order.CurrencyRate), true, order.CustomerCurrencyCode, false, languageId),
                    });
                }
            }
        }
        model.DisplayTaxRates = displayTaxRates;
        model.DisplayTax = displayTax;
        model.DisplayTaxShippingInfo = _catalogSettings.DisplayTaxShippingInfoOrderDetailsPage;
        model.PricesIncludeTax = order.CustomerTaxDisplayType == TaxDisplayType.IncludingTax;

        //discount (applied to order total)
        var orderDiscountInCustomerCurrency = _currencyService.ConvertCurrency(order.OrderDiscount, order.CurrencyRate);
        if (orderDiscountInCustomerCurrency > decimal.Zero)
        {
            model.OrderTotalDiscount = await _priceFormatter.FormatPriceAsync(-orderDiscountInCustomerCurrency, true, order.CustomerCurrencyCode, false, languageId);
            model.OrderTotalDiscountValue = orderDiscountInCustomerCurrency;
        }

        //gift cards
        foreach (var gcuh in await _giftCardService.GetGiftCardUsageHistoryAsync(order))
        {
            model.GiftCards.Add(new OrderDetailsModel.GiftCard
            {
                CouponCode = (await _giftCardService.GetGiftCardByIdAsync(gcuh.GiftCardId)).GiftCardCouponCode,
                Amount = await _priceFormatter.FormatPriceAsync(-(_currencyService.ConvertCurrency(gcuh.UsedValue, order.CurrencyRate)), true, order.CustomerCurrencyCode, false, languageId),
            });
        }

        //reward points           
        if (order.RedeemedRewardPointsEntryId.HasValue && await _rewardPointService.GetRewardPointsHistoryEntryByIdAsync(order.RedeemedRewardPointsEntryId.Value) is RewardPointsHistory redeemedRewardPointsEntry)
        {
            model.RedeemedRewardPoints = -redeemedRewardPointsEntry.Points;
            model.RedeemedRewardPointsAmount = await _priceFormatter.FormatPriceAsync(-(_currencyService.ConvertCurrency(redeemedRewardPointsEntry.UsedAmount, order.CurrencyRate)), true, order.CustomerCurrencyCode, false, languageId);
        }

        //total
        var orderTotalInCustomerCurrency = _currencyService.ConvertCurrency(order.OrderTotal, order.CurrencyRate);
        model.OrderTotal = await _priceFormatter.FormatPriceAsync(orderTotalInCustomerCurrency, true, order.CustomerCurrencyCode, false, languageId);
        model.OrderTotalValue = orderTotalInCustomerCurrency;

        //checkout attributes
        model.CheckoutAttributeInfo = order.CheckoutAttributeDescription;

        //order notes
        foreach (var orderNote in (await _orderService.GetOrderNotesByOrderIdAsync(order.Id, true))
                 .OrderByDescending(on => on.CreatedOnUtc)
                 .ToList())
        {
            model.OrderNotes.Add(new OrderDetailsModel.OrderNote
            {
                Id = orderNote.Id,
                HasDownload = orderNote.DownloadId > 0,
                Note = _orderService.FormatOrderNoteText(orderNote),
                CreatedOn = await _dateTimeHelper.ConvertToUserTimeAsync(orderNote.CreatedOnUtc, DateTimeKind.Utc)
            });
        }

        //purchased products
        model.ShowSku = _catalogSettings.ShowSkuOnProductDetailsPage;
        model.ShowVendorName = _vendorSettings.ShowVendorOnOrderDetailsPage;
        model.ShowProductThumbnail = _orderSettings.ShowProductThumbnailInOrderDetailsPage;

        var orderItems = await _orderService.GetOrderItemsAsync(order.Id);

        foreach (var orderItem in orderItems)
        {
            var product = await _productService.GetProductByIdAsync(orderItem.ProductId);

            var orderItemModel = new OrderDetailsModel.OrderItemModel
            {
                Id = orderItem.Id,
                OrderItemGuid = orderItem.OrderItemGuid,
                Sku = await _productService.FormatSkuAsync(product, orderItem.AttributesXml),
                VendorName = (await _vendorService.GetVendorByIdAsync(product.VendorId))?.Name ?? string.Empty,
                ProductId = (product.ParentGroupedProductId > 0 && !product.VisibleIndividually) ? product.ParentGroupedProductId : product.Id,
                ProductName = await _localizationService.GetLocalizedAsync(product, x => x.Name),
                ProductSeName = await _urlRecordService.GetSeNameAsync(product),
                Quantity = orderItem.Quantity,
                AttributeInfo = orderItem.AttributeDescription,
            };
            //rental info
            if (product.IsRental)
            {
                var rentalStartDate = orderItem.RentalStartDateUtc.HasValue
                    ? _productService.FormatRentalDate(product, orderItem.RentalStartDateUtc.Value) : "";
                var rentalEndDate = orderItem.RentalEndDateUtc.HasValue
                    ? _productService.FormatRentalDate(product, orderItem.RentalEndDateUtc.Value) : "";
                orderItemModel.RentalInfo = string.Format(await _localizationService.GetResourceAsync("Order.Rental.FormattedDate"),
                    rentalStartDate, rentalEndDate);
            }
            model.Items.Add(orderItemModel);

            //unit price, subtotal
            if (order.CustomerTaxDisplayType == TaxDisplayType.IncludingTax)
            {
                //including tax
                var unitPriceInclTaxInCustomerCurrency = _currencyService.ConvertCurrency(orderItem.UnitPriceInclTax, order.CurrencyRate);
                orderItemModel.UnitPrice = await _priceFormatter.FormatPriceAsync(unitPriceInclTaxInCustomerCurrency, true, order.CustomerCurrencyCode, languageId, true);
                orderItemModel.UnitPriceValue = unitPriceInclTaxInCustomerCurrency;

                var priceInclTaxInCustomerCurrency = _currencyService.ConvertCurrency(orderItem.PriceInclTax, order.CurrencyRate);
                orderItemModel.SubTotal = await _priceFormatter.FormatPriceAsync(priceInclTaxInCustomerCurrency, true, order.CustomerCurrencyCode, languageId, true);
                orderItemModel.SubTotalValue = priceInclTaxInCustomerCurrency;
            }
            else
            {
                //excluding tax
                var unitPriceExclTaxInCustomerCurrency = _currencyService.ConvertCurrency(orderItem.UnitPriceExclTax, order.CurrencyRate);
                orderItemModel.UnitPrice = await _priceFormatter.FormatPriceAsync(unitPriceExclTaxInCustomerCurrency, true, order.CustomerCurrencyCode, languageId, false);
                orderItemModel.UnitPriceValue = unitPriceExclTaxInCustomerCurrency;

                var priceExclTaxInCustomerCurrency = _currencyService.ConvertCurrency(orderItem.PriceExclTax, order.CurrencyRate);
                orderItemModel.SubTotal = await _priceFormatter.FormatPriceAsync(priceExclTaxInCustomerCurrency, true, order.CustomerCurrencyCode, languageId, false);
                orderItemModel.SubTotalValue = priceExclTaxInCustomerCurrency;
            }

            //downloadable products
            if (await _orderService.IsDownloadAllowedAsync(orderItem))
                orderItemModel.DownloadId = product.DownloadId;
            if (await _orderService.IsLicenseDownloadAllowedAsync(orderItem))
                orderItemModel.LicenseId = orderItem.LicenseDownloadId ?? 0;

            if (_orderSettings.ShowProductThumbnailInOrderDetailsPage)
            {
                orderItemModel.Picture = await PrepareOrderItemPictureModelAsync(orderItem, _mediaSettings.OrderThumbPictureSize, true, orderItemModel.ProductName);
            }
        }

        //++Custom code

        var genericAttributeService = EngineContext.Current.Resolve<IGenericAttributeService>();
        model.GiftMessage = await genericAttributeService.GetAttributeAsync<string>(order, CustomOrderDefaults.GiftMessageGenericAttributeKey, order.StoreId);

        //--Custom code

        return model;
    }

    #endregion
}