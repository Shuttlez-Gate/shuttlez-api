using FluentValidation;
using Shuttlez.Application.Auth.DTOs;

namespace Shuttlez.Application.Auth.Validators;

public class SendOtpRequestValidator : AbstractValidator<SendOtpRequest>
{
    public SendOtpRequestValidator()
    {
        RuleFor(x => x.Phone)
            .NotEmpty()
            .Matches(@"^\+?20\d{10}$")
            .WithMessage("رقم الهاتف غير صالح. استخدم صيغة مصرية مثل +2010xxxxxxxx");
        RuleFor(x => x.Purpose)
            .Must(p => p is "login" or "register")
            .WithMessage("الغرض يجب أن يكون login أو register");
    }
}

public class VerifyOtpRequestValidator : AbstractValidator<VerifyOtpRequest>
{
    public VerifyOtpRequestValidator()
    {
        RuleFor(x => x.Phone).NotEmpty();
        RuleFor(x => x.Code)
            .NotEmpty()
            .Length(4)
            .Matches(@"^\d{4}$")
            .WithMessage("رمز التحقق يجب أن يكون 4 أرقام");
    }
}

public class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    public RegisterRequestValidator()
    {
        RuleFor(x => x.Phone).NotEmpty();
        RuleFor(x => x.FullName).NotEmpty().MinimumLength(3);
        RuleFor(x => x.Code).Length(4);
        RuleFor(x => x.Gender)
            .Must(g => g is "male" or "female" or "ذكر" or "أنثى")
            .WithMessage("النوع غير صالح");
    }
}
