using MediatR;
using Microsoft.EntityFrameworkCore;
using Shuttlez.Application.Admin.DTOs;
using Shuttlez.Application.Admin.Handlers;
using Shuttlez.Application.Common;
using Shuttlez.Application.Common.Interfaces;
using Shuttlez.Domain.Enums;

namespace Shuttlez.Application.Drivers.Handlers;

public record UploadMyDriverDocumentCommand(
    DriverDocumentType DocumentType,
    Stream Content,
    string FileName,
    string ContentType,
    string? Notes = null) : IRequest<AdminDriverDocumentDto>;

public class UploadMyDriverDocumentHandler
    : IRequestHandler<UploadMyDriverDocumentCommand, AdminDriverDocumentDto>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IMediator _mediator;

    public UploadMyDriverDocumentHandler(
        IAppDbContext db,
        ICurrentUserService currentUser,
        IMediator mediator)
    {
        _db = db;
        _currentUser = currentUser;
        _mediator = mediator;
    }

    public async Task<AdminDriverDocumentDto> Handle(
        UploadMyDriverDocumentCommand request,
        CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId
            ?? throw new UnauthorizedAppException("غير مصرح");

        var driver = await _db.Drivers
            .FirstOrDefaultAsync(d => d.UserId == userId && !d.IsDeleted, cancellationToken)
            ?? throw new NotFoundException("الكابتن غير موجود");

        return await _mediator.Send(
            new UploadDriverDocumentCommand(
                driver.Id,
                request.DocumentType,
                request.Content,
                request.FileName,
                request.ContentType,
                "driver",
                request.Notes),
            cancellationToken);
    }
}
