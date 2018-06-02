using System;
using System.IO;
using Markdig.Renderers;

namespace Markdown.MAML.Transformer
{
    /// <summary>
    /// This is a helper renderer that handles complex nested markdig ASTs
    /// and provides ability to render a simpler MamlModel-competible primitives.
    /// </summary>
    class MarkdigRenderer : TextRendererBase<MarkdigRenderer>
    {
        public MarkdigRenderer(TextWriter writer) : base(writer)
        {
        }
    }
}
