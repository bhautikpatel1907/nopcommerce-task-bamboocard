namespace Nop.Plugin.Misc.Api.DTOs;

public class BaseApiResponse<T>
{
    public bool Success { get; set; }
    public string Message { get; set; }
    public T Data { get; set; }
}
