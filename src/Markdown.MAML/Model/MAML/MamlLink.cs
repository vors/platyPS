using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Markdown.MAML.Model.MAML
{
    /// <summary>
    /// This class represents the related links properties for MAML
    /// </summary>
    public class MamlLink
    {
        public string LinkName { get; set; }
        public string LinkUri { get; set; }

        // we use our own poor-man verison intermidiate format
        // to represent hyperlinks in maml model inline with text
        public const char HYPERLINK_START_MARKER = (char)12345;
        public const char HYPERLINK_MIDDLE_MARKER = (char)12346;
        public const char HYPERLINK_END_MARKER = (char)12347;
    }
}
