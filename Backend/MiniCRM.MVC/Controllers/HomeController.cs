using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using MiniCRM.Application;

namespace MiniCRM.MVC.Controllers;
[Authorize]
public sealed class HomeController(ICrmService crm) : Controller
{
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        ViewBag.Summary = await crm.DashboardAsync(ct);
        ViewBag.RecentCustomers = (await crm.CustomersAsync(null, 1, 5, ct)).Items;
        ViewBag.TodayTasks = (await crm.TasksAsync(1, 5, ct)).Items;
        return View();
    }
    public IActionResult Error() => View("Error");
}
