using System;
using System.Collections.Generic;
using System.Linq;

public class Trie
{
    public class Node
    {
        public int Count;
        public Dictionary<char, Node> Edges;
        public bool IsTerm;

        public Node(bool isTerminal)
        {
            if (!isTerminal)
                Edges = new Dictionary<char, Node>();
        }

        public IEnumerable<KeyValuePair<string, int>> GetTerms(string current)
        {
            if (IsTerm)
            {
                var childCount = Edges == null ? 0 : Edges.Sum(kvp => kvp.Value.Count);
                yield return new KeyValuePair<string, int>(current, Count - childCount);
            }

            if (Edges == null) yield break;
            foreach (var result in Edges.SelectMany(kvp => kvp.Value.GetTerms(current + kvp.Key)))
            {
                yield return result;
            }
        }
    }

    public Node Root = new Node(true);

    public void AddTerm(string term)
    {
        var current = Root;

        for (var i = 0; i < term.Length; i++)
        {
            var c = term[i];
            var isLastChar = i == term.Length - 1;

            if (current.Edges == null)
            {
                current.Edges = new Dictionary<char, Node>
                {
                  {c, new Node(isLastChar)}
                };
            }

            else if (!current.Edges.ContainsKey(c))
            {
                current.Edges[c] = new Node(isLastChar);
            }
            current.Count++;
            current = current.Edges[c];
        }

        current.IsTerm = true;
        current.Count++;
    }

    public IEnumerable<KeyValuePair<string, int>> GetTerms(long cutoff = 10L)
    {
        return GetAllTerms()
            .Where(kvp => kvp.Value >= cutoff)
            .OrderByDescending(kvp => kvp.Value);
    }

    private IEnumerable<KeyValuePair<string, int>> GetAllTerms()
    {
        return Root.GetTerms("");
    }


    //public Trie(string[] words)
    //{
    //    for (int w = 0; w < words.Length; w++)
    //    {
    //        var word = words[w];
    //        var node = Root;
    //        for (int len = 1; len <= word.Length; len++)
    //        {
    //            var letter = word[len - 1];
    //            Node next;
    //            if (!node.Edges.TryGetValue(letter, out next))
    //            {
    //                next = new Node();
    //                if (len == word.Length)
    //                {
    //                    next.Word = word;
    //                }
    //                node.Edges.Add(letter, next);
    //            }
    //            node = next;
    //        }
    //    }
    //}
}
