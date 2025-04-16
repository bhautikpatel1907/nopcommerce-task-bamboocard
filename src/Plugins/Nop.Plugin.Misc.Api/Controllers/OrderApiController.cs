using Microsoft.AspNetCore.Mvc;
using Nop.Plugin.Misc.Api.DTOs;
using Nop.Services.Catalog;
using Nop.Services.Customers;
using Nop.Services.Helpers;
using Nop.Services.Orders;

namespace Nop.Plugin.Misc.Api.Controllers;

[Route("api/orders")]
public class OrderApiController : BaseApiController
{
    private readonly IOrderService _orderService;
    private readonly ICustomerService _customerService;
    private readonly IPriceFormatter _priceFormatter;
    private readonly IDateTimeHelper _dateTimeHelper;

    public OrderApiController(IOrderService orderService,
        ICustomerService customerService,
        IPriceFormatter priceFormatter,
        IDateTimeHelper dateTimeHelper)
    {
        _orderService = orderService;
        _customerService = customerService;
        _priceFormatter = priceFormatter;
        _dateTimeHelper = dateTimeHelper;
    }

    [HttpGet("{email}")]
    public async Task<IActionResult> GetOrdersByEmail(string email)
    {
        try
        {
            //get customer by email
            var customer = await _customerService.GetCustomerByEmailAsync(email);
            if (customer == null)
                return BadRequestResponse("Customer not found with the specific email.");

            //get customer orders
            var orders = await _orderService.SearchOrdersAsync(customerId: customer.Id);

            var result = new List<OrderSummaryDto>();
            foreach (var o in orders)
            {
                result.Add(new OrderSummaryDto
                {
                    OrderId = o.Id,
                    TotalAmount = await _priceFormatter.FormatPriceAsync(o.OrderTotal, true, false),
                    OrderDate = await _dateTimeHelper.ConvertToUserTimeAsync(o.CreatedOnUtc, DateTimeKind.Utc)
                });
            }

            return OkResponse(result);
        }
        catch (Exception ex)
        {
            return ErrorResponse("An unexpected error occurred: " + ex.Message);
        }
    }
}
