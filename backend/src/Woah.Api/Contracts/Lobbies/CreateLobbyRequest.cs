using System.ComponentModel.DataAnnotations;

namespace Woah.Api.Contracts.Lobbies;

public class CreateLobbyRequest
{
    [Required]
    [MinLength(2)]
    [MaxLength(32)]
    public string HostNick { get; set; } = default!;

    [Range(2, 20)]
    public int MaxPlayers { get; set; } = 10;
}