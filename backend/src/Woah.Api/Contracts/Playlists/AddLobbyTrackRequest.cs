using System.ComponentModel.DataAnnotations;

namespace Woah.Api.Contracts.Playlists;

public class AddLobbyTrackRequest : IValidatableObject
{
    [Required]
    public Guid PlayerId { get; set; }

    [Required]
    [Range(1, long.MaxValue)]
    public long TrackId { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (PlayerId == Guid.Empty)
            yield return new ValidationResult("PlayerId must not be empty.", new[] { nameof(PlayerId) });
    }
}