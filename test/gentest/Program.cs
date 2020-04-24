using System;
using System.Collections.Generic;
using Orleans.Utilities;
namespace gentest
{
    class Program
    {
        static void Main(string[] args)
        {
            Tests();
            // var typeString = RuntimeTypeNameFormatter.Format(typeof(List<int[]>[]));
            //var typeString = "System.Collections.Generic.List`1[[System.Int32[]]][]";
            //var typeString = RuntimeTypeNameFormatter.Format(typeof(int[][,,][]));

            var input = "gentest.Program+Inner`1+InnerInner`2+Bottom[[System.Int32,CoreFx],[System.String,System],[System.Collections.Generic.List`1[[System.Int32,System.Private.CoreLib]]]],gentest";
            var result = RuntimeTypeNameParser.Parse(input);
            var rewritten = RuntimeTypeNameRewriter.Rewrite(result, input => input switch
            {
                (_, "System.Int32") => (null, "int"),
                (_, "System.String") => (null, "string"),
                (_, "gentest.Program+Inner`1+InnerInner`2+Bottom") p => (p.Assembly, "LaDiDa`3"),
                (_, "System.Collections.Generic.List`1") => (null, "List`1"),
                _ => input
            });
            var reparsed = RuntimeTypeNameParser.Parse(rewritten.Format());
            if (!string.Equals(reparsed.Format(), rewritten.Format())) throw new Exception("no");
            Console.WriteLine(rewritten); // outputs: LaDiDa`3[[int],[string],[List`1[[int]]]],gentest

            Console.WriteLine("=====");
            Console.WriteLine($"Input:  {result}");
            Console.WriteLine($"Output: {rewritten}");
            Console.WriteLine("=====");
            Print(rewritten, 0);

            /*
            GenericGrainType.TryParse(GrainType.Create("foo`2"), out var g);
            var f = new GenericGrainTypeFormatter(new IGenericGrainTypeFormatter[] { new KnownTypeFormatter() });
            Console.WriteLine(g);D
            var g2 = g.Construct(f, typeof(int), typeof(string));
            Console.WriteLine(g2);
            var t = g2.GetArguments(f);
            Console.WriteLine(t.ToString());

            var g3 = g.Construct(f, typeof(int[]), typeof(Dictionary<int, string>));
            var t2 = g3.GetArguments(f);
            Console.WriteLine(t2[0]);
            Console.WriteLine(g3);
            */
        }

        public static void Print(TypeSpec type, int depth)
        {
            var d = new string(' ', 2 * depth);
            switch (type)
            {
                case AssemblyQualifiedTypeSpec a:
                    Console.WriteLine(d + $"Asm: (Spec: {a.Assembly})");
                    Print(a.Type, depth + 1);
                    break;
                case NamedTypeSpec a:
                    Console.WriteLine(d + $"Named: (Name: {a.Name}, Arity: {a.Arity})");
                    Console.WriteLine(d + $"NamespaceQualifiedName: {a.GetNamespaceQualifiedName()}");
                    if (a.ContainingType is object)
                    {
                        Console.WriteLine(d + $"Parent:");
                        Print(a.ContainingType, depth + 1);
                    }
                    break;
                case PointerTypeSpec a:
                    Console.WriteLine(d + $"Pointer:");
                    Print(a.ElementType, depth + 1);
                    break;
                case ReferenceTypeSpec a:
                    Console.WriteLine(d + $"Reference:");
                    Print(a.ElementType, depth + 1);
                    break;
                case ConstructedGenericTypeSpec a:
                    Console.WriteLine(d + $"Constructed Generic:");
                    Print(a.UnconstructedType, depth + 1);
                    Console.WriteLine(d + $"Arguments: ");
                    foreach (var arg in a.Arguments) Print(arg, depth + 1);
                    break;
                case ArrayTypeSpec a:
                    Console.WriteLine(d + $"Array(Dimensions: {a.Dimensions}):");
                    Print(a.ElementType, depth + 1);
                    break;
                case null:
                    Console.WriteLine(d + "null");
                    break;
                default:
                    throw new NotSupportedException($"Type {type.GetType()} is not supported");
            }
        }

        public static void Tests()
        {
            var types = new[]
            {
                typeof(int),
                typeof(int[]),
                typeof(int*[]),
                typeof(int[]),
                typeof(List<>),
                typeof(List<int>),
                typeof(List<int*[]>),
                typeof(Inner<int[,,]>.InnerInner<string, List<int>>.Bottom[,]),
                typeof(Inner<>.InnerInner<,>.Bottom),
                typeof(Program),
                typeof(int).MakeByRefType(),
                typeof(Inner<int[]>.InnerInner<string, List<int>>.Bottom[,])
                    .MakePointerType()
                    .MakePointerType()
                    .MakeArrayType(10)
                    .MakeByRefType(),
            };

            foreach (var type in types)
            {
                var formatted = RuntimeTypeNameFormatter.Format(type);
                Console.WriteLine($"Full Name: {type.FullName}");
                Console.WriteLine($"Formatted: {formatted}");
                var parsed = RuntimeTypeNameParser.Parse(formatted);
                Console.WriteLine($"Parsed   : {parsed}");
            }
        }

        public class Inner<T>
        {
            public class InnerInner<U, V>
            {
                public class Bottom { }
            }
        }
    }
}
