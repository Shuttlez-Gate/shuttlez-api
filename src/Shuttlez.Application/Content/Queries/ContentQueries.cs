using MediatR;
using Microsoft.EntityFrameworkCore;
using Shuttlez.Application.Common;
using Shuttlez.Application.Common.Interfaces;
using Shuttlez.Application.Content.DTOs;

namespace Shuttlez.Application.Content.Queries;

public record GetFaqQuery : IRequest<IReadOnlyList<FaqItemDto>>;

public record GetLegalDocumentQuery(string Slug) : IRequest<LegalDocumentDto>;

public class ContentQueryHandlers :
    IRequestHandler<GetFaqQuery, IReadOnlyList<FaqItemDto>>,
    IRequestHandler<GetLegalDocumentQuery, LegalDocumentDto>
{
    private readonly IAppDbContext _db;

    public ContentQueryHandlers(IAppDbContext db) => _db = db;

    public async Task<IReadOnlyList<FaqItemDto>> Handle(
        GetFaqQuery request,
        CancellationToken cancellationToken)
    {
        var items = await _db.FaqItems
            .Where(x => x.IsActive && !x.IsDeleted)
            .OrderBy(x => x.Order)
            .Select(x => new FaqItemDto(x.Id, x.Question, x.Answer, x.Order))
            .ToListAsync(cancellationToken);

        return items;
    }

    public async Task<LegalDocumentDto> Handle(
        GetLegalDocumentQuery request,
        CancellationToken cancellationToken)
    {
        var slug = request.Slug.Trim().ToLowerInvariant();
        var doc = await _db.LegalDocuments
            .FirstOrDefaultAsync(
                x => x.Slug == slug && x.IsActive && !x.IsDeleted,
                cancellationToken)
            ?? throw new NotFoundException("المستند غير موجود");

        return new LegalDocumentDto(doc.Slug, doc.Title, doc.Content);
    }
}
