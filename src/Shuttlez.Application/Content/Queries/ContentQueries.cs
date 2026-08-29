using MediatR;
using Microsoft.EntityFrameworkCore;
using Shuttlez.Application.Common;
using Shuttlez.Application.Common.Interfaces;
using Shuttlez.Application.Content.DTOs;

namespace Shuttlez.Application.Content.Queries;

public record GetFaqQuery : IRequest<IReadOnlyList<FaqItemDto>>;

public record GetLegalDocumentQuery(string Slug, string? Language) : IRequest<LegalDocumentDto>;

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

        var titleAr = doc.Title ?? string.Empty;
        var contentAr = doc.Content ?? string.Empty;
        var titleEn = string.IsNullOrWhiteSpace(doc.TitleEn) ? titleAr : doc.TitleEn;
        var contentEn = string.IsNullOrWhiteSpace(doc.ContentEn) ? contentAr : doc.ContentEn;
        var english = IsEnglish(request.Language);

        return new LegalDocumentDto(
            doc.Slug,
            english ? titleEn : titleAr,
            english ? contentEn : contentAr,
            titleAr,
            contentAr,
            titleEn,
            contentEn);
    }

    private static bool IsEnglish(string? language)
    {
        if (string.IsNullOrWhiteSpace(language))
        {
            return false;
        }

        var code = language.Split(',', ';')[0].Trim().ToLowerInvariant();
        return code.StartsWith("en");
    }
}
