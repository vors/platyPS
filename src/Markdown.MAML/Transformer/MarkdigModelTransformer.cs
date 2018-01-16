using System;
using System.Collections.Generic;
using System.Linq;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using Markdown.MAML.Model.MAML;
using Markdown.MAML.Model.Markdown;

namespace Markdown.MAML.Transformer
{
    public class MarkdigModelTransformer
    {

        private MarkdownDocument _root;
        private IEnumerator<Block> _rootEnumerator;
        private Action<string> _infoCallback;
        private Action<string> _warningCallback;

        internal const int COMMAND_NAME_HEADING_LEVEL = 1;
        internal const int COMMAND_ENTRIES_HEADING_LEVEL = 2;
        internal const int PARAMETER_NAME_HEADING_LEVEL = 3;
        internal const int INPUT_OUTPUT_TYPENAME_HEADING_LEVEL = 3;
        internal const int EXAMPLE_HEADING_LEVEL = 3;
        internal const int PARAMETERSET_NAME_HEADING_LEVEL = 3;

        private string[] _applicableTags;

        public MarkdigModelTransformer(Action<string> infoCallback, Action<string> warningCallback, string[] applicableTags)
        {
            _applicableTags = applicableTags;
            _infoCallback = infoCallback;
            _warningCallback = warningCallback;
        }

        public IEnumerable<MamlCommand> MarkdigModelToMamlModel(MarkdownDocument doc)
        {
            _root = doc;
            _rootEnumerator = _root.GetEnumerator();

            List<MamlCommand> commands = new List<MamlCommand>();
            MarkdownObject markdownNode;
            while ((markdownNode = GetNextNode()) != null)
            {
                if (markdownNode is HeadingBlock)
                {
                    var headingNode = markdownNode as HeadingBlock;
                    switch (headingNode.Level)
                    {
                        case COMMAND_NAME_HEADING_LEVEL:
                            {
                                
                                MamlCommand command = new MamlCommand()
                                {
                                    Name = GetText(headingNode),
                                    Extent = new SourceExtent(headingNode),
                                    // we have explicit entry for common parameters in markdown
                                    SupportCommonParameters = false
                                };

                                if (_infoCallback != null)
                                {
                                    _infoCallback.Invoke("Start processing command " + command.Name);
                                }

                                // fill up command 
                                while (SectionDispatch(command)) { }

                                commands.Add(command);
                                break;
                            }
                        default: throw new HelpSchemaException(new SourceExtent(headingNode), "Booo, I don't know what is the heading level " + headingNode.Level);
                    }
                }
            }
            return commands;
        }

        private MarkdownObject _ungotNode { get; set; }

        protected MarkdownObject GetCurrentNode()
        {
            if (_ungotNode != null)
            {
                var node = _ungotNode;
                return node;
            }

            return _rootEnumerator.Current;
        }

        protected MarkdownObject GetNextNode()
        {
            if (_ungotNode != null)
            {
                _ungotNode = null;
                return _rootEnumerator.Current;
            }

            if (_rootEnumerator.MoveNext())
            {
                return _rootEnumerator.Current;
            }

            return null;
        }

        private string GetText(HeadingBlock headingBlock)
        {
            var slice = ((headingBlock.Inline as ContainerInline).FirstChild as LiteralInline).Content;
            return slice.Text.Substring(slice.Start, slice.End - slice.Start + 1);
        }

        protected void UngetNode(MarkdownObject node)
        {
            if (_ungotNode != null)
            {
                throw new ArgumentException("Cannot ungot token, already ungot one");
            }

            _ungotNode = node;
        }

        protected string SimpleTextSectionRule()
        {
            // grammar:
            // Simple paragraph Text
            return GetTextFromParagraphNode(ParagraphNodeRule());
        }

        //protected void InputsRule(MamlCommand commmand)
        //{
        //    MamlInputOutput input;
        //    while ((input = InputOutputRule()) != null)
        //    {
        //        commmand.Inputs.Add(input);
        //    }
        //}

        //protected void OutputsRule(MamlCommand commmand)
        //{
        //    MamlInputOutput output;
        //    while ((output = InputOutputRule()) != null)
        //    {
        //        commmand.Outputs.Add(output);
        //    }
        //}

