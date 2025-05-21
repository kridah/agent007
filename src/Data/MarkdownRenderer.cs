using Markdig;
using Microsoft.AspNetCore.Components;

public class MarkdownRenderer
{

    [Parameter]
    public string Content { get; set; } = "";

    [Parameter]
    public string CssClass { get; set; } = "";

    private string RenderedMarkdown => RenderMarkdown(Content);

    private string RenderMarkdown(string markdown)
    {
        if (string.IsNullOrWhiteSpace(markdown))
            return string.Empty;

        // Configure Markdig with common extensions
        var pipeline = new MarkdownPipelineBuilder()
            .UseAdvancedExtensions()
            .DisableHtml()  // For security, disable raw HTML
            .Build();

        // Convert markdown to HTML
        return Markdown.ToHtml(markdown, pipeline);
    }
}
