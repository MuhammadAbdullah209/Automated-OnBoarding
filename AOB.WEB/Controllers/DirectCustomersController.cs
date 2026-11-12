using AOB.BAL.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AOB.WEB.Controllers
{
    /// <summary>
    /// Customers who signed up themselves, for internal users only. Partners never reach this
    /// screen; they only see their own customers through CustomersController.
    /// </summary>
    [Authorize(Roles = AOB.BAL.Common.Constants.AppRoles.InternalAccess)]
    public class DirectCustomersController(ICustomerService customerService) : Controller
    {
        public async Task<IActionResult> Index()
            => View(await customerService.GetDirectCustomersAsync());
    }
}
