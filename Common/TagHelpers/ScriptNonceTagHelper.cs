using Microsoft.AspNetCore.Razor.TagHelpers;

namespace NEXUS.Common.TagHelpers;

/**
 * Stamps the per-request CSP nonce onto every inline <script> block.
 *
 * The alternative is hand-editing fifteen views and remembering to do it on
 * the sixteenth. Centralising it here means a new inline block is covered the
 * moment it is written, and script files loaded with src need nothing at all -
 * their bytes are already covered by 'self'.
 */
[HtmlTargetElement("script")]
public sealed class ScriptNonceTagHelper : TagHelper
{
    private readonly IHttpContextAccessor _http;

    public ScriptNonceTagHelper(IHttpContextAccessor http) => _http = http;

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        // An external file is allowed by the origin, not by a nonce. Only the
        // blocks the browser would otherwise refuse need one.
        if (output.Attributes.ContainsName("src")) return;

        var nonce = _http.HttpContext?.Items["csp-nonce"] as string;

        if (!string.IsNullOrEmpty(nonce))
            output.Attributes.SetAttribute("nonce", nonce);
    }
}
