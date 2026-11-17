using Microsoft.EntityFrameworkCore;
using MiniCRM.Application;
using MiniCRM.Domain;
using CrmTaskStatus = MiniCRM.Domain.TaskStatus;

namespace MiniCRM.Infrastructure;

public sealed class SqlCrmService(CrmDbContext db, ICurrentUser current) : ICrmService
{
    private static (int Page, int Size) Normalize(int page, int size) => (Math.Max(1, page), Math.Clamp(size, 1, 100));
    private IQueryable<Customer> VisibleCustomers() => current.IsAdministratorOrManager ? db.Customers : current.UserId is int id ? db.Customers.Where(x => x.AssignedToUserId == id) : db.Customers.Where(_ => false);
    private IQueryable<Lead> VisibleLeads() => current.IsAdministratorOrManager ? db.Leads : current.UserId is int id ? db.Leads.Where(x => x.AssignedToUserId == id) : db.Leads.Where(_ => false);
    private IQueryable<Opportunity> VisibleOpportunities() => current.IsAdministratorOrManager ? db.Opportunities : current.UserId is int id ? db.Opportunities.Where(x => x.AssignedToUserId == id) : db.Opportunities.Where(_ => false);
    private IQueryable<CrmTask> VisibleTasks() => current.IsAdministratorOrManager ? db.CrmTasks : current.UserId is int id ? db.CrmTasks.Where(x => x.AssignedToUserId == id) : db.CrmTasks.Where(_ => false);
    private IQueryable<Note> VisibleNotes() => current.IsAdministratorOrManager ? db.Notes : current.UserId is int id ? db.Notes.Where(x => x.CreatedByUserId == id || (x.CustomerId != null && VisibleCustomers().Any(c => c.Id == x.CustomerId)) || (x.LeadId != null && VisibleLeads().Any(l => l.Id == x.LeadId)) || (x.OpportunityId != null && VisibleOpportunities().Any(o => o.Id == x.OpportunityId))) : db.Notes.Where(_ => false);
    private IQueryable<Activity> VisibleActivities() => current.IsAdministratorOrManager ? db.Activities : current.UserId is int id ? db.Activities.Where(x => x.CreatedByUserId == id || (x.CustomerId != null && VisibleCustomers().Any(c => c.Id == x.CustomerId)) || (x.LeadId != null && VisibleLeads().Any(l => l.Id == x.LeadId)) || (x.OpportunityId != null && VisibleOpportunities().Any(o => o.Id == x.OpportunityId))) : db.Activities.Where(_ => false);
    private int RequireUserId() => current.UserId ?? throw new UnauthorizedAccessException("A signed-in user is required.");
    private async Task ValidateLinkedAccessAsync(int? customerId, int? leadId, int? opportunityId, CancellationToken ct)
    {
        if (!current.IsAdministratorOrManager && customerId is int customer && !await VisibleCustomers().AnyAsync(x => x.Id == customer, ct)) throw new UnauthorizedAccessException("You cannot access the selected customer.");
        if (!current.IsAdministratorOrManager && leadId is int lead && !await VisibleLeads().AnyAsync(x => x.Id == lead, ct)) throw new UnauthorizedAccessException("You cannot access the selected lead.");
        if (!current.IsAdministratorOrManager && opportunityId is int opportunity && !await VisibleOpportunities().AnyAsync(x => x.Id == opportunity, ct)) throw new UnauthorizedAccessException("You cannot access the selected opportunity.");
    }

