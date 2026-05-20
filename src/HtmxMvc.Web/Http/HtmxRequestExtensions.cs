using static HtmxMvc.Http.HtmxHeaders;

namespace HtmxMvc.Http;

public static class HtmxRequestExtensions
{
    extension(HttpRequest request)
    {
        public bool IsHtmx()
            => request.Headers[Request] == "true";

        public bool IsBoosted()
            => request.Headers[Boosted] == "true";

        public bool IsHistoryRestoreRequest()
            => request.Headers[HistoryRestoreRequest] == "true";

        public string? HxTarget()
            => request.Headers[Target];

        public string? HxTriggerName()
            => request.Headers[TriggerName];

        public string? HxTrigger()
            => request.Headers[Trigger];

        public string? HxCurrentUrl()
            => request.Headers[CurrentUrl];

        public string? HxPrompt()
            => request.Headers[Prompt];
    }
}
