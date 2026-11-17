using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiniCRM.Application;
using MiniCRM.Domain;

namespace MiniCRM.API.Controllers;

[ApiController, Authorize, Route("api")]
public sealed class CrmController(ICrmService crm) : ControllerBase
{
    [HttpGet("dashboard")] public Task<DashboardSummary> Dashboard(CancellationToken ct) => crm.DashboardAsync(ct);

    [HttpGet("customers")]
    public async Task<Page<CustomerDto>> Customers([FromQuery]string? search,[FromQuery]int page=1,[FromQuery]int pageSize=10,CancellationToken ct=default)=>PageMap.Map(await crm.CustomersAsync(search,page,pageSize,ct),CustomerDto.From);
    [HttpGet("customers/{id:int}")] public async Task<IActionResult> Customer(int id,CancellationToken ct){var x=await crm.CustomerAsync(id,ct);return x is null?NotFound():Ok(CustomerDto.From(x));}
    [HttpPost("customers")] public async Task<IActionResult> CreateCustomer(CustomerWriteRequest request,CancellationToken ct){var saved=await crm.SaveCustomerAsync(request.ToEntity(),ct);return CreatedAtAction(nameof(Customer),new{id=saved.Id},CustomerDto.From(saved));}
    [HttpPut("customers/{id:int}")] public async Task<IActionResult> UpdateCustomer(int id,CustomerWriteRequest request,CancellationToken ct){var existing=await crm.CustomerAsync(id,ct);if(existing is null)return NotFound();var model=request.ToEntity(id);model.CreatedAt=existing.CreatedAt;model.Contacts=existing.Contacts;model.UpdatedAt=DateTime.UtcNow;return Ok(CustomerDto.From(await crm.SaveCustomerAsync(model,ct)));}
    [HttpDelete("customers/{id:int}")] public async Task<IActionResult> DeleteCustomer(int id,CancellationToken ct)
    { if(await crm.CustomerAsync(id,ct)is null)return NotFound(); return await crm.DeleteCustomerAsync(id,ct)?NoContent():Conflict(new{error="Customer cannot be deleted while it has opportunities."}); }

    [HttpGet("leads")] public async Task<Page<LeadDto>> Leads([FromQuery]string? search,[FromQuery]int page=1,[FromQuery]int pageSize=10,CancellationToken ct=default)=>PageMap.Map(await crm.LeadsAsync(search,page,pageSize,ct),LeadDto.From);
    [HttpGet("leads/{id:int}")] public async Task<IActionResult> Lead(int id,CancellationToken ct){var x=await crm.LeadAsync(id,ct);return x is null?NotFound():Ok(LeadDto.From(x));}
    [HttpPost("leads")] public async Task<IActionResult> CreateLead(LeadWriteRequest request,CancellationToken ct){var saved=await crm.SaveLeadAsync(request.ToEntity(),ct);return CreatedAtAction(nameof(Lead),new{id=saved.Id},LeadDto.From(saved));}
    [HttpPut("leads/{id:int}")] public async Task<IActionResult> UpdateLead(int id,LeadWriteRequest request,CancellationToken ct){var existing=await crm.LeadAsync(id,ct);if(existing is null)return NotFound();var model=request.ToEntity(id);model.CreatedAt=existing.CreatedAt;model.UpdatedAt=DateTime.UtcNow;return Ok(LeadDto.From(await crm.SaveLeadAsync(model,ct)));}
    [HttpDelete("leads/{id:int}")] public async Task<IActionResult> DeleteLead(int id,CancellationToken ct)=>await crm.DeleteLeadAsync(id,ct)?NoContent():NotFound();
    [HttpPost("leads/{id:int}/convert")] public async Task<IActionResult> ConvertLead(int id,CancellationToken ct)=>await crm.ConvertLeadAsync(id,ct)?NoContent():Conflict(new{error="Lead does not exist or cannot be converted."});

    [HttpGet("opportunities")] public async Task<Page<OpportunityDto>> Opportunities([FromQuery]int page=1,[FromQuery]int pageSize=10,CancellationToken ct=default)=>PageMap.Map(await crm.OpportunitiesAsync(page,pageSize,ct),OpportunityDto.From);
    [HttpGet("opportunities/{id:int}")] public async Task<IActionResult> Opportunity(int id,CancellationToken ct){var x=await crm.OpportunityAsync(id,ct);return x is null?NotFound():Ok(OpportunityDto.From(x));}
    [HttpPost("opportunities")] public async Task<IActionResult> CreateOpportunity(OpportunityWriteRequest request,CancellationToken ct){var saved=await crm.SaveOpportunityAsync(request.ToEntity(),ct);return CreatedAtAction(nameof(Opportunity),new{id=saved.Id},OpportunityDto.From(saved));}
    [HttpPut("opportunities/{id:int}")] public async Task<IActionResult> UpdateOpportunity(int id,OpportunityWriteRequest request,CancellationToken ct){if(await crm.OpportunityAsync(id,ct)is null)return NotFound();return Ok(OpportunityDto.From(await crm.SaveOpportunityAsync(request.ToEntity(id),ct)));}
    [HttpDelete("opportunities/{id:int}")] public async Task<IActionResult> DeleteOpportunity(int id,CancellationToken ct)=>await crm.DeleteOpportunityAsync(id,ct)?NoContent():NotFound();

