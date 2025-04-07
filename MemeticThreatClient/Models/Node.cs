using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MemeticThreatClient.Models
{
    //
    internal class Node<T>
    {
        public T Value { get; set; }

        public ObservableCollection<Node<T>> Children { get; private set; }
        public Node() 
        {
            this.Children = new ObservableCollection<Node<T>>();
        }
        public Node(T value) : this()
        {
            this.Value = value;
        }

        public void AddChild(Node<T> node)
            => this.Children.Add(node);
        public void RemoveChildAt(int Index)
            => this.Children.RemoveAt(Index);
        public void RemoveChild(Node<T> node)
            => this.Children.Remove(node);
        public Node<T> GetChild(int Index)
            => this.Children[Index];
        public bool IsLeaf()
            => this.Children.Count == 0;
        public Node<T> ChildWithValue(T childValue)
        {
            EqualityComparer<T> @default = EqualityComparer<T>.Default;
            foreach (Node<T> node in Children)
            {
                if (@default.Equals(node.Value, childValue))
                    return node;
            }
            return null;
        }
    }
}
