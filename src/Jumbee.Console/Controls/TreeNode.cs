namespace Jumbee.Console;

using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using Spectre.Console.Rendering;
using Spectre.Console;

/// <summary>
/// Represents a tree node.
/// </summary>
/// <remarks>Based on <see cref="Spectre.Console.TreeNode"/> but updated to have mutable label</remarks>
public struct TreeNode 
{
    #region Constructors
    /// <summary>
    /// Initializes a new <see cref="TreeNode"/> instance.
    /// </summary>
    /// <param name="renderable">The tree node label.</param>
    public TreeNode(Tree tree, uint id, IRenderable label)
    {
        Id = id;
        Tree = tree;
        Label = label;
    }
    #endregion

    #region Properties
    public Tree Tree { get; }
    
    public uint Id { get; }

    public IRenderable Label 
    {
        get => field;
        set
        {
            field = value;
            Tree.UpdateNodes();
        }
    }

    public IRenderable Renderable => Label;

    internal ICollection<TreeNode> Nodes => _children.Values;

    /// <summary>
    /// Gets the tree node's child nodes.
    /// </summary>
    public ICollection<TreeNode> Children => _children.Values;

    /// <summary>
    /// Gets or sets a value indicating whether or not the tree node is expanded or not.
    /// </summary>
    public bool Expanded { get; set;  } = true;
    #endregion

    #region Methods
    public TreeNode AddChild(IRenderable label)
    {
        TreeNode c;
        bool complete = false;
        do
        {
            c = new TreeNode(this.Tree, Interlocked.Increment(ref childCount), label);
            complete = this._children.TryAdd(c.Id, c);
            if (complete) break;            
            Thread.Sleep(10);            
        }
        while (!complete);
        Tree.UpdateNodes();
        return c;
    }

    public TreeNode AddChild(string label) => AddChild(new Markup(label));

    public void AddChildren(params IRenderable[] children)
    {
        foreach (IRenderable child in children)
        {
            AddChild(child);
        }
    }

    public void AddChildren(params string[] children)
    {
        foreach (var child in children)
        {
            AddChild(child);
        }
    }
    #endregion

    #region Fields
    private ConcurrentDictionary<uint, TreeNode> _children = new ConcurrentDictionary<uint, TreeNode>();

    private uint childCount = 0;
    #endregion

}