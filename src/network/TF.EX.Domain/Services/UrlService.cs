using SDL3;

namespace TF.EX.Domain.Services
{
    public static class UrlService
    {
        public static void Open(string url)
        {
            if (!SDL.SDL_OpenURL(url))
            {
                throw new InvalidOperationException(SDL.SDL_GetError());
            }
        }
    }
}