        //protected void ExamplesRule(MamlCommand commmand)
        //{
        //    MamlExample example;
        //    while ((example = ExampleRule()) != null)
        //    {
        //        commmand.Examples.Add(example);
        //    }
        //}

        //protected MamlExample ExampleRule()
        //{
        //    // grammar:
        //    // #### ExampleTitle
        //    // Introduction
        //    // ```
        //    // code
        //    // ```
        //    // Remarks
        //    var node = GetNextNode();
        //    try
        //    {
        //        var headingNode = GetHeadingWithExpectedLevel(node, EXAMPLE_HEADING_LEVEL);

        //        if (headingNode == null)
        //        {
        //            return null;
        //        }

        //        MamlExample example = new MamlExample()
        //        {
        //            Title = headingNode.Text
        //        };
        //        example.Introduction = GetTextFromParagraphNode(ParagraphNodeRule());
        //        example.FormatOption = headingNode.FormatOption;
        //        CodeBlockNode codeBlockNode;
        //        List<MamlCodeBlock> codeBlocks = new List<MamlCodeBlock>();

        //        while ((codeBlockNode = CodeBlockRule()) != null)
        //        {
        //            codeBlocks.Add(new MamlCodeBlock(
        //                codeBlockNode.Text,
        //                codeBlockNode.LanguageMoniker
        //            ));
        //        }

        //        example.Code = codeBlocks.ToArray();

        //        example.Remarks = GetTextFromParagraphNode(ParagraphNodeRule());

        //        return example;
        //    }
        //    catch (HelpSchemaException headingException)
        //    {
        //        Report("Schema exception. This can occur when there are multiple code blocks in one example. " + headingException.Message);

        //        throw headingException;
        //    }

        //}

        //protected void RelatedLinksRule(MamlCommand commmand)
        //{
        //    var paragraphNode = ParagraphNodeRule();
        //    if (paragraphNode == null)
        //    {
        //        return;
        //    }

        //    foreach (var paragraphSpan in paragraphNode.Spans)
        //    {
        //        if (paragraphSpan.ParserMode == ParserMode.FormattingPreserve)
        //        {
        //            commmand.Links.Add(new MamlLink(isSimplifiedTextLink: true)
        //            {
        //                LinkName = paragraphSpan.Text,
        //            });
        //        }
        //        else
        //        {
        //            var linkSpan = paragraphSpan as HyperlinkSpan;
        //            if (linkSpan != null)
        //            {
        //                commmand.Links.Add(new MamlLink()
        //                {
        //                    LinkName = linkSpan.Text,
        //                    LinkUri = linkSpan.Uri
        //                });
        //            }
        //            else
        //            {
        //                throw new HelpSchemaException(paragraphSpan.SourceExtent, "Expect hyperlink, but got " + paragraphSpan.Text);
        //            }
        //        }
        //    }
        //}

        //protected MamlInputOutput InputOutputRule()
        //{
        //    // grammar:
        //    // #### TypeName
        //    // Description
        //    var node = GetNextNode();
        //    var headingNode = GetHeadingWithExpectedLevel(node, INPUT_OUTPUT_TYPENAME_HEADING_LEVEL);
        //    if (headingNode == null)
        //    {
        //        return null;
        //    }

        //    MamlInputOutput typeEntity = new MamlInputOutput()
        //    {
        //        TypeName = headingNode.Text,
        //        Description = SimpleTextSectionRule(),
        //        FormatOption = headingNode.FormatOption
        //    };

        //    return typeEntity;
        //}

