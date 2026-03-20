using System.ComponentModel.DataAnnotations;

namespace Woah.Api.Contracts.Sessions;

public class StartSessionRequest
{
    [Required]
    public Guid HostPlayerId { get; set; }

    [Required]
    public Guid PlaylistId { get; set; }

    [Range(5, 20)]
    public int RoundDurationSeconds { get; set; } = 30;
}