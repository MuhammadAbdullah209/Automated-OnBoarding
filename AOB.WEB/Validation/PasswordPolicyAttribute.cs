using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace AOB.WEB.Validation
{
    /// <summary>
    /// Password rules for new passwords (reset and change password): 8–64 characters with an
    /// uppercase letter, a lowercase letter, a number and a special character.
    /// Validates on the server and emits data-val-passwordpolicy-* attributes for
    /// wwwroot/js/password-policy.js, which applies the same rules in the browser
    /// (rule keys and patterns there must match <see cref="Rules"/>).
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class PasswordPolicyAttribute : ValidationAttribute, IClientModelValidator
    {
        public const int MinLength = 8;
        public const int MaxLength = 64;

        /// <summary>One entry per checklist line: key (shared with the JS), label, test.</summary>
        public static readonly IReadOnlyList<(string Key, string Label, Func<string, bool> IsMet)> Rules =
        [
            ("length", $"{MinLength}–{MaxLength} characters", p => p.Length is >= MinLength and <= MaxLength),
            ("upper", "One uppercase letter (A–Z)", p => Regex.IsMatch(p, "[A-Z]")),
            ("lower", "One lowercase letter (a–z)", p => Regex.IsMatch(p, "[a-z]")),
            ("number", "One number (0–9)", p => Regex.IsMatch(p, "[0-9]")),
            ("special", "One special character (e.g. ! @ # $ % ^ & *)", p => Regex.IsMatch(p, @"[^A-Za-z0-9\s]"))
        ];

        public PasswordPolicyAttribute()
            : base($"{{0}} must be {MinLength}–{MaxLength} characters and include an uppercase letter, a lowercase letter, a number and a special character.")
        {
        }

        public override bool IsValid(object? value)
        {
            // Empty values are left to [Required].
            if (value is not string password || password.Length == 0)
            {
                return true;
            }

            return Rules.All(rule => rule.IsMet(password));
        }

        public void AddValidation(ClientModelValidationContext context)
        {
            context.Attributes.TryAdd("data-val", "true");
            context.Attributes.TryAdd("data-val-passwordpolicy", FormatErrorMessage(context.ModelMetadata.GetDisplayName()));
            context.Attributes.TryAdd("data-val-passwordpolicy-min", MinLength.ToString());
            context.Attributes.TryAdd("data-val-passwordpolicy-max", MaxLength.ToString());
        }
    }
}
