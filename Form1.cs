using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace CommentDetective
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog fbd = new FolderBrowserDialog
            {
                RootFolder = Environment.SpecialFolder.DesktopDirectory,
                Description = "Select folder for comment detection"
            };

            if (fbd.ShowDialog() == DialogResult.OK)
            {
                List<string> allFiles = Directory.GetFiles(fbd.SelectedPath, "*.*", SearchOption.AllDirectories).Where(file => !file.EndsWith(".gitignore")).ToList();
                List<string> allComments = new List<string>();

                string visualEffects = string.Concat(Enumerable.Repeat('=', 10));

                foreach (string file in allFiles)
                {
                    string fileName = Path.GetFileName(file);
                    string contents = File.ReadAllText(file);

                    List<string> foundComments = fileName.EndsWith(".cs") ? GetCsharpComments(contents) : GetOtherLanguageComments(contents);

                    if (foundComments.Any())
                    {
                        allComments.Add($"{visualEffects}{fileName}{visualEffects}");

                        int number = 1;
                        foreach (string comment in foundComments)
                        {
                            allComments.Add($"{number++}. {comment}");
                        }
                    }
                }

                richTextBox1.Text = string.Join("\n", allComments);
            }
        }

        private List<string> GetCsharpComments(string contents)
        {
            CommentWalker walker = new CommentWalker();
            SyntaxTree tree = CSharpSyntaxTree.ParseText(contents);
            SyntaxNode root = tree.GetRoot();
            walker.Visit(root);
            return walker.Comments;
        }
        public class CommentWalker : CSharpSyntaxWalker
        {
            public List<string> Comments;
            public CommentWalker() : base(SyntaxWalkerDepth.Trivia)
            {
                Comments = new List<string>();
            }

            public override void VisitTrivia(SyntaxTrivia trivia)
            {
                if (trivia.IsKind(SyntaxKind.SingleLineCommentTrivia)
                    || trivia.IsKind(SyntaxKind.MultiLineCommentTrivia)
                    || trivia.IsKind(SyntaxKind.SingleLineDocumentationCommentTrivia)
                    || trivia.IsKind(SyntaxKind.MultiLineDocumentationCommentTrivia))
                {
                    Comments.Add(trivia.ToFullString());
                }
            }
        }

        private List<string> GetOtherLanguageComments(string contents)
        {
            RegexOptions RegexOptionsCompiled = RegexOptions.Compiled | RegexOptions.Multiline | RegexOptions.Singleline | RegexOptions.IgnoreCase;

            Regex regex = new Regex(@"((?<!https?:)\/\/.*?$|/\*.*?\*/|<!--.*?-->|@\*.*?\*@)", RegexOptionsCompiled);
            MatchCollection matches = regex.Matches(contents);
            return matches.Cast<Match>().Select(m => m.Value).ToList();
        }
        private void richTextBox1_TextChanged(object sender, EventArgs e)
        {

        }

        private void button2_Click(object sender, EventArgs e)
        {
            using (SaveFileDialog sfd = new SaveFileDialog())
            {
                sfd.Filter = "txt files (*.txt)|*.txt|All files (*.*)|*.*";
                sfd.FilterIndex = 1;
                sfd.FileName = "Comments";
                sfd.DefaultExt = ".txt";
                sfd.Title = "Save";

                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    File.WriteAllText(sfd.FileName, richTextBox1.Text);
                }
            }
        }
    }
}
