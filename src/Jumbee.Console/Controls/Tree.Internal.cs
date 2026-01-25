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
        internal TreeNode(Tree tree, uint id, IRenderable label, TreeNode? parent = null)
        {
            Tree = tree;
            Id = id;
            Label = label;
            Parent = parent;
        }
        #endregion

        #region Properties
        public Tree Tree { get; protected set; }

        public TreeNode? Parent { get; protected set; }

        public uint Id { get; }

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

        internal IRenderable Renderable => Label;

        internal ICollection<TreeNode> Nodes => _children.Values;
        #endregion

        #region Indexers
        public TreeNode? this[uint id] => _children.TryGetValue(id, out var node) ? node : null;
        #endregion
        
        #region Methods
        public TreeNode AddChild(IRenderable label)
        {
            TreeNode c;
            bool complete = false;
            do
            {
                c = new TreeNode(this.Tree, Interlocked.Increment(ref childIndex), label, this);
                complete = this._children.TryAdd(c.Id, c);
            }
            while (!complete);
            UpdateTree();
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
            if (!IsRemoved) Tree.UpdateNodes();
        }
        #endregion

        #region Fields
        private ConcurrentDictionary<uint, TreeNode> _children = new();
        private uint childIndex = 0;
        #endregion
    }
}