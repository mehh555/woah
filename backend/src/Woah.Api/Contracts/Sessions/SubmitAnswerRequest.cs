using System;
using System.ComponentModel.DataAnnotations;

namespace Woah.Api.Contracts.Sessions;

public class SubmitAnswerRequest : IValidatableObject
{
    [Required]
    public Guid PlayerId { get; set; }

    [Required]
    [MinLength(1)]
    [MaxLength(200)]
    public string Answer { get; set; } = default!;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (PlayerId == Guid.Empty)
            yield return new ValidationResult("PlayerId must not be empty.", new[] { nameof(PlayerId) });
    }
}