    [HttpGet("tasks")] public async Task<Page<TaskDto>> Tasks([FromQuery]int page=1,[FromQuery]int pageSize=10,CancellationToken ct=default)=>PageMap.Map(await crm.TasksAsync(page,pageSize,ct),TaskDto.From);
    [HttpGet("tasks/{id:int}")] public async Task<IActionResult> Task(int id,CancellationToken ct){var x=await crm.TaskAsync(id,ct);return x is null?NotFound():Ok(TaskDto.From(x));}
    [HttpPost("tasks")] public async Task<IActionResult> CreateTask(TaskWriteRequest request,CancellationToken ct){var saved=await crm.SaveTaskAsync(request.ToEntity(),ct);return CreatedAtAction(nameof(Task),new{id=saved.Id},TaskDto.From(saved));}
    [HttpPut("tasks/{id:int}")] public async Task<IActionResult> UpdateTask(int id,TaskWriteRequest request,CancellationToken ct){if(await crm.TaskAsync(id,ct)is null)return NotFound();return Ok(TaskDto.From(await crm.SaveTaskAsync(request.ToEntity(id),ct)));}
    [HttpDelete("tasks/{id:int}")] public async Task<IActionResult> DeleteTask(int id,CancellationToken ct)=>await crm.DeleteTaskAsync(id,ct)?NoContent():NotFound();

    [HttpGet("contacts")] public async Task<Page<ContactDto>> Contacts([FromQuery]int page=1,[FromQuery]int pageSize=10,[FromQuery]int? customerId=null,CancellationToken ct=default)=>PageMap.Map(await crm.ContactsAsync(page,pageSize,ct,customerId),ContactDto.From);
    [HttpGet("contacts/{id:int}")] public async Task<IActionResult> Contact(int id,CancellationToken ct){var x=await crm.ContactAsync(id,ct);return x is null?NotFound():Ok(ContactDto.From(x));}
    [HttpPost("contacts")] public async Task<IActionResult> CreateContact(ContactWriteRequest request,CancellationToken ct){var saved=await crm.SaveContactAsync(request.ToEntity(),ct);return CreatedAtAction(nameof(Contact),new{id=saved.Id},ContactDto.From(saved));}
    [HttpPut("contacts/{id:int}")] public async Task<IActionResult> UpdateContact(int id,ContactWriteRequest request,CancellationToken ct){if(await crm.ContactAsync(id,ct)is null)return NotFound();return Ok(ContactDto.From(await crm.SaveContactAsync(request.ToEntity(id),ct)));}
    [HttpDelete("contacts/{id:int}")] public async Task<IActionResult> DeleteContact(int id,CancellationToken ct)=>await crm.DeleteContactAsync(id,ct)?NoContent():NotFound();

    [HttpGet("notes")] public async Task<Page<NoteDto>> Notes([FromQuery]int page=1,[FromQuery]int pageSize=10,[FromQuery]int? customerId=null,[FromQuery]int? leadId=null,[FromQuery]int? opportunityId=null,CancellationToken ct=default)=>PageMap.Map(await crm.NotesAsync(page,pageSize,ct,customerId,leadId,opportunityId),NoteDto.From);
    [HttpGet("notes/{id:int}")] public async Task<IActionResult> Note(int id,CancellationToken ct){var x=await crm.NoteAsync(id,ct);return x is null?NotFound():Ok(NoteDto.From(x));}
    [HttpPost("notes")] public async Task<IActionResult> CreateNote(NoteWriteRequest request,CancellationToken ct){var saved=await crm.SaveNoteAsync(request.ToEntity(),ct);return CreatedAtAction(nameof(Note),new{id=saved.Id},NoteDto.From(saved));}
    [HttpGet("activities")] public async Task<Page<ActivityDto>> Activities([FromQuery]int page=1,[FromQuery]int pageSize=10,[FromQuery]int? customerId=null,[FromQuery]int? leadId=null,[FromQuery]int? opportunityId=null,CancellationToken ct=default)=>PageMap.Map(await crm.ActivitiesAsync(page,pageSize,ct,customerId,leadId,opportunityId),ActivityDto.From);
    [HttpGet("activities/{id:int}")] public async Task<IActionResult> Activity(int id,CancellationToken ct){var x=await crm.ActivityAsync(id,ct);return x is null?NotFound():Ok(ActivityDto.From(x));}
    [HttpPost("activities")] public async Task<IActionResult> CreateActivity(ActivityWriteRequest request,CancellationToken ct){var saved=await crm.SaveActivityAsync(request.ToEntity(),ct);return CreatedAtAction(nameof(Activity),new{id=saved.Id},ActivityDto.From(saved));}
}
