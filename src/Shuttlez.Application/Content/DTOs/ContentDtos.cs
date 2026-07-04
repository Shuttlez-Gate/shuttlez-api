namespace Shuttlez.Application.Content.DTOs;

public record FaqItemDto(Guid Id, string Question, string Answer, int Order);

public record LegalDocumentDto(string Slug, string Title, string Content);
