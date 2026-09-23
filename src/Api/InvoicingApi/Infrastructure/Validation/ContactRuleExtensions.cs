using System.Text.RegularExpressions;
using FluentValidation;

namespace InvoicingApi.Infrastructure.Validation;

// FluentValidation's EmailAddress() only checks for an '@', so "a@b" passes. These rules require a
// dotted domain with a letter TLD, and a phone number made of digits and common separators.
// Keep in sync with validateField in src/web/src/components/ClientForm.tsx.
public static partial class ContactRuleExtensions
{
    public static IRuleBuilderOptions<T, string?> ValidEmail<T>(this IRuleBuilder<T, string?> ruleBuilder) =>
        ruleBuilder
            .Must(email => string.IsNullOrEmpty(email) || EmailPattern().IsMatch(email))
            .WithMessage("'{PropertyName}' must be a valid email address, like name@company.com.");

    public static IRuleBuilderOptions<T, string?> ValidPhone<T>(this IRuleBuilder<T, string?> ruleBuilder) =>
        ruleBuilder
            .Must(phone => string.IsNullOrEmpty(phone) || IsValidPhone(phone))
            .WithMessage("'{PropertyName}' must be a phone number with 7 to 15 digits, like +61 2 5550 1234.");

    private static bool IsValidPhone(string phone)
    {
        if (!PhonePattern().IsMatch(phone))
        {
            return false;
        }

        var digits = phone.Count(char.IsAsciiDigit);
        return digits is >= 7 and <= 15;
    }

    [GeneratedRegex(@"^[A-Za-z0-9!#$%&'*+/=?^_`{|}~-]+(\.[A-Za-z0-9!#$%&'*+/=?^_`{|}~-]+)*@([A-Za-z0-9]([A-Za-z0-9-]*[A-Za-z0-9])?\.)+[A-Za-z]{2,}$")]
    private static partial Regex EmailPattern();

    [GeneratedRegex(@"^\+?[0-9 ().-]+$")]
    private static partial Regex PhonePattern();
}
