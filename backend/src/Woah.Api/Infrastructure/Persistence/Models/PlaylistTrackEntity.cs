using System;
using System.Text.Json;

namespace Woah.Api.Infrastructure.Persistence.Models;

public class PlaylistTrackEntity
{
    public Guid PlaylistId { get; set; }
    public PlaylistEntity? Playlist { get; set; }
    public int ItemNumber { get; set; } 
    //tutaj przekopiowac Jasona z Tokyo Ghoul aka Spotify API aka https://static.wikia.nocookie.net/tokyoghoul/images/9/97/Yamori_profile.png/revision/latest?cb=20170109195008

}