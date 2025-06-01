// Add to your main layout or ChatMessage component
// Include these CDN links in your layout:
// <script src="https://cdnjs.cloudflare.com/ajax/libs/marked/5.1.1/marked.min.js"></script>
// <script src="https://cdnjs.cloudflare.com/ajax/libs/prism/1.29.0/prism.min.js"></script>
// <script src="https://cdnjs.cloudflare.com/ajax/libs/prism/1.29.0/components/prism-csharp.min.js"></script>
// <script src="https://cdnjs.cloudflare.com/ajax/libs/prism/1.29.0/components/prism-javascript.min.js"></script>
// <script src="https://cdnjs.cloudflare.com/ajax/libs/prism/1.29.0/components/prism-python.min.js"></script>
// <link rel="stylesheet" href="https://cdnjs.cloudflare.com/ajax/libs/prism/1.29.0/themes/prism.min.css">

window.MarkdownRenderer = {
    // Configure marked with custom renderer
    setupMarked: function () {
        const renderer = new marked.Renderer();

        // Custom code block renderer with syntax highlighting
        renderer.code = function (code, language) {
            const validLanguage = language && Prism.languages[language] ? language : 'text';
            const highlighted = Prism.highlight(code, Prism.languages[validLanguage] || Prism.languages.text, validLanguage);

            return `<pre class="language-${validLanguage}"><code class="language-${validLanguage}">${highlighted}</code></pre>`;
        };

        // Custom inline code renderer
        renderer.codespan = function (code) {
            return `<code class="inline-code">${code}</code>`;
        };

        marked.setOptions({
            renderer: renderer,
            breaks: true, // Support line breaks
            gfm: true,    // GitHub Flavored Markdown
        });
    },

    // Render markdown with partial content support
    renderMarkdown: function (elementId, markdownText, isStreaming = false) {
        try {
            if (!window.marked) {
                console.error('Marked.js not loaded');
                return;
            }

            const element = document.getElementById(elementId);
            if (!element) {
                console.error('Element not found:', elementId);
                return;
            }

            let processedText = markdownText;

            // Handle partial code blocks during streaming
            if (isStreaming) {
                processedText = this.handlePartialContent(markdownText);
            }

            // Render markdown
            const html = marked.parse(processedText);
            element.innerHTML = html;

            // Add streaming cursor if generating
            if (isStreaming) {
                const cursor = document.createElement('span');
                cursor.className = 'streaming-cursor';
                cursor.textContent = '▊';
                element.appendChild(cursor);
            }

        } catch (error) {
            console.error('Markdown rendering error:', error);
            // Fallback to plain text
            const element = document.getElementById(elementId);
            if (element) {
                element.textContent = markdownText;
            }
        }
    },

    // Handle partial markdown during streaming
    handlePartialContent: function (text) {
        // Count code block delimiters
        const codeBlockCount = (text.match(/```/g) || []).length;

        // If odd number of ``` and we're streaming, we have an incomplete code block
        if (codeBlockCount % 2 === 1) {
            // Add a temporary closing block for rendering
            text += '\n```';
        }

        // Handle partial inline code
        const inlineCodeCount = (text.match(/`/g) || []).length;
        if (inlineCodeCount % 2 === 1) {
            // Check if the last ` is part of a code block (```)
            const lastTripleBacktick = text.lastIndexOf('```');
            const lastSingleBacktick = text.lastIndexOf('`');

            if (lastSingleBacktick > lastTripleBacktick) {
                text += '`';
            }
        }

        return text;
    },

    // Update content during streaming
    updateStreamingContent: function (elementId, newContent) {
        this.renderMarkdown(elementId, newContent, true);
    },

    // Finalize content when streaming is complete
    finalizeContent: function (elementId, finalContent) {
        this.renderMarkdown(elementId, finalContent, false);
    }
};

// Initialize when DOM is ready
document.addEventListener('DOMContentLoaded', function () {
    if (window.marked) {
        window.MarkdownRenderer.setupMarked();
    }
});