using SDL3;

namespace TF.EX.Domain.Services
{
    public static class ClipboardService
    {
        public static string GetText() => SDL.SDL_HasClipboardText() ? SDL.SDL_GetClipboardText() ?? string.Empty : string.Empty;

        public static void SetText(string text)
        {
            if (!SDL.SDL_SetClipboardText(text ?? string.Empty))
            {
                throw new InvalidOperationException(SDL.SDL_GetError());
            }
        }
    }
}
