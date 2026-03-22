using System;

namespace Blog.Api.Shared.Services;

public interface ISlugService
{
    string Generate(string title);
}

public class SlugService : ISlugService
{
    public string Generate(string text)
    {
        return text
            .ToLower()
            .Replace(" ", "-")
            .Replace("--", "-")
            .Replace("ä", "a")
            .Replace("é", "e")
            .Replace("í", "i")
            .Replace("ó", "o")
            .Replace("ú", "u");
    }

}
