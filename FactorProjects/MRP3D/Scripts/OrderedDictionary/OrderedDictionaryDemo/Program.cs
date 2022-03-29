using System;
using OD;

namespace OrderedDictionaryDemo
{
	class Program
	{
		static void Main(string[] args)
		{
			var od = new OrderedDictionary<string, string>();
			Console.WriteLine("Add(\"foo\",\"bar\")");
			Console.WriteLine();
			od.Add("foo","bar");
			Console.WriteLine("Add(\"bar\",\"baz\")");
			Console.WriteLine();
			od.Add("bar", "baz");
			Console.WriteLine("Add(\"foobar\",\"foobaz\")");
			Console.WriteLine();
			od.Add("foobar", "foobaz");
			Console.WriteLine("Enumerating {0} Items:",od.Count);
			foreach (var item in od)
				Console.WriteLine("  {0}: {1}", item.Key, item.Value);
			Console.WriteLine();
			Console.WriteLine("SetAt(1,\"zab\")");
			od.SetAt(1, "zab");
			Console.WriteLine();
			Console.WriteLine("GetAt(1): {0}",od.GetAt(1));
			Console.WriteLine();
			Console.WriteLine("Insert(1,\"zaboof\",\"baz\") and Enumerating {0} items:",od.Count+1);
			od.Insert(1, "zaboof", "baz");
			foreach (var item in od)
				Console.WriteLine("  {0}: {1}", item.Key, item.Value);
			Console.WriteLine();
			Console.WriteLine("RemoveAt(1) and Enumerating {0} items:",od.Count-1);
			od.RemoveAt(1);
			foreach (var item in od)
				Console.WriteLine("  {0}: {1}", item.Key, item.Value);
			Console.WriteLine();
			Console.WriteLine();
		}
	}
}
