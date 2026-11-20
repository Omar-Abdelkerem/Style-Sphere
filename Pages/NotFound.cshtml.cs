using Microsoft.AspNetCore.Mvc.RazorPages;

namespace StyleSphere.Pages;

public class NotFoundModel : PageModel
{
    public int StatusCodeValue { get; set; } = 404;

    public void OnGet(int? code = null)
    {
        StatusCodeValue = code ?? HttpContext.Response.StatusCode;
        if (StatusCodeValue < 400)
        {
            StatusCodeValue = 404;
            HttpContext.Response.StatusCode = 404;
        }
    }
}
