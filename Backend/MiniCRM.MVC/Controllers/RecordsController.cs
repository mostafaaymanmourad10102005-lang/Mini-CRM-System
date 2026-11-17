using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiniCRM.Application;
using MiniCRM.Domain;
using MiniCRM.MVC.Models;

namespace MiniCRM.MVC.Controllers;

[Authorize]
public sealed class RecordsController(ICrmService crm) : Controller
{
    private static string Name(string kind) => kind switch { "leads"=>"Leads", "opportunities"=>"Opportunities", "tasks"=>"Tasks", "contacts"=>"Contacts", "notes"=>"Notes", "activities"=>"Activities", _=>"" };

    public async Task<IActionResult> Index(string kind, string? search, int page=1, CancellationToken ct=default)
    {
        var title=Name(kind); if(title.Length==0)return NotFound();
        IReadOnlyList<RecordRowViewModel> rows; int total;
        switch(kind)
        {
            case "leads": { var p=await crm.LeadsAsync(search,page,20,ct); rows=p.Items.Select(x=>new RecordRowViewModel{Id=x.Id,Primary=x.Title,Secondary=x.ContactName??x.Email,Status=x.Status.ToString(),Related=x.EstimatedValue.ToString("C"),Date=x.CreatedAt}).ToArray(); total=p.TotalCount; break; }
            case "opportunities": { var p=await crm.OpportunitiesAsync(page,20,ct); rows=p.Items.Select(x=>new RecordRowViewModel{Id=x.Id,Primary=x.Title,Secondary=$"Customer #{x.CustomerId}",Status=x.Stage.ToString(),Related=x.Amount.ToString("C"),Date=x.ExpectedCloseDate}).ToArray(); total=p.TotalCount; break; }
            case "tasks": { var p=await crm.TasksAsync(page,20,ct); rows=p.Items.Select(x=>new RecordRowViewModel{Id=x.Id,Primary=x.Title,Secondary=x.Description,Status=$"{x.Status} · {x.Priority}",Related=x.CustomerId is int id?$"Customer #{id}":null,Date=x.DueDate}).ToArray(); total=p.TotalCount; break; }
            case "contacts": { var p=await crm.ContactsAsync(page,20,ct); rows=p.Items.Select(x=>new RecordRowViewModel{Id=x.Id,Primary=x.FullName,Secondary=x.Email,Status=x.IsPrimary?"Primary":null,Related=$"Customer #{x.CustomerId}"}).ToArray(); total=p.TotalCount; break; }
            case "notes": { var p=await crm.NotesAsync(page,20,ct); rows=p.Items.Select(x=>new RecordRowViewModel{Id=x.Id,Primary=x.Content,Related=Linked(x.CustomerId,x.LeadId,x.OpportunityId),Date=x.CreatedAt}).ToArray(); total=p.TotalCount; break; }
            default: { var p=await crm.ActivitiesAsync(page,20,ct); rows=p.Items.Select(x=>new RecordRowViewModel{Id=x.Id,Primary=x.Description,Secondary=x.ActivityType.ToString(),Related=Linked(x.CustomerId,x.LeadId,x.OpportunityId),Date=x.CreatedAt}).ToArray(); total=p.TotalCount; break; }
        }
        ViewBag.Search=search;
        return View(new RecordListViewModel{Kind=kind,Title=title,Items=rows,Page=page,PageSize=20,TotalCount=total,CanCreate=true});
    }

    public async Task<IActionResult> Create(string kind, CancellationToken ct) { if(Name(kind).Length==0)return NotFound(); var model=new RecordFormViewModel{Kind=kind}; return View("Form",await LoadOptions(model,ct)); }

    public async Task<IActionResult> Edit(string kind,int id,CancellationToken ct)
    {
        RecordFormViewModel? m=kind switch { "leads"=>Map(await crm.LeadAsync(id,ct),RecordFormViewModel.From), "opportunities"=>Map(await crm.OpportunityAsync(id,ct),RecordFormViewModel.From), "tasks"=>Map(await crm.TaskAsync(id,ct),RecordFormViewModel.From), "contacts"=>Map(await crm.ContactAsync(id,ct),RecordFormViewModel.From), _=>null };
        if(m is null)return NotFound(); return View("Form",await LoadOptions(m,ct));
    }

