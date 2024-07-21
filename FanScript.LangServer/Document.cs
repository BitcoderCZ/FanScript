using FanScript.Compiler.Syntax;
using FanScript.Compiler.Text;
using OmniSharp.Extensions.LanguageServer.Protocol;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;

namespace FanScript.LangServer
{
    internal class Document
    {
        public readonly DocumentUri Uri;

        public int ContentVersion { get; private set; }
        private string? content;
        public string? Content => content;

        private int contentVersionByTree;
        private SyntaxTree? tree;
        public SyntaxTree? Tree
        {
            get
            {
                if (tree is not null && contentVersionByTree == ContentVersion)
                    return tree;

                contentVersionByTree = ContentVersion;
                if (string.IsNullOrEmpty(content))
                    return null;

                return tree = SyntaxTree.Parse(SourceText.From(content, DocumentUri.GetFileSystemPath(Uri) ?? string.Empty));
            }
        }

        public Document(DocumentUri _uri)
        {
            Uri = _uri;
        }

        public void SetContent(string content, int? version)
        {
            this.content = content;
            ContentVersion = version ?? ContentVersion + 1;
        }

        public async Task<string?> GetContentAsync()
        {
            if (!string.IsNullOrEmpty(Content))
                return Content;

            try
            {
                string? path = DocumentUri.GetFileSystemPath(Uri);
                if (!string.IsNullOrEmpty(path))
                    content = await File.ReadAllTextAsync(path).ConfigureAwait(false);
            }
            catch { }

            return Content;
        }
    }
}
