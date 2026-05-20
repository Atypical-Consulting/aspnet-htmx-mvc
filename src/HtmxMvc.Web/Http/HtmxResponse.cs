using System.Text.Json;

namespace HtmxMvc.Http;

public sealed class HtmxResponse
{
    private readonly HttpResponse _response;
    private readonly Dictionary<string, object?> _triggers = new(StringComparer.Ordinal);
    private readonly Dictionary<string, object?> _triggersAfterSwap = new(StringComparer.Ordinal);
    private readonly Dictionary<string, object?> _triggersAfterSettle = new(StringComparer.Ordinal);

    internal HtmxResponse(HttpResponse response) => _response = response;

    public HtmxResponse Trigger(string eventName, object? detail = null)
    {
        _triggers[eventName] = detail;
        return this;
    }

    public HtmxResponse TriggerAfterSwap(string eventName, object? detail = null)
    {
        _triggersAfterSwap[eventName] = detail;
        return this;
    }

    public HtmxResponse TriggerAfterSettle(string eventName, object? detail = null)
    {
        _triggersAfterSettle[eventName] = detail;
        return this;
    }

    public HtmxResponse PushUrl(string url) => Set(HtmxHeaders.PushUrl, url);
    public HtmxResponse PreventPushUrl() => Set(HtmxHeaders.PushUrl, "false");
    public HtmxResponse ReplaceUrl(string url) => Set(HtmxHeaders.ReplaceUrl, url);
    public HtmxResponse PreventReplaceUrl() => Set(HtmxHeaders.ReplaceUrl, "false");
    public HtmxResponse Location(string url) => Set(HtmxHeaders.Location, url);
    public HtmxResponse Redirect(string url) => Set(HtmxHeaders.Redirect, url);
    public HtmxResponse Refresh() => Set(HtmxHeaders.Refresh, "true");
    public HtmxResponse Reswap(string swap) => Set(HtmxHeaders.Reswap, swap);
    public HtmxResponse Retarget(string selector) => Set(HtmxHeaders.Retarget, selector);
    public HtmxResponse Reselect(string selector) => Set(HtmxHeaders.Reselect, selector);

    private HtmxResponse Set(string name, string value)
    {
        _response.Headers[name] = value;
        return this;
    }

    internal void Apply()
    {
        WriteTriggers(HtmxHeaders.Trigger, _triggers);
        WriteTriggers(HtmxHeaders.TriggerAfterSwap, _triggersAfterSwap);
        WriteTriggers(HtmxHeaders.TriggerAfterSettle, _triggersAfterSettle);
    }

    private void WriteTriggers(string header, Dictionary<string, object?> triggers)
    {
        if (triggers.Count == 0) return;

        var value = triggers.Values.All(v => v is null)
            ? string.Join(", ", triggers.Keys)
            : JsonSerializer.Serialize(triggers);

        _response.Headers[header] = value;
    }
}

public static class HtmxResponseExtensions
{
    extension(HttpResponse response)
    {
        public HttpResponse Htmx(Action<HtmxResponse> configure)
        {
            var builder = new HtmxResponse(response);
            configure(builder);
            builder.Apply();
            return response;
        }
    }
}
