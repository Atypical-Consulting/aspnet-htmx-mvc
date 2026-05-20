using System.Text.Json;
using HtmxMvc.Http;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace HtmxMvc.Web.Tests;

public class HtmxResponseTests
{
    private static HttpResponse NewResponse() => new DefaultHttpContext().Response;

    [Fact]
    public void Sets_no_headers_when_builder_does_nothing()
    {
        var response = NewResponse();

        response.Htmx(_ => { });

        Assert.Empty(response.Headers);
    }

    [Fact]
    public void PushUrl_sets_header()
    {
        var response = NewResponse();

        response.Htmx(h => h.PushUrl("/contacts/42"));

        Assert.Equal("/contacts/42", response.Headers["HX-Push-Url"]);
    }

    [Fact]
    public void PreventPushUrl_sets_header_to_false()
    {
        var response = NewResponse();

        response.Htmx(h => h.PreventPushUrl());

        Assert.Equal("false", response.Headers["HX-Push-Url"]);
    }

    [Fact]
    public void ReplaceUrl_sets_header()
    {
        var response = NewResponse();

        response.Htmx(h => h.ReplaceUrl("/contacts"));

        Assert.Equal("/contacts", response.Headers["HX-Replace-Url"]);
    }

    [Fact]
    public void Location_sets_header()
    {
        var response = NewResponse();

        response.Htmx(h => h.Location("/dashboard"));

        Assert.Equal("/dashboard", response.Headers["HX-Location"]);
    }

    [Fact]
    public void Redirect_sets_header()
    {
        var response = NewResponse();

        response.Htmx(h => h.Redirect("/login"));

        Assert.Equal("/login", response.Headers["HX-Redirect"]);
    }

    [Fact]
    public void Refresh_sets_header_to_true()
    {
        var response = NewResponse();

        response.Htmx(h => h.Refresh());

        Assert.Equal("true", response.Headers["HX-Refresh"]);
    }

    [Fact]
    public void Reswap_sets_header()
    {
        var response = NewResponse();

        response.Htmx(h => h.Reswap("outerHTML"));

        Assert.Equal("outerHTML", response.Headers["HX-Reswap"]);
    }

    [Fact]
    public void Retarget_sets_header()
    {
        var response = NewResponse();

        response.Htmx(h => h.Retarget("#errors"));

        Assert.Equal("#errors", response.Headers["HX-Retarget"]);
    }

    [Fact]
    public void Reselect_sets_header()
    {
        var response = NewResponse();

        response.Htmx(h => h.Reselect("#fragment"));

        Assert.Equal("#fragment", response.Headers["HX-Reselect"]);
    }

    [Fact]
    public void Trigger_single_event_without_detail_uses_csv_form()
    {
        var response = NewResponse();

        response.Htmx(h => h.Trigger("contact-saved"));

        Assert.Equal("contact-saved", response.Headers["HX-Trigger"]);
    }

    [Fact]
    public void Trigger_multiple_events_without_detail_uses_csv_form()
    {
        var response = NewResponse();

        response.Htmx(h => h.Trigger("a").Trigger("b"));

        Assert.Equal("a, b", response.Headers["HX-Trigger"]);
    }

    [Fact]
    public void Trigger_with_detail_uses_json_form()
    {
        var response = NewResponse();

        response.Htmx(h => h.Trigger("contact-saved", new { id = 42 }));

        var json = response.Headers["HX-Trigger"].ToString();
        using var doc = JsonDocument.Parse(json);
        Assert.Equal(42, doc.RootElement.GetProperty("contact-saved").GetProperty("id").GetInt32());
    }

    [Fact]
    public void Trigger_mixed_payloads_uses_json_form_with_null_for_bare_events()
    {
        var response = NewResponse();

        response.Htmx(h => h
            .Trigger("bare")
            .Trigger("rich", new { value = 1 }));

        var json = response.Headers["HX-Trigger"].ToString();
        using var doc = JsonDocument.Parse(json);
        Assert.Equal(JsonValueKind.Null, doc.RootElement.GetProperty("bare").ValueKind);
        Assert.Equal(1, doc.RootElement.GetProperty("rich").GetProperty("value").GetInt32());
    }

    [Fact]
    public void Trigger_same_event_twice_uses_last_payload()
    {
        var response = NewResponse();

        response.Htmx(h => h
            .Trigger("contact-saved", new { id = 1 })
            .Trigger("contact-saved", new { id = 2 }));

        var json = response.Headers["HX-Trigger"].ToString();
        using var doc = JsonDocument.Parse(json);
        Assert.Equal(2, doc.RootElement.GetProperty("contact-saved").GetProperty("id").GetInt32());
    }

    [Fact]
    public void TriggerAfterSwap_uses_separate_header()
    {
        var response = NewResponse();

        response.Htmx(h => h.TriggerAfterSwap("settled"));

        Assert.Equal("settled", response.Headers["HX-Trigger-After-Swap"]);
        Assert.False(response.Headers.ContainsKey("HX-Trigger"));
    }

    [Fact]
    public void TriggerAfterSettle_uses_separate_header()
    {
        var response = NewResponse();

        response.Htmx(h => h.TriggerAfterSettle("ready"));

        Assert.Equal("ready", response.Headers["HX-Trigger-After-Settle"]);
    }

    [Fact]
    public void All_three_trigger_phases_can_coexist()
    {
        var response = NewResponse();

        response.Htmx(h => h
            .Trigger("now")
            .TriggerAfterSwap("swap")
            .TriggerAfterSettle("settle"));

        Assert.Equal("now", response.Headers["HX-Trigger"]);
        Assert.Equal("swap", response.Headers["HX-Trigger-After-Swap"]);
        Assert.Equal("settle", response.Headers["HX-Trigger-After-Settle"]);
    }

    [Fact]
    public void Chaining_returns_same_builder()
    {
        var response = NewResponse();

        response.Htmx(h => h
            .PushUrl("/a")
            .Reswap("outerHTML")
            .Retarget("#x")
            .Trigger("saved"));

        Assert.Equal("/a", response.Headers["HX-Push-Url"]);
        Assert.Equal("outerHTML", response.Headers["HX-Reswap"]);
        Assert.Equal("#x", response.Headers["HX-Retarget"]);
        Assert.Equal("saved", response.Headers["HX-Trigger"]);
    }
}
