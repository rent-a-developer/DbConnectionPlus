using System.Globalization;

namespace RentADeveloper.DbConnectionPlus.Benchmarks.TestData;

// Generates the entities the benchmarks operate on. A plain seeded generator rather than the unit test
// project's AutoFixture based Generate, for two reasons.
//
// The first is Native AOT. Every assembly this project references is compiled into the Native AOT job's
// closure, and AutoFixture, Bogus and Mapster all resolve members reflectively. Trimming would be free to
// remove BenchmarkEntity's properties, because nothing but reflection reaches them, and reflection over a
// trimmed type returns fewer members with no error at all - the setup would quietly seed default valued
// entities and the benchmarks would happily measure that. It is the same failure mode the library defends
// against with its annotations and its zero-binding guard: measured at 6 columns of real data in, 0 bound, no
// exception.
//
// The second reason applies to the JIT job just as much: the generator is seeded, so every process produces
// the same entities. BenchmarkDotNet runs each job in its own process, so with an unseeded generator the two
// jobs would be measured against different data and their means would not be comparable.
//
// The value ranges mirror the unit test project's, because the benchmark schema stores several of these types
// as TEXT and they have to round trip: seconds precision for DateTime, ten fractional digits for Decimal,
// three for Double and Single, and alphabetic characters only for Char.
public static class Generate
{
    public static BenchmarkEntity Single() =>
        Create(NextId());

    public static List<BenchmarkEntity> Multiple(int numberOfEntities) =>
        [.. Enumerable.Range(0, numberOfEntities).Select(_ => Single())];

    public static BenchmarkEntity UpdateFor(BenchmarkEntity entity)
    {
        var updatedEntity = Create(entity.Id);

        // For the rare case that all generated values are the same as in the original entity, regenerate until at
        // least one value is different.
        while (updatedEntity == entity)
        {
            updatedEntity = Create(entity.Id);
        }

        return updatedEntity;
    }

    public static List<BenchmarkEntity> UpdatesFor(List<BenchmarkEntity> entities) =>
        [.. entities.Select(UpdateFor)];

    private static BenchmarkEntity Create(long id)
    {
        lock (syncRoot)
        {
            return new()
            {
                Id = id,
                BooleanValue = random.Next(2) == 1,
                BytesValue = NextBytes(random.Next(1, 10)),
                ByteValue = (byte)random.Next(0, 256),
                CharValue = characters[random.Next(0, characters.Length)],
                // Seconds precision, and a fixed base date so that the values do not depend on when the benchmarks
                // are run. The span covers roughly six years.
                DateTimeValue = dateTimeBase.AddSeconds(random.Next(0, 200_000_000)),
                DecimalValue = Math.Round((decimal)(random.NextDouble() * 999.0), 10),
                DoubleValue = Math.Round(random.NextDouble() * 999.0, 3),
                EnumValue = (TestEnum)random.Next(1, 6),
                Int16Value = (short)random.Next(short.MinValue, short.MaxValue + 1),
                Int32Value = random.Next(int.MinValue, int.MaxValue),
                Int64Value = random.NextInt64(),
                SingleValue = (float)Math.Round(random.NextDouble() * 999.0, 3),
                StringValue = NextSentence()
            };
        }
    }

    private static long NextId() =>
        Interlocked.Increment(ref nextId);

    private static byte[] NextBytes(int count)
    {
        var bytes = new byte[count];

        random.NextBytes(bytes);

        return bytes;
    }

    private static string NextSentence()
    {
        var wordCount = random.Next(4, 9);
        var sentence = new string[wordCount];

        for (var i = 0; i < wordCount; i++)
        {
            sentence[i] = words[random.Next(0, words.Length)];
        }

        sentence[0] = string.Concat(
            sentence[0][..1].ToUpper(CultureInfo.InvariantCulture),
            sentence[0].AsSpan(1)
        );

        return string.Join(' ', sentence) + '.';
    }

    private static long nextId;

    // Seeded, so that every process generates the same entities.
    private static readonly Random random = new(20260813);

    // Guards random. The setup is single threaded today, but a shared unsynchronized Random silently starts
    // returning zeroes once it is not, which would be invisible in a benchmark result.
    private static readonly Lock syncRoot = new();

    private static readonly char[] characters = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz".ToCharArray();

    private static readonly string[] words =
    [
        "lorem", "ipsum", "dolor", "sit", "amet", "consectetur", "adipiscing", "elit", "sed", "do", "eiusmod",
        "tempor", "incididunt", "ut", "labore", "et", "dolore", "magna", "aliqua", "enim", "ad", "minim", "veniam",
        "quis", "nostrud", "exercitation", "ullamco", "laboris", "nisi", "aliquip", "ex", "ea", "commodo"
    ];

    private static readonly DateTime dateTimeBase = new(2020, 1, 1, 0, 0, 0, DateTimeKind.Local);
}
