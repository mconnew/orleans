using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Orleans.Utilities;

namespace Orleans.Runtime
{
    public class TypeConverter
    {
        private readonly ITypeConverter[] _converters;
        private readonly ClrTypeConverter _defaultFormatter;
        private readonly ConcurrentDictionary<string, Type> _parsed = new ConcurrentDictionary<string, Type>();
        private readonly ConcurrentDictionary<Type, string> _formatted = new ConcurrentDictionary<Type, string>();
        private readonly Func<Type, string> _formatFunc;
        private readonly Func<string, Type> _parseFunc;
        private readonly Func<QualifiedType, QualifiedType> _convertToDisplayName;
        private readonly Func<QualifiedType, QualifiedType> _convertFromDisplayName;

        public TypeConverter(IEnumerable<ITypeConverter> formatters)
        {
            _converters = formatters.ToArray();
            _defaultFormatter = new ClrTypeConverter();
            _parseFunc = ParseInternal;
            _formatFunc = FormatInternal;
            _convertToDisplayName = ConvertToDisplayName;
            _convertFromDisplayName = ConvertFromDisplayName;
        }

        public string Format(Type type) => _formatted.GetOrAdd(type, _formatFunc);

        public Type Parse(string formatted) => _parsed.GetOrAdd(formatted, _parseFunc);

        private string FormatInternal(Type type)
        {
            string runtimeType = null;
            foreach (var converter in _converters)
            {
                if (converter.TryFormat(type, out var value))
                {
                    runtimeType = value;
                    break;
                }
            }

            if (string.IsNullOrWhiteSpace(runtimeType))
            {
                runtimeType = _defaultFormatter.Format(type);
            }

            var runtimeTypeSpec = RuntimeTypeNameParser.Parse(runtimeType);
            var displayTypeSpec = RuntimeTypeNameRewriter.Rewrite(runtimeTypeSpec, _convertToDisplayName);
            var formatted = displayTypeSpec.Format();

            return formatted;
        }

        private Type ParseInternal(string formatted)
        {
            var parsed = RuntimeTypeNameParser.Parse(formatted);
            var runtimeTypeSpec = RuntimeTypeNameRewriter.Rewrite(parsed, _convertFromDisplayName);
            var runtimeType = runtimeTypeSpec.Format();

            foreach (var converter in _converters)
            {
                if (converter.TryParse(runtimeType, out var result))
                {
                    return result;
                }
            }

            return _defaultFormatter.Parse(runtimeType);
        }

        private QualifiedType ConvertToDisplayName(QualifiedType input)
        {
            return input;
        }

        private QualifiedType ConvertFromDisplayName(QualifiedType input)
        {
            return input;
        }
    }

    public static class TypeConverterExtensions
    {
        private const char GenericTypeIndicator = '`';
        private const char StartArgument = '[';

        public static bool IsGenericType(string type)
        {
            if (string.IsNullOrWhiteSpace(type)) return false;
            return type.IndexOf(GenericTypeIndicator) >= 0;
        }

        public static bool IsConstructed(string type)
        {
            if (string.IsNullOrWhiteSpace(type)) return false;
            var index = type.IndexOf(StartArgument);
            return index > 0;
        }

        public static string GetDeconstructed(string type)
        {
            var index = type.IndexOf(StartArgument);

            if (index <= 0)
            {
                return type;
            }

            return type.Substring(0, index);
        }

        public static string GetConstructed(this TypeConverter formatter, string unconstructed, params Type[] typeArguments)
        {
            var typeString = unconstructed;
            var indicatorIndex = typeString.IndexOf(GenericTypeIndicator);
            var argumentsIndex = typeString.IndexOf(StartArgument, indicatorIndex);
            if (argumentsIndex >= 0)
            {
                throw new InvalidOperationException("Cannot construct an already-constructed type");
            }

            var arityString = typeString.Substring(indicatorIndex + 1);
            var arity = int.Parse(arityString);
            if (typeArguments.Length != arity)
            {
                throw new InvalidOperationException($"Insufficient number of type arguments, {typeArguments.Length}, provided while constructing type \"{unconstructed}\" of arity {arity}");
            }

            var typeSpecs = new TypeSpec[typeArguments.Length];
            for (var i = 0; i < typeArguments.Length; i++)
            {
                typeSpecs[i] = RuntimeTypeNameParser.Parse(formatter.Format(typeArguments[i]));
            }

            var constructed = new ConstructedGenericTypeSpec(new NamedTypeSpec(null, typeString, typeArguments.Length), typeSpecs).Format();
            return constructed;
        }

        public static Type[] GetArguments(this TypeConverter formatter, string constructed)
        {
            var str = constructed;
            var index = str.IndexOf(StartArgument);
            if (index <= 0)
            {
                return Array.Empty<Type>();
            }

            var safeString = "safer" + str.Substring(str.IndexOf(GenericTypeIndicator));
            var parsed = RuntimeTypeNameParser.Parse(safeString);
            if (!(parsed is ConstructedGenericTypeSpec spec))
            {
                throw new InvalidOperationException($"Unable to correctly parse grain type {str}");
            }

            var result = new Type[spec.Arguments.Length];
            for (var i = 0; i < result.Length; i++)
            {
                var arg = spec.Arguments[i];
                var formattedArg = arg.Format();
                result[i] = formatter.Parse(formattedArg);
                if (result[i] is null)
                {
                    throw new InvalidOperationException($"Unable to parse argument \"{formattedArg}\" as a type for grain type \"{str}\"");
                }
            }

            return result;
        }
    }

    internal class ClrTypeConverter
    {
        private readonly CachedTypeResolver _resolver = new CachedTypeResolver();

        public string Format(Type type) => RuntimeTypeNameFormatter.Format(type);

        public Type Parse(string formatted) => _resolver.ResolveType(formatted);
    }

    public interface ITypeConverter
    {
        bool TryFormat(Type type, out string formatted);
        bool TryParse(string formatted, out Type type);
    }
}
