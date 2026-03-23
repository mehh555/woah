namespace Woah.Api.Services.Session;

public interface ITrackTitleCleaner
{
    string CleanTitle(string title);
    string ExtractMainArtist(string artist);
}