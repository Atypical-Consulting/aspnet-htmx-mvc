using HtmxMvc.Http;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace HtmxMvc.Web.Tests;

public class HtmxRequestExtensionsTests
{
    private static HttpRequest RequestWith(params (string Name, string Value)[] headers)
    {
        var ctx = new DefaultHttpContext();
        foreach (var (name, value) in headers)
        {
            ctx.Request.Headers[name] = value;
        }
        return ctx.Request;
    }

    [Fact]
    public void IsHtmx_true_when_header_is_true()
    {
        Assert.True(RequestWith(("HX-Request", "true")).IsHtmx());
    }

    [Fact]
    public void IsHtmx_false_when_header_missing()
    {
        Assert.False(RequestWith().IsHtmx());
    }

    [Fact]
    public void IsHtmx_false_when_header_is_false()
    {
        Assert.False(RequestWith(("HX-Request", "false")).IsHtmx());
    }

    [Fact]
    public void IsBoosted_true_when_header_is_true()
    {
        Assert.True(RequestWith(("HX-Boosted", "true")).IsBoosted());
    }

    [Fact]
    public void IsBoosted_false_when_header_missing()
    {
        Assert.False(RequestWith().IsBoosted());
    }

    [Fact]
    public void IsHistoryRestoreRequest_true_when_header_is_true()
    {
        Assert.True(RequestWith(("HX-History-Restore-Request", "true")).IsHistoryRestoreRequest());
    }

    [Fact]
    public void HxTarget_returns_header_value()
    {
        Assert.Equal("#contact-rows", RequestWith(("HX-Target", "#contact-rows")).HxTarget());
    }

    [Fact]
    public void HxTarget_null_when_header_missing()
    {
        Assert.Null(RequestWith().HxTarget());
    }

    [Fact]
    public void HxTriggerName_returns_header_value()
    {
        Assert.Equal("q", RequestWith(("HX-Trigger-Name", "q")).HxTriggerName());
    }

    [Fact]
    public void HxTrigger_returns_header_value()
    {
        Assert.Equal("contact-42", RequestWith(("HX-Trigger", "contact-42")).HxTrigger());
    }

    [Fact]
    public void HxCurrentUrl_returns_header_value()
    {
        Assert.Equal("https://example.com/", RequestWith(("HX-Current-URL", "https://example.com/")).HxCurrentUrl());
    }

    [Fact]
    public void HxPrompt_returns_header_value()
    {
        Assert.Equal("yes please", RequestWith(("HX-Prompt", "yes please")).HxPrompt());
    }
}
