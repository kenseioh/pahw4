using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WebRole1
{
    public class TrieNode
    {
        public char c;
        public TrieNode parent;
        public Dictionary<Char, TrieNode> children = new Dictionary<char, TrieNode>();
        public Boolean isLeaf;

        public TrieNode() { }
        public TrieNode(char c) { this.c = c; }
        public Dictionary<Char, TrieNode> getChildren()
        {
            return children;
        }



    }
}