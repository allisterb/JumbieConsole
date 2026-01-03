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
    protected readonly IRenderable _rootLabel;
    private static readonly PropertyInfo? _renderableProp = typeof(TreeNode).GetProperty("Renderable", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

    /// <summary>
    /// Initializes a new instance of the <see cref="Tree"/> class.
    /// </summary>
    /// <param name="rootLabel">The tree root label.</param>
    public Tree(IRenderable rootLabel) : base(new Spectre.Console.Tree(rootLabel))
    {
        _rootLabel = rootLabel;
        contentBuffer = CloneContent();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Tree"/> class.
    /// </summary>
    /// <param name="root">The tree root label as a string.</param>
    public Tree(string root) : this(new Markup(root))
    {
    }

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
        UpdateContentBuffer();
        contentBuffer.Nodes.Add(new TreeNode(node));
        SwapContentBuffer();
    }

    /// <summary>
    /// Adds multiple nodes to the tree.
    /// </summary>
    /// <param name="nodes">The node labels.</param>
    public void AddNodes(params string[] nodes)
    {
        UpdateContentBuffer();
        foreach (var node in nodes)
        {
            contentBuffer!.Nodes.Add(new TreeNode(new Markup(node)));
        }
        SwapContentBuffer();
    }

    /// <summary>
    /// Adds multiple nodes to the tree.
    /// </summary>
    /// <param name="nodes">The node renderables.</param>
    public void AddNodes(params IRenderable[] nodes)
    {
        UpdateContent(c =>
        {
            foreach (var node in nodes)
            {
                c.Nodes.Add(new TreeNode(node));
            }
        });        
    }

    protected override void UpdateContentBuffer()
    {                               
        contentBuffer.Style = Content.Style;
        contentBuffer.Guide = Content.Guide;
        contentBuffer.Expanded = Content.Expanded;        
        contentBuffer.Nodes.Clear();
        foreach (var child in Content.Nodes)
        {
            // For Tree, we must deep copy nodes because TreeNode is mutable.
            contentBuffer.Nodes.Add(CloneNode(child));
        }
    }

    /// <inheritdoc/>
    protected override Spectre.Console.Tree CloneContent()
    {
        if (_renderableProp == null) throw new InvalidOperationException("Could not find 'Renderable' property on TreeNode.");
        
        var newTree = new Spectre.Console.Tree(_rootLabel);
        newTree.Style = Content.Style;
        newTree.Guide = Content.Guide;
        newTree.Expanded = Content.Expanded;

        foreach (var child in Content.Nodes)
        {
            newTree.Nodes.Add(CloneNode(child));
        }

        return newTree;
    }

    private TreeNode CloneNode(TreeNode original)
    {
        var renderable = (IRenderable?)_renderableProp!.GetValue(original); 
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
