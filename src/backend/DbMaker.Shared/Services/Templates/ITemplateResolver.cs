using DbMaker.Shared.Models;

namespace DbMaker.Shared.Services.Templates;

public interface ITemplateResolver
{
    Task<DatabaseTemplate?> ResolveAsync(string templateKey, string? version = null, CancellationToken ct = default);
}
