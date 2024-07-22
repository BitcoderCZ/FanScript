using FanScript.Compiler;
using FanScript.Compiler.Syntax;
using FanScript.Compiler.Text;
using OmniSharp.Extensions.LanguageServer.Protocol;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace FanScript.LangServer
{
    internal class Document
    {
        public readonly DocumentUri Uri;

        private readonly object lockObj = new object();

        public int ContentVersion { get; private set; } = -1;
        private string? content;
        //public string? Content => content;

        private int treeVersion = -2;
        private SyntaxTree? tree;
        public SyntaxTree? Tree
        {
            get
            {
                lock (lockObj)
                {
                    if (treeVersion == ContentVersion)
                        return tree;

                    treeVersion = ContentVersion;
                    if (string.IsNullOrEmpty(content))
                        return null;

                    return tree = SyntaxTree.Parse(SourceText.From(content, DocumentUri.GetFileSystemPath(Uri) ?? string.Empty));
                }
            }
        }

        private int compilationVersion = -3;
        private Compilation? compilation;
        public Compilation? Compilation
        {
            get
            {
                lock (lockObj)
                {
                    if (compilationVersion == treeVersion)
                        return compilation;

                    SyntaxTree? tree = this.tree;
                    compilationVersion = treeVersion;
                    if (tree is null)
                        return null;

                    return compilation = Compilation.CreateScript(null, tree);
                }
            }
        }

        public Document(DocumentUri _uri)
        {
            Uri = _uri;
        }

        public void SetContent(string content, int? version)
        {
            lock (lockObj)
            {
                this.content = content;
                ContentVersion = version ?? ContentVersion + 1;
            }
        }

        public async Task<string?> GetContentAsync(CancellationToken cancellationToken = default)
        {
            lock (lockObj)
                if (!string.IsNullOrEmpty(content))
                    return content;

            string? _content = null;
            try
            {
                string? path = DocumentUri.GetFileSystemPath(Uri);
                if (!string.IsNullOrEmpty(path))
                    _content = await File.ReadAllTextAsync(path, cancellationToken).ConfigureAwait(false);
            }
            catch { }

            lock (lockObj)
            {
                if (!string.IsNullOrEmpty(_content))
                {
                    content = _content;
                    ContentVersion++;
                }

                return content;
            }
        }
    }
}
