using System;
using System.ComponentModel.DataAnnotations;

namespace Woah.Api.Contracts.Playlists;

public class AddLobbyTrackRequest : IValidatableObject
{
    [Required]
    public Guid HostPlayerId { get; set; }

    [Required]
    [Range(1, long.MaxValue)]
    public long TrackId { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (HostPlayerId == Guid.Empty)
            yield return new ValidationResult("HostPlayerId must not be empty.", new[] { nameof(HostPlayerId) });
    }
}