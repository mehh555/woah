using Microsoft.AspNetCore.Mvc;
using Woah.Api.Itunes;

namespace Woah.Api.Controllers;

[ApiController]
[Route("api/itunes")]
public class ItunesController : ControllerBase
{
    private readonly ItunesApiClient _itunesApiClient;

    public ItunesController(ItunesApiClient itunesApiClient)
    {
        _itunesApiClient = itunesApiClient;
    }

    [HttpGet("search")]
    public async Task<ActionResult<List<ItunesSongResult>>> Search(
        [FromQuery] string term,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(term))
        {
            return BadRequest("Query parameter 'term' is required.");
        }

        var results = await _itunesApiClient.SearchSongsAsync(term, cancellationToken);
        return Ok(results);
    }
}