    [HttpPost,ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(RecordFormViewModel model,CancellationToken ct)
    {
        if(Name(model.Kind).Length==0)return NotFound();
        if(!ModelState.IsValid)return View("Form",await LoadOptions(model,ct));
        switch(model.Kind)
        {
            case "leads": await crm.SaveLeadAsync(new Lead{Id=model.Id,Title=model.Title!.Trim(),CustomerId=model.CustomerId,ContactName=model.ContactName,Email=model.Email,PhoneNumber=model.PhoneNumber,Source=model.Source,Status=Enum.Parse<LeadStatus>(model.Status),EstimatedValue=model.Amount},ct); break;
            case "opportunities": await crm.SaveOpportunityAsync(new Opportunity{Id=model.Id,Title=model.Title!.Trim(),CustomerId=model.CustomerId!.Value,LeadId=model.LeadId,Description=model.Description,Amount=model.Amount,Stage=Enum.Parse<OpportunityStage>(model.Stage),ExpectedCloseDate=model.ExpectedCloseDate},ct); break;
            case "tasks": await crm.SaveTaskAsync(new CrmTask{Id=model.Id,Title=model.Title!.Trim(),Description=model.Description,DueDate=model.DueDate,Priority=Enum.Parse<TaskPriority>(model.Priority),Status=Enum.Parse<MiniCRM.Domain.TaskStatus>(model.Status),CustomerId=model.CustomerId,LeadId=model.LeadId,OpportunityId=model.OpportunityId},ct); break;
            case "contacts": await crm.SaveContactAsync(new Contact{Id=model.Id,CustomerId=model.CustomerId!.Value,FullName=model.FullName!.Trim(),Email=model.Email,PhoneNumber=model.PhoneNumber,JobTitle=model.Title,IsPrimary=model.IsPrimary},ct); break;
            case "notes": await crm.SaveNoteAsync(new Note{Content=model.Content!.Trim(),CustomerId=model.CustomerId,LeadId=model.LeadId,OpportunityId=model.OpportunityId},ct); break;
            case "activities": await crm.SaveActivityAsync(new Activity{ActivityType=Enum.Parse<ActivityType>(model.ActivityType),Description=model.Description!.Trim(),CustomerId=model.CustomerId,LeadId=model.LeadId,OpportunityId=model.OpportunityId},ct); break;
        }
        TempData["Success"]=$"{Name(model.Kind)} saved."; return RedirectToAction(nameof(Index),new{kind=model.Kind});
    }

    [HttpPost,ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(string kind,int id,CancellationToken ct)
    {
        bool removed=kind switch {"leads"=>await crm.DeleteLeadAsync(id,ct),"opportunities"=>await crm.DeleteOpportunityAsync(id,ct),"tasks"=>await crm.DeleteTaskAsync(id,ct),"contacts"=>await crm.DeleteContactAsync(id,ct),_=>false};
        TempData[removed?"Success":"Error"]=removed?"Record deleted.":"Record was not found or cannot be deleted."; return RedirectToAction(nameof(Index),new{kind});
    }

    [HttpPost,ValidateAntiForgeryToken]
    public async Task<IActionResult> ConvertLead(int id,CancellationToken ct)
    { var converted=await crm.ConvertLeadAsync(id,ct); TempData[converted?"Success":"Error"]=converted?"Lead converted to a customer.":"Lead was not found or cannot be converted."; return RedirectToAction(nameof(Index),new{kind="leads"}); }

    private async Task<RecordFormViewModel> LoadOptions(RecordFormViewModel m,CancellationToken ct)
    { ViewBag.Customers=(await crm.CustomersAsync(null,1,100,ct)).Items; ViewBag.Leads=(await crm.LeadsAsync(null,1,100,ct)).Items; ViewBag.Opportunities=(await crm.OpportunitiesAsync(1,100,ct)).Items; ViewBag.Title=Name(m.Kind); return m; }
    private static string Linked(int? c,int? l,int? o)=>c is int ci?$"Customer #{ci}":l is int li?$"Lead #{li}":o is int oi?$"Opportunity #{oi}":"—";
    private static TOut? Map<TIn,TOut>(TIn? source,Func<TIn,TOut> map) where TIn:class where TOut:class => source is null?null:map(source);
}
