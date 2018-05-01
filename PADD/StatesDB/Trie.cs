using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PADD.StatesDB
{
	/// <summary>
	/// A trie that allows to search by characters.
	/// </summary>
	public class Trie<LeafElement>
		where LeafElement : IEquatable<LeafElement>
	{
		private TrieNode root;

		protected class TrieNode
		{
			public bool hasElement { get; private set; }
			public LeafElement element { get; private set; }
			public Dictionary<char, TrieNode> successors;

			public TrieNode()
			{
				this.hasElement = false;
				this.successors = new Dictionary<char, TrieNode>();
				this.element = default;
			}

			public void setElement(LeafElement element)
			{
				if (this.hasElement && !this.element.Equals(element))
					throw new Exception();
				this.hasElement = true;
				this.element = element;
			}

			public void addSuccessor(char key)
			{
				if (this.successors.ContainsKey(key))
					throw new Exception();
				successors.Add(key, new TrieNode());
			}

			public bool hasSuccessor(char key)
			{
				return successors.ContainsKey(key);
			}

			public TrieNode getSuccessor(char key)
			{
				return successors[key];
			}

			public void writeToStream(System.IO.StreamWriter writer)
			{
				writer.Write((this.hasElement ? this.element.ToString() : "") + "{" + this.successors.Keys.Count + ";");
				foreach (var item in this.successors.Keys)
				{
					writer.Write(item);
					this.getSuccessor(item).writeToStream(writer);
					writer.Write("|");
				}
				writer.Write("}");
			}

			public static TrieNode loadFromStream(System.IO.StreamReader reader, Func<string, LeafElement> parser)
			{
				TrieNode result = new TrieNode();
				var elementString = readTillDelimiter(reader, '{');
				LeafElement element = default;
				if (elementString != "")
				{
					element = parser.Invoke(elementString);
					result.hasElement = true;
				}
				result.element = element;
				var delim = (char)reader.Read(); //skipes the opening bracket
				var countString = readTillDelimiter(reader, ';');
				int count = (countString.Length > 0 ? int.Parse(countString) : 0);
				delim = (char)reader.Read(); //skipes the ;
				for (int i = 0; i < count; i++)
				{
					char key = (char)reader.Read();
					TrieNode successor = loadFromStream(reader, parser);
					result.successors.Add(key, successor);
					delim = (char)reader.Read();    //skipes the mid
				}
				delim = (char)reader.Read();    //skipes the closing bracket
				return result;
			}

			/// <summary>
			/// Reads characters from the reader until a specified delimiter is found. The delimiter character is NOT consumed from the reader.
			/// </summary>
			/// <param name="reader"></param>
			/// <param name="delimiter"></param>
			/// <returns></returns>
			protected static string readTillDelimiter(System.IO.StreamReader reader, params char[] delimiters)
			{
				StringBuilder b = new StringBuilder();
				char c = (char)reader.Peek();
				while(!delimiters.Contains(c))
				{
					b.Append(c);
					reader.Read();
					c = (char)reader.Peek();
				}
				return b.ToString();
			}
		}

		public void add(string key, LeafElement value)
		{
			TrieNode currentNode = root;
			foreach (var item in key)
			{
				if (!currentNode.hasSuccessor(item))
					currentNode.addSuccessor(item);
				currentNode = currentNode.getSuccessor(item);
			}
			currentNode.setElement(value);
		}

		/// <summary>
		/// Searches for an element the corresponds to the given key.
		/// </summary>
		/// <returns>True if lement was found or false otherwise</returns>
		public bool tryGetElement(string key, out LeafElement value)
		{
			TrieNode currentNode = root;
			foreach (var item in key)
			{
				if (!currentNode.hasSuccessor(item))
				{
					value = default;
					return false;
				}
				currentNode = currentNode.getSuccessor(item);
			}
			if (currentNode.hasElement)
			{
				value = currentNode.element;
				return true;
			}
			value = default;
			return false;
		}

		public IEnumerable<(string key, LeafElement value)> getAllElements()
		{
			StringBuilder sb = new StringBuilder();
			return getElementsRecur(sb, root);
		}

		protected IEnumerable<(string key, LeafElement value)> getElementsRecur(StringBuilder builder, TrieNode currentNode)
		{
			if (currentNode.hasElement)
			{
				yield return (builder.ToString(), currentNode.element);
			}
			foreach (var item in currentNode.successors.Keys)
			{
				builder.Append(item);
				foreach (var res in getElementsRecur(builder, currentNode.getSuccessor(item)))
				{
					yield return res;
				}
				builder.Remove(builder.Length - 1, 1);
			}
		}

		public Trie()
		{
			this.root = new TrieNode();
		}

		public void store(string filePath)
		{
			var writter = new System.IO.StreamWriter(filePath);
			root.writeToStream(writter);
			writter.Close();
		}

		public static Trie<LeafElement> load(string filePath, Func<string, LeafElement> parser)
		{
			Trie<LeafElement> result = new Trie<LeafElement>();
			var path = System.IO.Path.GetFullPath(filePath);
			var reader = new System.IO.StreamReader(path);
			result.root = TrieNode.loadFromStream(new System.IO.StreamReader(path), parser);
			reader.Close();
			return result;
		}
	}

}
