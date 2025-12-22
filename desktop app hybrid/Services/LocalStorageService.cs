using Microsoft.JSInterop;
using System;
using System.Collections.Generic;
using System.Text;

namespace desktop_app_hybrid.Services
{
    public class LocalStorageService
    {
        private readonly IJSRuntime _js;

        public LocalStorageService(IJSRuntime js)
        {
            _js = js;
        }

        public ValueTask SetAsync(string key, string value) =>
            _js.InvokeVoidAsync("storage.set", key, value);

        public ValueTask<string?> GetAsync(string key) =>
            _js.InvokeAsync<string?>("storage.get", key);

        public ValueTask RemoveAsync(string key) =>
            _js.InvokeVoidAsync("storage.remove", key);
    }
}