        protected SourceExtent GetExtent(MarkdownObject node)
        {
            return new SourceExtent(node);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="node"></param>
        /// <param name="level"></param>
        /// <returns>
        /// return headingNode if expected heading level encounterd.
        /// null, if higher level encountered.
        /// throw exception, if unexpected node encountered.
        /// </returns>
        protected HeadingBlock GetHeadingWithExpectedLevel(MarkdownObject node, int level)
        {
            if (node == null)
            {
                return null;
            }

            // check for appropriate header
            var headingNode = node as HeadingBlock;
            if (headingNode == null)
            {
                throw new HelpSchemaException(GetExtent(node), "Expect Heading");
            }

            if (headingNode.Level < level)
            {
                UngetNode(node);
                return null;
            }

            if (headingNode.Level != level)
            {
                throw new HelpSchemaException(GetExtent(headingNode), "Expect Heading level " + level);
            }
            return headingNode;
        }

        /// <summary>
        /// </summary>
        /// <returns>
        /// return paragraphNode if encounterd.
        /// null, if any header level encountered.
        /// throw exception, if other unexpected node encountered.
        /// </returns>
        protected ParagraphBlock ParagraphNodeRule()
        {
            var node = GetNextNode();
            if (node == null)
            {
                return null;
            }

            if (node is HeadingBlock || node is FencedCodeBlock)
            {
                UngetNode(node);
                return null;
            }

            if (node is ParagraphBlock)
            {
                return node as ParagraphBlock;
            }

            throw new HelpSchemaException(GetExtent(node), "Expect Paragraph");
        }

        //protected string ParagraphOrCodeBlockNodeRule(string excludeLanguageMoniker)
        //{
        //    var res = new List<string>();
        //    MarkdownNode node;

        //    while ((node = GetNextNode()) != null)
        //    {
        //        bool breakFlag = false;
        //        switch (node.NodeType)
        //        {
        //            case MarkdownNodeType.Paragraph:
        //                {
        //                    res.Add(GetTextFromParagraphNode(node as ParagraphNode));
        //                    break;
        //                }
        //            case MarkdownNodeType.CodeBlock:
        //                {
        //                    var codeblock = node as CodeBlockNode;
        //                    if (!String.Equals(excludeLanguageMoniker, codeblock.LanguageMoniker, StringComparison.OrdinalIgnoreCase))
        //                    {
        //                        res.Add(codeblock.Text);
        //                    }
        //                    else
        //                    {
        //                        UngetNode(node);
        //                        breakFlag = true;
        //                    }

        //                    break;
        //                }
        //            case MarkdownNodeType.Heading:
        //                {
        //                    UngetNode(node);
        //                    breakFlag = true;
        //                    break;
        //                }
        //            default:
        //                {
        //                    throw new HelpSchemaException(GetExtent(node), "Expect Paragraph or CodeBlock");
        //                }
        //        }

        //        if (breakFlag)
        //        {
        //            break;
        //        }
        //    }

        //    return string.Join("\r\n\r\n", res);
        //}

        ///// <summary>
        ///// </summary>
        ///// <returns>
        ///// return paragraphNode if encounterd.
        ///// null, if any header level encountered.
        ///// throw exception, if other unexpected node encountered.
        ///// </returns>
        //protected CodeBlockNode CodeBlockRule()
        //{
        //    var node = GetNextNode();
        //    if (node == null)
        //    {
        //        return null;
        //    }

        //    switch (node.NodeType)
        //    {
        //        case MarkdownNodeType.CodeBlock:
        //            break;
        //        case MarkdownNodeType.Heading:
        //        case MarkdownNodeType.Paragraph:
        //            UngetNode(node);
        //            return null;
        //        default:
        //            throw new HelpSchemaException(GetExtent(node), "Expect CodeBlock");
        //    }

        //    return node as CodeBlockNode;
        //}

        //private string GetTextFromParagraphSpans(IEnumerable<ParagraphSpan> spans)
        //{
        //    // in preserve formatting there is only one span all the time
        //    if (spans.Count() == 1)
        //    {
        //        var textSpan = spans.First() as TextSpan;
        //        if (textSpan.ParserMode == ParserMode.FormattingPreserve)
        //        {
        //            return textSpan.Text;
        //        }
        //    }

        //    StringBuilder sb = new StringBuilder();
        //    bool first = true;
        //    bool previousSpanIsSpecial = false;
        //    foreach (var paragraphSpan in spans)
        //    {
        //        // TODO: make it handle hyperlinks, codesnippets, italic, bold etc more wisely
        //        HyperlinkSpan hyperlink = paragraphSpan as HyperlinkSpan;
        //        TextSpan text = paragraphSpan as TextSpan;
        //        bool spanIsSpecial = hyperlink != null || (text != null && text.Style != TextSpanStyle.Normal);
        //        if (!first && spanIsSpecial)
        //        {
        //            sb.Append(" ");
        //        }
        //        else if (previousSpanIsSpecial)
        //        {
        //            sb.Append(" ");
        //        }

        //        sb.Append(paragraphSpan.Text);
        //        previousSpanIsSpecial = spanIsSpecial;
        //        if (hyperlink != null)
        //        {
        //            if (!string.IsNullOrWhiteSpace(hyperlink.Uri))
        //            {
        //                sb.AppendFormat(" ({0})", hyperlink.Uri);
        //                previousSpanIsSpecial = false;
        //            }
        //        }

        //        first = false;

        //    }
        //    return sb.ToString();
        //}

        protected string GetTextFromParagraphNode(ParagraphBlock node)
        {
            if (node == null)
            {
                return "";
            }
            // TODO(markdig): this is a hack, replace it!
            var firstInline = node.Inline.FirstChild as LiteralInline;
            return firstInline.Content.Text.Substring(node.Span.Start, node.Span.End - node.Span.Start + 1);
            //return GetTextFromParagraphSpans(node.Spans);
        }

        protected void Report(string warning)
        {
            if (_warningCallback != null)
            {
                _warningCallback.Invoke("Error encountered: " + warning);
            }
        }

        protected bool SectionDispatch(MamlCommand command)
        {
            var node = GetNextNode();
            var headingNode = GetHeadingWithExpectedLevel(node, COMMAND_ENTRIES_HEADING_LEVEL);
            if (headingNode == null)
            {
                return false;
            }

            // TODO: When we are going to implement Localization story, we would need to replace
            // this strings by MarkdownStrings values.

            // TODO(markdig): restore SectionFormatOption passing instead of SectionFormatOption.None

            switch (GetText(headingNode).ToUpper())
            {
                case "DESCRIPTION":
                    {
                        command.Description = new SectionBody(SimpleTextSectionRule(), SectionFormatOption.None);
                        break;
                    }
                case "SYNOPSIS":
                    {
                        command.Synopsis = new SectionBody(SimpleTextSectionRule(), SectionFormatOption.None);
                        break;
                    }
                case "SYNTAX":
                    {
                        //SyntaxRule(command);
                        break;
                    }
                case "EXAMPLES":
                    {
                        //ExamplesRule(command);
                        break;
                    }
                case "PARAMETERS":
                    {
                        //ParametersRule(command);
                        break;
                    }
                case "INPUTS":
                    {
                        //InputsRule(command);
                        break;
                    }
                case "OUTPUTS":
                    {
                        //OutputsRule(command);
                        break;
                    }
                case "NOTES":
                    {
                        command.Notes = new SectionBody(SimpleTextSectionRule(), SectionFormatOption.None);
                        break;
                    }
                case "RELATED LINKS":
                    {
                        //RelatedLinksRule(command);
                        break;
                    }
                default:
                    {
                        throw new HelpSchemaException(GetExtent(headingNode), "Unexpected header name " + GetText(headingNode));
                    }
            }
            return true;
        }

        //protected void SyntaxRule(MamlCommand commmand)
        //{
        //    MamlSyntax syntax;
        //    while ((syntax = SyntaxEntryRule()) != null)
        //    {
        //        //this is the only way to retain information on which syntax is the default 
        //        // without adding new members to command object.
        //        //Though the cmdlet object, does have a member which contains the default syntax name only.
        //        if (syntax.IsDefault) { commmand.Syntax.Add(syntax); }
        //    }
        //}

        //protected MamlSyntax SyntaxEntryRule()
        //{
        //    // grammar:
        //    // ### ParameterSetName 
        //    // ```
        //    // code
        //    // ```

        //    MamlSyntax syntax;

        //    var node = GetNextNode();
        //    if (node.NodeType == MarkdownNodeType.CodeBlock)
        //    {
        //        // if header is omitted
        //        syntax = new MamlSyntax()
        //        {
        //            ParameterSetName = ALL_PARAM_SETS_MONIKER,
        //            IsDefault = true
        //        };
        //    }
        //    else
        //    {
        //        var headingNode = GetHeadingWithExpectedLevel(node, PARAMETERSET_NAME_HEADING_LEVEL);
        //        if (headingNode == null)
        //        {
        //            return null;
        //        }

        //        bool isDefault = headingNode.Text.EndsWith(MarkdownStrings.DefaultParameterSetModifier);
        //        syntax = new MamlSyntax()
        //        {
        //            ParameterSetName = isDefault ? headingNode.Text.Substring(0, headingNode.Text.Length - MarkdownStrings.DefaultParameterSetModifier.Length) : headingNode.Text,
        //            IsDefault = isDefault
        //        };

        //        var codeBlock = CodeBlockRule();
        //    }
        //    // we don't use the output of it
        //    // TODO: we should capture syntax and verify that it's complient.
        //    return syntax;
        //}

        //protected void ParametersRule(MamlCommand command)
        //{
        //    while (ParameterRule(command))
        //    {
        //    }

        //    GatherSyntax(command);
        //}

        //private void FillUpSyntax(MamlSyntax syntax, string name)
        //{
        //    var parametersList = new List<MamlParameter>();

        //    foreach (var pair in _parameterName2ParameterSetMap)
        //    {
        //        MamlParameter param = null;
        //        if (pair.Item2.ContainsKey(name))
        //        {
        //            param = pair.Item2[name];
        //        }
        //        else
        //        {
        //            if (pair.Item2.Count == 1 && pair.Item2.First().Key == ALL_PARAM_SETS_MONIKER)
        //            {
        //                param = pair.Item2.First().Value;
        //            }
        //        }
        //        if (param != null)
        //        {
        //            parametersList.Add(param);
        //        }
        //    }

        //    // order parameters based on position
        //    // User OrderBy instead of Sort for stable sort
        //    syntax.Parameters.AddRange(parametersList.OrderBy(x => x.Position));
        //}

        //private void GatherSyntax(MamlCommand command)
        //{
        //    var parameterSetNames = GetParameterSetNames();
        //    var defaultSetName = string.Empty;

        //    if (command.Syntax.Count == 1 && command.Syntax[0].IsDefault)
        //    {
        //        //checks for existing IsDefault paramset and remove it while saving the name
        //        defaultSetName = command.Syntax[0].ParameterSetName;
        //        command.Syntax.Remove(command.Syntax[0]);
        //    }

        //    if (parameterSetNames.Count == 0)
        //    {
        //        // special case: there are no parameters and hence there is only one parameter set
        //        MamlSyntax syntax = new MamlSyntax();
        //        command.Syntax.Add(syntax);
        //    }

        //    foreach (var setName in parameterSetNames)
        //    {
        //        MamlSyntax syntax = new MamlSyntax();
        //        if (setName == ALL_PARAM_SETS_MONIKER)
        //        {
        //            if (parameterSetNames.Count == 1)
        //            {
        //                // special case: there is only one parameter set and it's the default one
        //                // we don't specify the name in this case.
        //            }
        //            else
        //            {
        //                continue;
        //            }
        //        }
        //        else
        //        {
        //            syntax.ParameterSetName = StringComparer.OrdinalIgnoreCase.Equals(syntax.ParameterSetName, defaultSetName)
        //                ? string.Format("{0}{1}", setName, MarkdownStrings.DefaultParameterSetModifier)
        //                : setName;
        //        }

        //        FillUpSyntax(syntax, setName);
        //        command.Syntax.Add(syntax);
        //    }
        //}

        //private List<string> GetParameterSetNames()
        //{
        //    // Inefficient alogrithm, but it's fine, because all collections are pretty small.
        //    var parameterSetNames = new List<string>();
        //    foreach (var pair in _parameterName2ParameterSetMap)
        //    {
        //        foreach (var pair2 in pair.Item2)
        //        {
        //            var paramSetName = pair2.Key;

        //            bool found = false;
        //            foreach (var candidate in parameterSetNames)
        //            {
        //                if (StringComparer.OrdinalIgnoreCase.Equals(candidate, paramSetName))
        //                {
        //                    found = true;
        //                    break;
        //                }
        //            }

        //            if (!found)
        //            {
        //                parameterSetNames.Add(paramSetName);
        //            }
        //        }
        //    }

        //    return parameterSetNames;
        //}

        //private bool IsKnownKey(string key)
        //{
        //    return StringComparer.OrdinalIgnoreCase.Equals(key, MarkdownStrings.Type) ||
        //        StringComparer.OrdinalIgnoreCase.Equals(key, MarkdownStrings.Parameter_Sets) ||
        //        StringComparer.OrdinalIgnoreCase.Equals(key, MarkdownStrings.Aliases) ||
        //        StringComparer.OrdinalIgnoreCase.Equals(key, MarkdownStrings.Accepted_values) ||
        //        StringComparer.OrdinalIgnoreCase.Equals(key, MarkdownStrings.Required) ||
        //        StringComparer.OrdinalIgnoreCase.Equals(key, MarkdownStrings.Position) ||
        //        StringComparer.OrdinalIgnoreCase.Equals(key, MarkdownStrings.Default_value) ||
        //        StringComparer.OrdinalIgnoreCase.Equals(key, MarkdownStrings.Accept_pipeline_input) ||
        //        StringComparer.OrdinalIgnoreCase.Equals(key, MarkdownStrings.Accept_wildcard_characters) ||
        //        StringComparer.OrdinalIgnoreCase.Equals(key, MarkdownStrings.Applicable);
        //}

        ///// <summary>
        ///// we only parse simple key-value pairs here
        ///// </summary>
        ///// <param name="yamlSnippet"></param>
        ///// <returns></returns>
        //private Dictionary<string, string> ParseYamlKeyValuePairs(CodeBlockNode yamlSnippet)
        //{
        //    Dictionary<string, string> result;
        //    try
        //    {
        //        result = MarkdownParser.ParseYamlKeyValuePairs(yamlSnippet.Text);
        //    }
        //    catch (ArgumentException)
        //    {
        //        throw new HelpSchemaException(yamlSnippet.SourceExtent, "Invalid yaml: expected simple key-value pairs");
        //    }

        //    foreach (var pair in result)
        //    {
        //        if (!IsKnownKey(pair.Key))
        //        {
        //            throw new HelpSchemaException(yamlSnippet.SourceExtent, "Invalid yaml: unknown key " + pair.Key);
        //        }
        //    }

        //    return result;
        //}

        //private string[] SplitByCommaAndTrim(string input)
        //{
        //    if (input == null)
        //    {
        //        return new string[0];
        //    }

        //    return input.Split(',').Select(x => x.Trim()).ToArray();
        //}

        //private void FillUpParameterFromKeyValuePairs(Dictionary<string, string> pairs, MamlParameter parameter)
        //{
        //    // for all null keys, we should ignore the value in this context
        //    var newPairs = new Dictionary<string, string>(pairs.Comparer);

        //    foreach (var pair in pairs)
        //    {
        //        if (pair.Value != null)
        //        {
        //            newPairs[pair.Key] = pair.Value;
        //        }
        //    }

        //    pairs = newPairs;

        //    string value;
        //    parameter.Type = pairs.TryGetValue(MarkdownStrings.Type, out value) ? value : null;
        //    parameter.Aliases = pairs.TryGetValue(MarkdownStrings.Aliases, out value) ? SplitByCommaAndTrim(value) : new string[0];
        //    parameter.ParameterValueGroup.AddRange(pairs.TryGetValue(MarkdownStrings.Accepted_values, out value) ? SplitByCommaAndTrim(value) : new string[0]);
        //    parameter.Required = pairs.TryGetValue(MarkdownStrings.Required, out value) ? StringComparer.OrdinalIgnoreCase.Equals("true", value) : false;
        //    parameter.Position = pairs.TryGetValue(MarkdownStrings.Position, out value) ? value : "named";
        //    parameter.DefaultValue = pairs.TryGetValue(MarkdownStrings.Default_value, out value) ? value : null;
        //    parameter.PipelineInput = pairs.TryGetValue(MarkdownStrings.Accept_pipeline_input, out value) ? value : "false";
        //    parameter.Globbing = pairs.TryGetValue(MarkdownStrings.Accept_wildcard_characters, out value) ? StringComparer.OrdinalIgnoreCase.Equals("true", value) : false;
        //    // having Applicable for the whole parameter is a little bit sloppy: ideally it should be per yaml entry.
        //    // but that will make the code super ugly and it's unlikely that these two features would need to be used together.
        //    parameter.Applicable = pairs.TryGetValue(MarkdownStrings.Applicable, out value) ? SplitByCommaAndTrim(value) : null;
        //}

        //private bool ParameterRule(MamlCommand commmand)
        //{
        //    // grammar:
        //    // #### -Name
        //    // Description              -  optional, there also could be codesnippets in the description
        //    //                             but no yaml codesnippets
        //    //
        //    // ```yaml                  -  one entry for every unique parameter metadata set
        //    // ...
        //    // ```

        //    var node = GetNextNode();
        //    var headingNode = GetHeadingWithExpectedLevel(node, PARAMETER_NAME_HEADING_LEVEL);
        //    if (headingNode == null)
        //    {
        //        return false;
        //    }

        //    var name = headingNode.Text;
        //    if (name.Length > 0 && name[0] == '-')
        //    {
        //        name = name.Substring(1);
        //    }

        //    MamlParameter parameter = new MamlParameter()
        //    {
        //        Name = name,
        //        Extent = headingNode.SourceExtent
        //    };

        //    parameter.Description = ParagraphOrCodeBlockNodeRule("yaml");
        //    parameter.FormatOption = headingNode.FormatOption;

        //    if (StringComparer.OrdinalIgnoreCase.Equals(parameter.Name, MarkdownStrings.CommonParametersToken))
        //    {
        //        // ignore text body
        //        commmand.SupportCommonParameters = true;
        //        return true;
        //    }

        //    if (StringComparer.OrdinalIgnoreCase.Equals(parameter.Name, MarkdownStrings.WorkflowParametersToken))
        //    {
        //        // ignore text body
        //        commmand.IsWorkflow = true;
        //        return true;
        //    }

        //    // we are filling up two pieces here: Syntax and Parameters
        //    // we are adding this parameter object to the parameters and later modifying it
        //    // in the rare case, when there are multiply yaml snippets,
        //    // the first one should be present in the resulted maml in the Parameters section
        //    // (all of them would be present in Syntax entry)
        //    var parameterSetMap = new Dictionary<string, MamlParameter>(StringComparer.OrdinalIgnoreCase);

        //    CodeBlockNode codeBlock;

        //    // fill up couple other things, even if there are no codeBlocks
        //    // if there are, we will fill it up inside
        //    parameter.ValueRequired = true;

        //    // First parameter is what should be used in the Parameters section
        //    MamlParameter firstParameter = null;
        //    bool isAtLeastOneYaml = false;

        //    while ((codeBlock = CodeBlockRule()) != null)
        //    {
        //        isAtLeastOneYaml = true;
        //        var yaml = ParseYamlKeyValuePairs(codeBlock);
        //        FillUpParameterFromKeyValuePairs(yaml, parameter);

        //        parameter.ValueRequired = parameter.IsSwitchParameter() ? false : true;

        //        // handle applicable tag
        //        if (parameter.IsApplicable(this._applicableTag))
        //        {
        //            if (firstParameter == null)
        //            {
        //                firstParameter = parameter;
        //            }

        //            // handle parameter sets
        //            if (yaml.ContainsKey(MarkdownStrings.Parameter_Sets))
        //            {
        //                foreach (string parameterSetName in SplitByCommaAndTrim(yaml[MarkdownStrings.Parameter_Sets]))
        //                {
        //                    if (string.IsNullOrEmpty(parameterSetName))
        //                    {
        //                        continue;
        //                    }

        //                    parameterSetMap[parameterSetName] = parameter;
        //                }
        //            }
        //            else
        //            {
        //                parameterSetMap[ALL_PARAM_SETS_MONIKER] = parameter;
        //            }
        //        }

        //        // in the rare case, when there are multiply yaml snippets
        //        parameter = parameter.Clone();
        //    }

        //    if (!isAtLeastOneYaml)
        //    {
        //        // if no yaml are present it's a special case and we leave it as is
        //        firstParameter = parameter;
        //    }

        //    // capture these two piece of information
        //    if (firstParameter != null)
        //    {
        //        commmand.Parameters.Add(firstParameter);
        //        _parameterName2ParameterSetMap.Add(Tuple.Create(name, parameterSetMap));
        //    }

        //    return true;
        //}
    }
}
