namespace Jumbee.Console;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;

using Spectre.Console;
using Spectre.Console.Rendering;
using Spectre.Console.Interop;

using CircularTreeException = Spectre.Console.Interop.CircularTreeException;

/// <summary>
/// Displays hierarchical data in a tree layout.
/// </summary>
/// <remarks>
/// Based on <see cref="Spectre.Console.Tree"/> but modified to support mutable tree nodes, concurrent updates, and node selection via user input.
/// </remarks>
public class Tree : RenderableControl
{
    #region Constructors
    /// <summary>
    /// Initializes a new instance of the <see cref="Tree"/> class.
    /// </summary>
    /// <param name="rootLabel">The tree root label.</param>
    public Tree(IRenderable rootLabel, Style? style = null, TreeGuide? guide = null, bool expanded = true) : base()
    {
        this._rootLabel = rootLabel;
        this._root = new TreeNode(this, 0, _rootLabel);
        this._style = style ?? Style.Plain;
        this._guide = guide ?? TreeGuide.Line;
        this._expanded = expanded;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Tree"/> class.
    /// </summary>
    /// <param name="root">The tree root label as a string.</param>
    public Tree(string root, Style? style = null, TreeGuide? guide = null, bool expanded = true) : 
        this(new Markup(root), style, guide, expanded) {}
    #endregion


    #region Properties

    public TreeNode Root => _root;

    /// <summary>
    /// Gets or sets the tree style.
    /// </summary>
    public Style Style
    {
        get => _style;
        set
        {
            _style = value;
            Invalidate();
        }
    }
    /// <summary>
    /// Gets or sets the tree guide lines.
    /// </summary>
    public TreeGuide Guide
    {
        get => _guide;
        set
        {
            _guide = value;
            Invalidate();
        }
    }
    
    /// <summary>
    /// Gets or sets a value indicating whether or not the tree is expanded or not.
    /// </summary>
    public bool Expanded
    {
        get => _expanded;
        set
        {
            _expanded = value;
            Invalidate();
        }
    }

    internal ICollection<TreeNode> Nodes => _root.Children;

    #endregion

    #region Methods
    internal void UpdateNodes() => this.Invalidate();

    protected override IEnumerable<Segment> Render(RenderOptions options, int maxWidth)
    {
        var result = new List<Segment>();
        var visitedNodes = new HashSet<TreeNode>();

        var stack = new Stack<Queue<TreeNode>>();
        stack.Push(new Queue<TreeNode>(new[] { _root }));

        var levels = new List<Segment>();
        levels.Add(GetGuide(options, TreeGuidePart.Continue));

        while (stack.Count > 0)
        {
            var stackNode = stack.Pop();
            if (stackNode.Count == 0)
            {
                //Spectre.Console.
                levels.RemoveLast();
                if (levels.Count > 0)
                {
                    levels.AddOrReplaceLast(GetGuide(options, TreeGuidePart.Fork));
                }

                continue;
            }

            var isLastChild = stackNode.Count == 1;
            var current = stackNode.Dequeue();
            if (!visitedNodes.Add(current))
            {
                throw new CircularTreeException("Cycle detected in tree - unable to render.");
            }

            stack.Push(stackNode);

            if (isLastChild)
            {
                levels.AddOrReplaceLast(GetGuide(options, TreeGuidePart.End));
            }

            var prefix = levels.Skip(1).ToList();
            var renderableLines = Segment.SplitLines(current.Renderable.Render(options, maxWidth - Segment.CellCount(prefix)));

            foreach (var (_, isFirstLine, _, line) in renderableLines.Enumerate())
            {
                if (prefix.Count > 0)
                {
                    result.AddRange(prefix.ToList());
                }

                result.AddRange(line);
                result.Add(Segment.LineBreak);

                if (isFirstLine && prefix.Count > 0)
                {
                    var part = isLastChild ? TreeGuidePart.Space : TreeGuidePart.Continue;
                    prefix.AddOrReplaceLast(GetGuide(options, part));
                }
            }

            if (current.Expanded && current.Nodes.Count > 0)
            {
                levels.AddOrReplaceLast(GetGuide(options, isLastChild ? TreeGuidePart.Space : TreeGuidePart.Continue));
                levels.Add(GetGuide(options, current.Nodes.Count == 1 ? TreeGuidePart.End : TreeGuidePart.Fork));

                stack.Push(new Queue<TreeNode>(current.Nodes));
            }
        }

        return result;
    }
    
    private Segment GetGuide(RenderOptions options, TreeGuidePart part)
    {
        var guide = Guide.GetSafeTreeGuide(safe: !options.Unicode);
        return new Segment(guide.GetPart(part), Style ?? Style.Plain);
    }
    #endregion

    #region Fields
    public IRenderable _rootLabel;
    public TreeNode _root;
    protected Style _style;
    protected TreeGuide _guide;
    protected bool _expanded;
    #endregion
}
