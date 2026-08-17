using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shuttlez.Application.Common;
using Shuttlez.Application.Content.DTOs;
using Shuttlez.Application.Content.Queries;

namespace Shuttlez.API.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/v1/content")]
public class ContentController : ControllerBase
{
    private readonly IMediator _mediator;

    public ContentController(IMediator mediator) => _mediator = mediator;

    [HttpGet("faq")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<FaqItemDto>>>> GetFaq(
        CancellationToken cancellationToken)
    {
        var items = await _mediator.Send(new GetFaqQuery(), cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<FaqItemDto>>.Ok(items));
    }

    [HttpGet("legal/{slug}")]
    public async Task<ActionResult<ApiResponse<LegalDocumentDto>>> GetLegal(
        string slug,
        CancellationToken cancellationToken)
    {
        var doc = await _mediator.Send(new GetLegalDocumentQuery(slug), cancellationToken);
        return Ok(ApiResponse<LegalDocumentDto>.Ok(doc));
    }
}
