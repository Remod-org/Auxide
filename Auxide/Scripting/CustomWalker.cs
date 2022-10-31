using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Auxide.Scripting
{
    public class TreeWalk : CSharpSyntaxWalker
    {
        static int Tabs = 0;
        public override void Visit(SyntaxNode node)
        {
            Tabs++;
            string indents = new string('\t', Tabs);
            Utils.DoLog(indents + node.Kind());
            base.Visit(node);
            Tabs--;
        }
    }
}
