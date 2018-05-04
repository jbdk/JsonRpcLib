﻿namespace SpanJson.Formatters
{
    /// <summary>
    ///     Used for types which are not built-in
    /// </summary>
    public sealed class ComplexStructFormatter<T, TSymbol, TResolver> : ComplexFormatter, IJsonFormatter<T, TSymbol, TResolver>
        where T : struct where TResolver : IJsonFormatterResolver<TSymbol, TResolver>, new() where TSymbol : struct
    {
        public static readonly ComplexStructFormatter<T, TSymbol, TResolver>
            Default = new ComplexStructFormatter<T, TSymbol, TResolver>();

        private static readonly DeserializeDelegate<T, TSymbol, TResolver> Deserializer =
            BuildDeserializeDelegate<T, TSymbol, TResolver>();

        private static readonly SerializeDelegate<T, TSymbol, TResolver> Serializer = BuildSerializeDelegate<T, TSymbol, TResolver>();

        public T Deserialize(ref JsonReader<TSymbol> reader)
        {
            return Deserializer(ref reader);
        }

        public void Serialize(ref JsonWriter<TSymbol> writer, T value, int nestingLimit)
        {
            Serializer(ref writer, value, nestingLimit);
        }
    }
}