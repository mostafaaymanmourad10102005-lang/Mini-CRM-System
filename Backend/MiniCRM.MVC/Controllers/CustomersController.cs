using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiniCRM.Application;
using MiniCRM.MVC.Models;

namespace MiniCRM.MVC.Controllers;

[Authorize]
public sealed class CustomersController(ICrmService crm) : Controller
{
    public async Task<IActionResult> Index(string? search, int page = 1, CancellationToken ct = default)
    { ViewBag.Search = search; return View(await crm.CustomersAsync(search, page, 20, ct)); }
    public async Task<IActionResult> Details(int id, CancellationToken ct)
    { var customer = await crm.CustomerAsync(id, ct); return customer is null ? NotFound() : View(customer); }
    [HttpGet] public IActionResult Create() => View(new CustomerFormViewModel());
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CustomerFormViewModel model, CancellationToken ct)
    { if (!ModelState.IsValid) return View(model); var saved=await crm.SaveCustomerAsync(model.ToEntity(),ct); TempData["Success"]="Customer created."; return RedirectToAction(nameof(Details),new{id=saved.Id}); }
    [HttpGet] public async Task<IActionResult> Edit(int id,CancellationToken ct)
    { var customer=await crm.CustomerAsync(id,ct); return customer is null?NotFound():View(CustomerFormViewModel.From(customer)); }
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id,CustomerFormViewModel model,CancellationToken ct)
    { if(id!=model.Id)return BadRequest(); if(!ModelState.IsValid)return View(model); try { await crm.SaveCustomerAsync(model.ToEntity(),ct); TempData["Success"]="Customer updated."; return RedirectToAction(nameof(Details),new{id}); } catch(UnauthorizedAccessException){return Forbid();} }
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id,CancellationToken ct)
    { try { var deleted=await crm.DeleteCustomerAsync(id,ct); if(!deleted){TempData["Error"]="This customer has opportunities and cannot be deleted.";return RedirectToAction(nameof(Details),new{id});} TempData["Success"]="Customer deleted.";return RedirectToAction(nameof(Index)); } catch(UnauthorizedAccessException){return Forbid();} }
}
