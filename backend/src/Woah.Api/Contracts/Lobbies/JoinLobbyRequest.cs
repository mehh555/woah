using System.ComponentModel.DataAnnotations;

namespace Woah.Api.Contracts.Lobbies;

public class JoinLobbyRequest
{
    [Required]
    [MinLength(1)]
    [MaxLength(30)]
    public string Nick { get; set; } = default!;
}