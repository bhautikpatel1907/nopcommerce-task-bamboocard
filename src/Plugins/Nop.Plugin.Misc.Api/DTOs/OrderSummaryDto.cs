namespace Nop.Plugin.Misc.Api.DTOs;

public class OrderSummaryDto
{
    public int OrderId { get; set; }
    public string TotalAmount { get; set; }
    public DateTime OrderDate { get; set; }
}