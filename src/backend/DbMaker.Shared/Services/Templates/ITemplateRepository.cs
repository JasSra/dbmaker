using DbMaker.Shared.Models;
using System.Threading.Tasks;

namespace DbMaker.Shared.Services.Templates;

public interface ITemplateRepository
{
    Task<List<Template>> GetAllAsync(string? category = null, string? query = null, CancellationToken ct = default);
    Task<Template?> GetByKeyAsync(string key, CancellationToken ct = default);
    Task<TemplateVersion?> GetVersionAsync(string key, string version, CancellationToken ct = default);
}
