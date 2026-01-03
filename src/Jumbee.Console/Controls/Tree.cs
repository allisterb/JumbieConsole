namespace Jumbee.Console;

using System;
using System.Collections.Generic;
using System.Reflection;
using Spectre.Console;
using Spectre.Console.Rendering;

/// <summary>
/// A wrapper for the Spectre.Console Tree control.
/// </summary>
public class Tree : SpectreControl<Spectre.Console.Tree>
{
    #region Fields
    public readonly IRenderable rootLabel;
    
    private static readonly PropertyInfo renderableProp = 
        typeof(TreeNode).GetProperty("Renderable", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public) 
        ?? throw new InvalidOperationException("Could not find 'Renderable' property on TreeNode.");
    
    private static readonly FieldInfo rootField = 
        typeof(Spectre.Console.Tree).GetField("_root", BindingFlags.Instance | BindingFlags.NonPublic)
        ?? throw new InvalidOperationException("Could not find '_root' field on Tree.");
    #endregion

    /// <summary>
    /// Initializes a new instance of the <see cref="Tree"/> class.
    /// </summary>
    /// <param name="rootLabel">The tree root label.</param>
    public Tree(IRenderable rootLabel, Style? style = null, TreeGuide? guide = null, bool expanded = true) : base(new Spectre.Console.Tree(rootLabel))
    {
        this.rootLabel = rootLabel;
        Content.Style = style;
        Content.Guide = guide ?? TreeGuide.Line;
        Content.Expanded = expanded;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Tree"/> class.
    /// </summary>
    /// <param name="root">The tree root label as a string.</param>
    public Tree(string root, Style? style = null, TreeGuide? guide = null, bool expanded = true) : 
        this(new Markup(root), style, guide, expanded) {}

    /// <summary>
    /// Gets or sets the tree guide lines.
    /// </summary>
    public TreeGuide Guide
    {
        get => Content.Guide;
        set
        {
            Content.Guide = value;
            Invalidate();
        }
    }

    /// <summary>
    /// Gets or sets the tree style.
    /// </summary>
    public Style? Style
    {
        get => Content.Style;
        set
        {
            Content.Style = value;
            Invalidate();
        }
    }

    /// <summary>
    /// Gets or sets a value indicating whether or not the tree is expanded or not.
    /// </summary>
    public bool Expanded
    {
        get => Content.Expanded;
        set
        {
            Content.Expanded = value;
            Invalidate();
        }
    }

    /// <summary>
    /// Adds a node to the tree.
    /// </summary>
    /// <param name="node">The node label.</param>
    public void AddNode(string node)
    {
        AddNode(new Markup(node));
    }

    /// <summary>
    /// Adds a node to the tree.
    /// </summary>
    /// <param name="node">The node renderable.</param>
    public void AddNode(IRenderable node)
    {
        UpdateContent(c => c.AddNode(new TreeNode(node)));
     
    }

    /// <summary>
    /// Adds multiple nodes to the tree.
    /// </summary>
    /// <param name="nodes">The node labels.</param>
    public void AddNodes(params string[] nodes)
    {
        UpdateContent(c =>
        {
            c.AddNodes(nodes);
        });       
    }

    /// <summary>
    /// Adds multiple nodes to the tree.
    /// </summary>
    /// <param name="nodes">The node renderables.</param>
    public void AddNodes(params IRenderable[] nodes)
    {
        UpdateContent(c =>
        {
            c.AddNodes(nodes);
        });        
    }

    /// <inheritdoc/>
    protected override Spectre.Console.Tree CloneContent()
    {
        var newTree = new Spectre.Console.Tree(rootLabel);
        newTree.Style = Content.Style;
        newTree.Guide = Content.Guide;
        newTree.Expanded = Content.Expanded;
        foreach (var child in Content.Nodes)
        {
            newTree.AddNode(CloneNode(child));
        }
        return newTree;
    }

    private TreeNode CloneNode(TreeNode original)
    {
        var renderable = (IRenderable?)renderableProp!.GetValue(original); 
        if (renderable == null) throw new InvalidOperationException("TreeNode renderable is null.");

        var newNode = new TreeNode(renderable);
        newNode.Expanded = original.Expanded;
        
        foreach (var child in original.Nodes)
        {
            newNode.Nodes.Add(CloneNode(child));
        }
        return newNode;
    }
}
