using Microsoft.JSInterop;
using System.Runtime.CompilerServices;

namespace DistributedWMS.Client.Common;

/// <summary>
/// Dostęp do pamięci lokalnej przeglądarki na potrzeby przechowywania
/// tokenu dostępowego między sesjami aplikacji.
/// </summary>
public class LocalStorage
{
    private readonly IJSRuntime _js;

    public LocalStorage(IJSRuntime js) => _js = js;

    public async Task<string?> GetAsync(string key)
    {
        try
        {
            return await _js.InvokeAsync<string?>("localStorage.getItem", key);
        }
        catch
        {
            return null;
        }
    }

    public async Task SetAsync(string key, string value)
        => await _js.InvokeVoidAsync("localStorage.setItem", key, value);

    public async Task RemoveAsync(string key)
        => await _js.InvokeVoidAsync("localStorage.removeItem", key);

}