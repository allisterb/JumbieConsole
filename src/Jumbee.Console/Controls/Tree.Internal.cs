namespace Jumbee.Console;

using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;

using Spectre.Console.Rendering;
using Spectre.Console;

public partial class Tree
{
    /// <summary>
    /// Represents a tree node.
    /// </summary>
    /// <remarks>Based on <see cref="Spectre.Console.TreeNode"/> but modified to have a mutable label and concurrent child nodes collection.</remarks>
    public class TreeNode
    {
        #region Constructors
        /// <summary>
        /// Initializes a new <see cref="TreeNode"/> instance.
        /// </summary>
        /// <param name="renderable">The tree node label.</param>
        internal TreeNode(Tree tree, uint index, IRenderable label, TreeNode? parent = null, string? text = null)
        {
            Tree = tree;
            Index = index;
            Label = label;
            Parent = parent;
            Text = text;
        }

        internal TreeNode(Tree tree, uint index, string label, TreeNode? parent = null) : this(tree, index, new Markup(label), parent) {}
        #endregion

        #region Properties
        public Tree Tree { get; protected set; }

        public TreeNode? Parent { get; protected set; }

        public uint Index { get; }

        public string? Text { get; internal set; }

        public IRenderable Label
        {
            get => field;
            set
            {
                field = value;
                UpdateTree();
            }
        }

        /// <summary>
        /// Gets the tree node's child nodes.
        /// </summary>
        public ICollection<TreeNode> Children => _children.Values;

        /// <summary>
        /// Gets or sets a value indicating whether or not the tree node is expanded or not.
        /// </summary>
        public bool Expanded { get; set; } = true;

        public bool IsRemoved { get; internal set; } = false;

        /// <summary>
        /// Gets or sets a value indicating whether the node is selected.
        /// </summary>
        public bool Selected
        {
            get => _selected;
            set
            {
                if (_selected != value)
                {
                    _selected = value;
                    UpdateTree();
                }
            }
        }

        internal IRenderable Renderable => Label;

        internal ICollection<TreeNode> Nodes => _children.Values;
        #endregion

        #region Indexers
        public TreeNode? this[uint id] => _children.TryGetValue(id, out var node) ? node : null;
        #endregion
        
        #region Methods
        public TreeNode AddChild(IRenderable label, string? text = null)
        {
            TreeNode c;
            bool complete = false;
            do
            {
                c = new TreeNode(this.Tree, Interlocked.Increment(ref childIndex), label, this, text);
                complete = this._children.TryAdd(c.Index, c);
            }
            while (!complete);
            UpdateTree();
            return c;
        }

        public TreeNode AddChild(string label) => AddChild(new Markup(label), label);

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

        public bool RemoveChild(uint id)
        {
            if (_children.TryRemove(id, out var c))
            {
                c.Parent = null;
                c.IsRemoved = true;
                UpdateTree();
                return true;
            }
            else
            {
                return false;
            }
        }

        protected void UpdateTree()
        {
            if (!IsRemoved) Tree.Update();
        }
        #endregion

        #region Fields
        private ConcurrentDictionary<uint, TreeNode> _children = new();
        private uint childIndex = 0;
        private bool _selected;
        #endregion
    }
}