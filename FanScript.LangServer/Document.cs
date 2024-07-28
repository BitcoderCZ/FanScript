using FanScript.Compiler;
using FanScript.Compiler.Syntax;
using FanScript.Compiler.Text;
using OmniSharp.Extensions.LanguageServer.Protocol;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace FanScript.LangServer
{
    internal class Document
    {
        public readonly DocumentUri Uri;

        private readonly object lockObj = new object();

        public int ContentVersion { get; private set; } = -1;
        private string? content;

        private int treeVersion = -2;
        private SyntaxTree? tree;
        /// <summary>
        /// Gets a <see cref="SyntaxTree"/> for the current content
        /// </summary>
        /// <remarks>
        /// Make sure to make a local variable, might change between accesses
        /// </remarks>
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
        /// <summary>
        /// Gets a <see cref="Compiler.Compilation"/> for the current content
        /// </summary>
        /// <remarks>
        /// Make sure to make a local variable, might change between accesses
        /// </remarks>
        public Compilation? Compilation
        {
            get
            {
                lock (lockObj)
                {
                    if (compilationVersion == treeVersion && treeVersion == ContentVersion)
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