    public async Task<Page<Customer>> CustomersAsync(string? search, int page, int pageSize, CancellationToken ct)
    {
        (page, pageSize) = Normalize(page, pageSize);
        var query = VisibleCustomers().AsNoTracking();
        if (!string.IsNullOrWhiteSpace(search)) query = query.Where(x => x.FirstName.Contains(search) || x.LastName.Contains(search) || (x.CompanyName != null && x.CompanyName.Contains(search)) || x.Email.Contains(search));
        var total = await query.CountAsync(ct);
        var items = await query.OrderBy(x => x.LastName).ThenBy(x => x.FirstName).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        return new(items, page, pageSize, total);
    }
    public Task<Customer?> CustomerAsync(int id, CancellationToken ct) => VisibleCustomers().AsNoTracking().Include(x => x.Contacts).SingleOrDefaultAsync(x => x.Id == id, ct);
    public async Task<Customer> SaveCustomerAsync(Customer model, CancellationToken ct)
    {
        model.FirstName = model.FirstName.Trim(); model.LastName = model.LastName.Trim(); model.Email = model.Email.Trim();
        if (!current.IsAdministratorOrManager)
        {
            var userId = RequireUserId();
            if (model.Id == 0) { model.AssignedToUserId = userId; model.CreatedAt = DateTime.UtcNow; db.Customers.Add(model); }
            else { var existing = await VisibleCustomers().SingleOrDefaultAsync(x => x.Id == model.Id, ct) ?? throw new UnauthorizedAccessException("You cannot modify this customer."); model.AssignedToUserId = userId; model.CreatedAt = existing.CreatedAt; model.UpdatedAt = DateTime.UtcNow; db.Entry(existing).CurrentValues.SetValues(model); model = existing; }
        }
        else if (model.Id == 0) { model.CreatedAt = DateTime.UtcNow; db.Customers.Add(model); }
        else { var existing = await db.Customers.SingleOrDefaultAsync(x => x.Id == model.Id, ct) ?? throw new KeyNotFoundException("Customer was not found."); model.CreatedAt = existing.CreatedAt; model.UpdatedAt = DateTime.UtcNow; db.Entry(existing).CurrentValues.SetValues(model); model = existing; }
        await db.SaveChangesAsync(ct); return model;
    }
    public async Task<bool> DeleteCustomerAsync(int id, CancellationToken ct)
    {
        var customer = await VisibleCustomers().SingleOrDefaultAsync(x => x.Id == id, ct); if (customer is null) return false;
        if (await db.Opportunities.AnyAsync(x => x.CustomerId == id, ct)) return false;
        db.Customers.Remove(customer); await db.SaveChangesAsync(ct); return true;
    }
    public async Task<Page<Lead>> LeadsAsync(string? search, int page, int pageSize, CancellationToken ct)
    {
        (page, pageSize) = Normalize(page, pageSize); var query = VisibleLeads().AsNoTracking();
        if (!string.IsNullOrWhiteSpace(search)) query = query.Where(x => x.Title.Contains(search) || (x.ContactName != null && x.ContactName.Contains(search)) || (x.Email != null && x.Email.Contains(search)));
        var total = await query.CountAsync(ct); var items = await query.OrderByDescending(x => x.CreatedAt).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct); return new(items, page, pageSize, total);
    }
    public Task<Lead?> LeadAsync(int id, CancellationToken ct) => VisibleLeads().AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, ct);
    public async Task<Lead> SaveLeadAsync(Lead model, CancellationToken ct)
    {
        if (model.EstimatedValue < 0) throw new ArgumentOutOfRangeException(nameof(model.EstimatedValue));
        await ValidateLinkedAccessAsync(model.CustomerId, null, null, ct);
        if (!current.IsAdministratorOrManager)
        {
            var userId = RequireUserId();
            if (model.Id == 0) { if(model.Status!=LeadStatus.New)throw new InvalidOperationException("New leads must start in the New status."); model.AssignedToUserId = userId; model.CreatedAt = DateTime.UtcNow; db.Leads.Add(model); }
            else { var existing = await VisibleLeads().SingleOrDefaultAsync(x => x.Id == model.Id, ct) ?? throw new UnauthorizedAccessException("You cannot modify this lead."); if(!CrmBusinessRules.CanMoveLead(existing.Status,model.Status))throw new InvalidOperationException("This lead status transition is not allowed; convert leads through the conversion workflow."); model.AssignedToUserId = userId; model.CreatedAt = existing.CreatedAt; model.UpdatedAt = DateTime.UtcNow; db.Entry(existing).CurrentValues.SetValues(model); model = existing; }
        }
        else if (model.Id == 0) { if(model.Status!=LeadStatus.New)throw new InvalidOperationException("New leads must start in the New status."); model.CreatedAt = DateTime.UtcNow; db.Leads.Add(model); }
        else { var existing = await db.Leads.SingleOrDefaultAsync(x => x.Id == model.Id, ct) ?? throw new KeyNotFoundException("Lead was not found."); if(!CrmBusinessRules.CanMoveLead(existing.Status,model.Status))throw new InvalidOperationException("This lead status transition is not allowed; convert leads through the conversion workflow."); model.CreatedAt = existing.CreatedAt; model.UpdatedAt = DateTime.UtcNow; db.Entry(existing).CurrentValues.SetValues(model); model = existing; }
        await db.SaveChangesAsync(ct); return model;
    }
    public async Task<bool> DeleteLeadAsync(int id, CancellationToken ct)
    {
        var lead = await VisibleLeads().SingleOrDefaultAsync(x => x.Id == id, ct); if (lead is null) return false;
        db.Leads.Remove(lead); await db.SaveChangesAsync(ct); return true;
    }
    public async Task<bool> ConvertLeadAsync(int id, CancellationToken ct)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        var lead = await VisibleLeads().SingleOrDefaultAsync(x => x.Id == id, ct);
        if (lead is null || !CrmBusinessRules.CanConvertLead(lead.Status)) return false;
        Customer? customer = null;
        if (!string.IsNullOrWhiteSpace(lead.Email))
        {
            customer = await VisibleCustomers().SingleOrDefaultAsync(x => x.Email == lead.Email, ct);
            if (customer is null && await db.Customers.AnyAsync(x => x.Email == lead.Email, ct))
                throw new InvalidOperationException("A customer with this email already exists and is outside your assigned records.");
        }
        if (customer is null)
        {
            var name = (lead.ContactName ?? lead.Title).Split(' ', StringSplitOptions.RemoveEmptyEntries);
            customer = new Customer { FirstName = name.FirstOrDefault() ?? lead.Title, LastName = string.Join(' ', name.Skip(1)), Email = lead.Email ?? $"lead-{lead.Id}-{Guid.NewGuid():N}@invalid.local", PhoneNumber = lead.PhoneNumber, Source = lead.Source, Status = CustomerStatus.Prospect, AssignedToUserId = lead.AssignedToUserId, CreatedAt = DateTime.UtcNow };
            db.Customers.Add(customer);
        }
        lead.CustomerId = customer.Id; lead.Status = LeadStatus.Converted; lead.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct); await transaction.CommitAsync(ct); return true;
    }
    public async Task<Page<Opportunity>> OpportunitiesAsync(int page, int pageSize, CancellationToken ct)
    {
        (page, pageSize) = Normalize(page, pageSize); var query = VisibleOpportunities().AsNoTracking(); var total = await query.CountAsync(ct);
        var items = await query.OrderBy(x => x.ExpectedCloseDate).ThenByDescending(x => x.CreatedAt).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct); return new(items, page, pageSize, total);
    }
    public Task<Opportunity?> OpportunityAsync(int id, CancellationToken ct) => VisibleOpportunities().AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, ct);
    public async Task<Opportunity> SaveOpportunityAsync(Opportunity model, CancellationToken ct)
    {
        if (model.Amount < 0) throw new ArgumentOutOfRangeException(nameof(model.Amount));
        await ValidateLinkedAccessAsync(model.CustomerId, model.LeadId, null, ct);
        if (!current.IsAdministratorOrManager) model.AssignedToUserId = RequireUserId();
        if (model.Id == 0) { if (model.Stage != OpportunityStage.Qualification) throw new InvalidOperationException("New opportunities must start at Qualification."); model.CreatedAt = DateTime.UtcNow; db.Opportunities.Add(model); }
        else { var existing = await VisibleOpportunities().SingleOrDefaultAsync(x => x.Id == model.Id, ct) ?? throw new UnauthorizedAccessException("You cannot modify this opportunity.");
            if (!current.IsAdministratorOrManager && existing.AssignedToUserId != current.UserId) throw new UnauthorizedAccessException("You cannot modify this opportunity.");
            if (!CrmBusinessRules.CanMoveOpportunity(existing.Stage, model.Stage)) throw new InvalidOperationException("This opportunity stage transition is not allowed.");
            model.CreatedAt = existing.CreatedAt; model.UpdatedAt = DateTime.UtcNow; db.Entry(existing).CurrentValues.SetValues(model); model = existing; }
        await db.SaveChangesAsync(ct); return model;
    }
    public async Task<bool> DeleteOpportunityAsync(int id, CancellationToken ct)
    {
        var item=await VisibleOpportunities().SingleOrDefaultAsync(x=>x.Id==id,ct);if(item is null)return false;
        if(item.Stage is OpportunityStage.Won or OpportunityStage.Lost)throw new InvalidOperationException("Closed opportunities cannot be deleted.");
        db.Opportunities.Remove(item);await db.SaveChangesAsync(ct);return true;
    }
    public async Task<Page<CrmTask>> TasksAsync(int page, int pageSize, CancellationToken ct)
    {
        (page, pageSize) = Normalize(page, pageSize); var query = VisibleTasks().AsNoTracking(); var total = await query.CountAsync(ct);
        var items = await query.OrderBy(x => x.DueDate == null).ThenBy(x => x.DueDate).ThenByDescending(x => x.Priority).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct); return new(items, page, pageSize, total);
    }
    public Task<CrmTask?> TaskAsync(int id,CancellationToken ct)=>VisibleTasks().AsNoTracking().SingleOrDefaultAsync(x=>x.Id==id,ct);
    public async Task<CrmTask> SaveTaskAsync(CrmTask model, CancellationToken ct)
    {
        await ValidateLinkedAccessAsync(model.CustomerId, model.LeadId, model.OpportunityId, ct);
        CrmBusinessRules.ApplyTaskCompletion(model, DateTime.UtcNow);
        if (!current.IsAdministratorOrManager) model.AssignedToUserId = RequireUserId();
        if (model.Id == 0) { model.CreatedAt = DateTime.UtcNow; db.CrmTasks.Add(model); }
        else { var existing = await VisibleTasks().SingleOrDefaultAsync(x => x.Id == model.Id, ct) ?? throw new UnauthorizedAccessException("You cannot modify this task."); model.CreatedAt = existing.CreatedAt; db.Entry(existing).CurrentValues.SetValues(model); model = existing; }
        await db.SaveChangesAsync(ct); return model;
    }
    public async Task<bool> DeleteTaskAsync(int id,CancellationToken ct)
    { var item=await VisibleTasks().SingleOrDefaultAsync(x=>x.Id==id,ct);if(item is null)return false;db.CrmTasks.Remove(item);await db.SaveChangesAsync(ct);return true; }
    public async Task<Page<Contact>> ContactsAsync(int page, int pageSize, CancellationToken ct, int? customerId = null)
    {
        (page, pageSize) = Normalize(page, pageSize); var query = current.IsAdministratorOrManager ? db.Contacts.AsNoTracking() : db.Contacts.AsNoTracking().Where(x => VisibleCustomers().Any(c => c.Id == x.CustomerId)); if(customerId is int cId)query=query.Where(x=>x.CustomerId==cId); var total = await query.CountAsync(ct);
        var items = await query.OrderBy(x => x.FullName).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct); return new(items, page, pageSize, total);
    }
    public Task<Contact?> ContactAsync(int id,CancellationToken ct)=>db.Contacts.AsNoTracking().Where(x=>current.IsAdministratorOrManager||VisibleCustomers().Any(c=>c.Id==x.CustomerId)).SingleOrDefaultAsync(x=>x.Id==id,ct);
    public async Task<Contact> SaveContactAsync(Contact model, CancellationToken ct)
    {
        if (!current.IsAdministratorOrManager && !await VisibleCustomers().AnyAsync(x => x.Id == model.CustomerId, ct)) throw new UnauthorizedAccessException("You cannot add a contact to this customer.");
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        if (model.IsPrimary) { var others = await db.Contacts.Where(x => x.CustomerId == model.CustomerId && x.Id != model.Id && x.IsPrimary).ToListAsync(ct); foreach (var contact in others) contact.IsPrimary = false; }
        if (model.Id == 0) db.Contacts.Add(model);
        else { var existing = await db.Contacts.SingleOrDefaultAsync(x => x.Id == model.Id && VisibleCustomers().Any(c => c.Id == x.CustomerId), ct) ?? throw new UnauthorizedAccessException("You cannot modify this contact."); db.Entry(existing).CurrentValues.SetValues(model); model = existing; }
        await db.SaveChangesAsync(ct); await transaction.CommitAsync(ct); return model;
    }
    public async Task<bool> DeleteContactAsync(int id,CancellationToken ct)
    { var item=await db.Contacts.SingleOrDefaultAsync(x=>x.Id==id&&VisibleCustomers().Any(c=>c.Id==x.CustomerId),ct);if(item is null)return false;db.Contacts.Remove(item);await db.SaveChangesAsync(ct);return true; }
    public async Task<Page<Note>> NotesAsync(int page, int pageSize, CancellationToken ct, int? customerId = null, int? leadId = null, int? opportunityId = null)
    {
        (page, pageSize) = Normalize(page, pageSize); var query = VisibleNotes().AsNoTracking(); if(customerId is int cId)query=query.Where(x=>x.CustomerId==cId);if(leadId is int lId)query=query.Where(x=>x.LeadId==lId);if(opportunityId is int oId)query=query.Where(x=>x.OpportunityId==oId);var total = await query.CountAsync(ct);
        var items = await query.OrderByDescending(x => x.CreatedAt).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct); return new(items, page, pageSize, total);
    }
    public async Task<Note> SaveNoteAsync(Note model, CancellationToken ct)
    { await ValidateLinkedAccessAsync(model.CustomerId, model.LeadId, model.OpportunityId, ct); model.CreatedByUserId = RequireUserId(); model.CreatedAt = DateTime.UtcNow; db.Notes.Add(model); await db.SaveChangesAsync(ct); return model; }
    public Task<Note?> NoteAsync(int id,CancellationToken ct)=>VisibleNotes().AsNoTracking().SingleOrDefaultAsync(x=>x.Id==id,ct);
    public async Task<Page<Activity>> ActivitiesAsync(int page, int pageSize, CancellationToken ct, int? customerId = null, int? leadId = null, int? opportunityId = null)
    {
        (page, pageSize) = Normalize(page, pageSize); var query = VisibleActivities().AsNoTracking(); if(customerId is int cId)query=query.Where(x=>x.CustomerId==cId);if(leadId is int lId)query=query.Where(x=>x.LeadId==lId);if(opportunityId is int oId)query=query.Where(x=>x.OpportunityId==oId);var total = await query.CountAsync(ct);
        var items = await query.OrderByDescending(x => x.CreatedAt).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct); return new(items, page, pageSize, total);
    }
    public async Task<Activity> SaveActivityAsync(Activity model, CancellationToken ct)
    { await ValidateLinkedAccessAsync(model.CustomerId, model.LeadId, model.OpportunityId, ct); model.CreatedByUserId = RequireUserId(); model.CreatedAt = DateTime.UtcNow; db.Activities.Add(model); await db.SaveChangesAsync(ct); return model; }
    public Task<Activity?> ActivityAsync(int id,CancellationToken ct)=>VisibleActivities().AsNoTracking().SingleOrDefaultAsync(x=>x.Id==id,ct);
    public async Task<DashboardSummary> DashboardAsync(CancellationToken ct)
    {
        var customers = await VisibleCustomers().CountAsync(ct); var totalLeads = await VisibleLeads().CountAsync(ct);
        var converted = await VisibleLeads().CountAsync(x => x.Status == LeadStatus.Converted, ct);
        var open = await VisibleOpportunities().CountAsync(x => x.Stage != OpportunityStage.Won && x.Stage != OpportunityStage.Lost, ct);
        var won = await VisibleOpportunities().CountAsync(x => x.Stage == OpportunityStage.Won, ct); var lost = await VisibleOpportunities().CountAsync(x => x.Stage == OpportunityStage.Lost, ct);
        var pipeline = await VisibleOpportunities().Where(x => x.Stage != OpportunityStage.Won && x.Stage != OpportunityStage.Lost).SumAsync(x => (decimal?)x.Amount, ct) ?? 0m;
        var revenue = await VisibleOpportunities().Where(x => x.Stage == OpportunityStage.Won).SumAsync(x => (decimal?)x.Amount, ct) ?? 0m;
        var pending = await VisibleTasks().CountAsync(x => x.Status != CrmTaskStatus.Completed && x.Status != CrmTaskStatus.Cancelled, ct);
        var overdue = await VisibleTasks().CountAsync(x => x.Status != CrmTaskStatus.Completed && x.Status != CrmTaskStatus.Cancelled && x.DueDate < DateTime.UtcNow, ct);
        return new(customers, totalLeads, converted, open, won, lost, pipeline, revenue, pending, overdue);
    }
}
