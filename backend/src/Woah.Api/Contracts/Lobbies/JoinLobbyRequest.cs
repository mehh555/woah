using System.ComponentModel.DataAnnotations;

namespace Woah.Api.Contracts.Lobbies;

public class JoinLobbyRequest
{
    [Required]
    [MinLength(2)]
    [MaxLength(32)]
    public string Nick { get; set; } = default!;
}