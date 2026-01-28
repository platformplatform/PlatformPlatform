import { useEffect, useState } from "react";

interface MarkdownRendererProps {
  path: string;
}

export function MarkdownRenderer({ path }: MarkdownRendererProps) {
  const [html, setHtml] = useState<string>("");

  useEffect(() => {
    async function loadMarkdown() {
      try {
        const response = await fetch(path);
        if (!response.ok) {
          throw new Error(`Failed to fetch ${path}: ${response.status}`);
        }
        const markdown = await response.text();

        const htmlContent = convertMarkdownToHtml(markdown);
        setHtml(htmlContent);
      } catch (error) {
        console.error("Failed to load markdown:", error);
        setHtml("<p>Failed to load content. Please try again.</p>");
      }
    }

    loadMarkdown();
  }, [path]);

  return (
    <div className="text-foreground">
      {/* biome-ignore lint/style/useNamingConvention: React's dangerouslySetInnerHTML requires __html */}
      <div dangerouslySetInnerHTML={{ __html: html }} />
    </div>
  );
}

function convertMarkdownToHtml(markdown: string): string {
  let html = markdown;

  // Store code blocks with placeholders to protect from other regex processing
  const codeBlocks: string[] = [];
  html = html.replace(/```\n([\s\S]*?)\n```/g, (_match, content) => {
    const placeholder = `___CODE_BLOCK_${codeBlocks.length}___`;
    codeBlocks.push(
      `<pre class="my-4 p-4 bg-muted rounded-md overflow-x-auto"><code class="text-xs font-mono text-foreground leading-[1.4] whitespace-pre">${content}</code></pre>`
    );
    return placeholder;
  });

  // Tables - parse markdown tables before other processing
  html = html.replace(/(?:^|\n)((?:\|[^\n]+\|\n)+)/g, (_match, tableBlock) => {
    const rows = tableBlock
      .trim()
      .split("\n")
      .filter((row: string) => row.trim());
    if (rows.length < 2) {
      return tableBlock;
    }

    const parseRow = (row: string) =>
      row
        .split("|")
        .slice(1, -1)
        .map((cell: string) => cell.trim());

    const headerCells = parseRow(rows[0]);
    const isSeparator = (row: string) => /^\|[\s\-:|]+\|$/.test(row.trim());
    const dataStartIndex = isSeparator(rows[1]) ? 2 : 1;
    const dataRows = rows.slice(dataStartIndex);

    const headerHtml = headerCells
      .map((cell: string) => `<th class="border border-border px-3 py-2 text-left font-semibold bg-muted">${cell}</th>`)
      .join("");
    const bodyHtml = dataRows
      .map((row: string) => {
        const cells = parseRow(row);
        return `<tr>${cells.map((cell: string) => `<td class="border border-border px-3 py-2">${cell}</td>`).join("")}</tr>`;
      })
      .join("");

    return `<table class="my-4 w-full border-collapse text-sm"><thead><tr>${headerHtml}</tr></thead><tbody>${bodyHtml}</tbody></table>`;
  });

  // Remove horizontal rules around warning boxes
  html = html.replace(/---\n\n> ⚠️/gm, "> ⚠️");
  html = html.replace(/\n\n---\n\n##/gm, "\n\n##");

  // Handle blockquotes (warning boxes) - support multiple lines with bullets
  html = html.replace(/^> ⚠️ \*\*(.+?)\*\*\n>\n> (.+?)\n((?:> -.+\n?)+)/gm, (_, title, intro, bullets) => {
    const bulletLines = bullets
      .split("\n")
      .map((line: string) => line.replace(/^> - /, "").trim())
      .filter((line: string) => line);
    const bulletHtml = bulletLines.map((line: string) => `<li class="text-sm leading-relaxed">${line}</li>`).join("");
    return `<div class="my-8 rounded-md border-2 border-warning bg-background p-5">
      <h3 class="text-lg font-semibold mb-3 text-foreground">⚠️ ${title}</h3>
      <p class="text-base leading-relaxed mb-2 text-foreground">${intro}</p>
      <ul class="ml-6 space-y-1 list-disc text-foreground">${bulletHtml}</ul>
    </div>`;
  });

  // Headers
  html = html.replace(/^# (.+)$/gm, '<h1 class="text-4xl font-bold mb-6 mt-0 text-foreground">$1</h1>');
  html = html.replace(/^## (.+)$/gm, '<h2 class="text-2xl font-semibold mt-8 mb-3 first:mt-0 text-foreground">$1</h2>');
  html = html.replace(/^### (.+)$/gm, '<h3 class="text-xl font-semibold mt-6 mb-2 text-foreground">$1</h3>');

  // Horizontal rules
  html = html.replace(/^---$/gm, '<hr class="my-8 border-t border-border" />');

  // Definition lists (bold term followed by description)
  html = html.replace(
    /^\*\*([^:]+):\*\* (.+)$/gm,
    '<p class="text-base leading-relaxed my-4 text-foreground"><strong class="font-semibold text-foreground">$1:</strong> $2</p>'
  );

  // Bold text
  html = html.replace(/\*\*(.+?)\*\*/g, '<strong class="font-semibold text-foreground">$1</strong>');

  // Links
  html = html.replace(
    /\[([^\]]+)\]\(([^)]+)\)/g,
    '<a href="$2" class="rounded-md text-primary underline underline-offset-4 outline-ring hover:text-primary/80 focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2">$1</a>'
  );

  // Lists - handle multi-line list items
  html = html.replace(/^- (.+)$/gm, '<li class="text-base leading-relaxed text-foreground">$1</li>');
  html = html.replace(/(<li[^>]*>.*?<\/li>\n?)+/gs, (match) => {
    return `<ul class="ml-6 my-3 space-y-1 list-disc">\n${match}</ul>\n`;
  });

  // Paragraphs (match lines that aren't already HTML tags or blank)
  html = html.replace(/^(?!<|#|>|\*\*Effective|---|\n)(.+)$/gm, (match) => {
    if (match.trim() && !match.startsWith("<")) {
      return `<p class="text-base leading-relaxed my-4 text-foreground">${match}</p>`;
    }
    return match;
  });

  // Clean up multiple newlines
  html = html.replace(/\n{3,}/g, "\n\n");

  // Restore code blocks
  codeBlocks.forEach((block, index) => {
    html = html.replace(`___CODE_BLOCK_${index}___`, block);
  });

  return html;
}
