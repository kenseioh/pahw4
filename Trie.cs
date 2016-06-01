using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WebRole1
{
    public class Trie
    {
        public static TrieNode root;
        public static List<string> words;
        public static TrieNode prefixRoot;
        public static String curPrefix;

        public Trie()
        {
            root = new TrieNode();
            words = new List<string>();
        }

        // Inserts a word into the trie.
        public void insert(String word)
        {
            Dictionary<Char, TrieNode> children = root.children;

            TrieNode crntparent;

            crntparent = root;

            //cur children parent = root

            for (int i = 0; i < word.Length; i++)
            {
                char c = word[i];

                TrieNode t;
                if (children.ContainsKey(c)) { t = children[c]; }
                else
                {
                    t = new TrieNode(c);
                    t.parent = crntparent;
                    children.Add(c, t);
                }

                children = t.children;
                crntparent = t;

                //set leaf node
                if (i == word.Length - 1)
                    t.isLeaf = true;
            }
        }

        // Returns if the word is in the trie.
        public Boolean search(String word)
        {
            TrieNode t = searchNode(word);
            if (t != null && t.isLeaf) { return true; }
            else { return false; }
        }

        // Returns if there is any word in the trie
        // that starts with the given prefix.
        public Boolean startsWith(String prefix)
        {
            if (searchNode(prefix) == null) { return false; }
            else { return true; }
        }

        public TrieNode searchNode(String str)
        {
            Dictionary<Char, TrieNode> children = root.children;
            TrieNode t = null;
            for (int i = 0; i < str.Length; i++)
            {
                char c = str[i];
                if (children.ContainsKey(c))
                {
                    t = children[c];
                    children = t.children;
                }
                else { return null; }
            }

            prefixRoot = t;
            curPrefix = str;
            words.Clear();
            return t;
        }

        public void wordsFinderTraversal(TrieNode node, int offset)
        {
            //  print(node, offset);

            if (node.isLeaf == true)
            {
                //println("leaf node found");

                TrieNode altair;
                altair = node;

                Stack<String> hstack = new Stack<String>();

                while (altair != prefixRoot)
                {
                    //println(altair.c);
                    hstack.Push(altair.c.ToString());
                    altair = altair.parent;
                }

                String wrd = curPrefix;

                while (hstack.Count() > 0)
                {
                    wrd = wrd + hstack.Pop();
                }

                //println(wrd);
                words.Add(wrd);

            }

            List<char> keyList = new List<char>(node.children.Keys);

            List<char> aloc = new List<char>();

            for (int i = 0; i < keyList.Count; i++)
            {
                Char ch = keyList[i];
                aloc.Add(ch);
            }

            // here you can play with the order of the children

            for (int i = 0; i < aloc.Count; i++)
            {
                if (words.Count <= 9)
                {
                    wordsFinderTraversal(node.children[(char)aloc[i]], offset + 2);
                }
            }

        }


        public List<string> displayFoundWords()
        {
            return words;
        }
    }
}