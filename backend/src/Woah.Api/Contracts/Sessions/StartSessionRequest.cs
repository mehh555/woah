using System;
using System.ComponentModel.DataAnnotations;

namespace Woah.Api.Contracts.Sessions;

public class StartSessionRequest : IValidatableObject
{
    [Required]
    public Guid HostPlayerId { get; set; }

    [Required]
    public Guid PlaylistId { get; set; }

    [Range(5, 25)]
    public int RoundDurationSeconds { get; set; } = 10;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (HostPlayerId == Guid.Empty)
            yield return new ValidationResult("HostPlayerId must not be empty.", new[] { nameof(HostPlayerId) });

        if (PlaylistId == Guid.Empty)
            yield return new ValidationResult("PlaylistId must not be empty.", new[] { nameof(PlaylistId) });
    }
}