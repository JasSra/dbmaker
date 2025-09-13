using DbMaker.Shared.Data;
using DbMaker.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace DbMaker.Shared.Services.Templates;

public class EfTemplateRepository : ITemplateRepository
{
    private readonly DbMakerDbContext _db;

    public EfTemplateRepository(DbMakerDbContext db)
    {
        _db = db;
    }

    public async Task<List<Template>> GetAllAsync(string? category = null, string? query = null, CancellationToken ct = default)
    {
        var q = _db.Templates.AsNoTracking().Include(t => t.Versions).AsQueryable();
        if (!string.IsNullOrWhiteSpace(category))
        {
            q = q.Where(t => t.Category == category);
        }
        if (!string.IsNullOrWhiteSpace(query))
        {
            var like = $"%{query.Trim()}%";
            q = q.Where(t => EF.Functions.Like(t.DisplayName, like) || EF.Functions.Like(t.Description, like) || EF.Functions.Like(t.Key, like));
        }
        return await q.OrderBy(t => t.DisplayName).ToListAsync(ct);
    }

    public Task<Template?> GetByKeyAsync(string key, CancellationToken ct = default)
        => _db.Templates.AsNoTracking().Include(t => t.Versions).FirstOrDefaultAsync(t => t.Key == key, ct);

    public async Task<TemplateVersion?> GetVersionAsync(string key, string version, CancellationToken ct = default)
    {
        var template = await _db.Templates.AsNoTracking().FirstOrDefaultAsync(t => t.Key == key, ct);
        if (template == null) return null;
        return await _db.TemplateVersions.AsNoTracking().FirstOrDefaultAsync(v => v.TemplateId == template.Id && v.Version == version, ct);
    }
}
