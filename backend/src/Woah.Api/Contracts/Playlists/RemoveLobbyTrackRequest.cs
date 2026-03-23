using System.ComponentModel.DataAnnotations;

namespace Woah.Api.Contracts.Playlists;

public class RemoveLobbyTrackRequest : IValidatableObject
{
    [Required]
    public Guid PlayerId { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (PlayerId == Guid.Empty)
            yield return new ValidationResult("PlayerId must not be empty.", new[] { nameof(PlayerId) });
    }
}