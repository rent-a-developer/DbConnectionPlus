// ReSharper disable ConvertToLambdaExpression
// ReSharper disable RedundantTypeArgumentsOfMethod

#pragma warning disable IDE0053

using AutoFixture;
using Bogus;
using Mapster;
using RentADeveloper.DbConnectionPlus.Entities;

namespace RentADeveloper.DbConnectionPlus.UnitTests.TestData;

/// <summary>
/// Generates test data.
/// </summary>
public static class Generate
{
    /// <summary>
    /// Initializes the <see cref="Generate" /> class.
    /// </summary>
    static Generate()
    {
        faker = new();
        fixture = new();

        fixture.Register<Boolean>(() => faker.Random.Bool());
        fixture.Register<Byte>(() => faker.Random.Byte());
        fixture.Register<Byte[]>(() => faker.Random.Bytes(SmallNumber()));
        fixture.Register<Char>(() => characters[faker.Random.Int(0, characters.Length - 1)]);
        fixture.Register<DateOnly>(() => faker.Date.PastDateOnly());
        fixture.Register<DateTime>(() =>
            {
                var dateTime = faker.Date.Past();

                // We limit to seconds precision because not all database systems support a higher precision.
                return new(
                    dateTime.Year,
                    dateTime.Month,
                    dateTime.Day,
                    dateTime.Hour,
                    dateTime.Minute,
                    dateTime.Second,
                    DateTimeKind.Local
                );
            }
        );
        fixture.Register<DateTimeOffset>(() =>
            {
                var dateTimeOffset = faker.Date.PastOffset();

                // We limit to seconds precision because not all database systems support a higher precision.
                return new(
                    dateTimeOffset.Year,
                    dateTimeOffset.Month,
                    dateTimeOffset.Day,
                    dateTimeOffset.Hour,
                    dateTimeOffset.Minute,
                    dateTimeOffset.Second,
                    dateTimeOffset.Offset
                );
            }
        );
        fixture.Register<Decimal>(() =>
            {
                // We limit to 10 fractional digits because not all database systems support a higher precision.
                return Math.Round(faker.Random.Decimal(0, 999), 10);
            }
        );
        fixture.Register<Double>(() =>
            {
                // We limit to 3 fractional digits because not all database systems support a higher precision.
                return Math.Round(faker.Random.Double(0, 999), 3);
            }
        );
        fixture.Register<Guid>(() => faker.Random.Guid());
        fixture.Register<Int16>(() => faker.Random.Short());
        fixture.Register<Int32>(() => faker.Random.Int());
        fixture.Register<Int64>(() => Interlocked.Increment(ref entityId));
        fixture.Register<Single>(() =>
            {
                // We limit to 3 fractional digits because not all database systems support a higher precision.
                return (Single)Math.Round(faker.Random.Float(0, 999), 3);
            }
        );
        fixture.Register<String>(() => faker.Lorem.Sentence());
        fixture.Register<TestEnum>(() => faker.Random.Enum<TestEnum>());
        fixture.Register<TimeOnly>(() =>
            {
                var timeOnly = faker.Date.RecentTimeOnly();

                // We limit to seconds precision because not all database systems support a higher precision.
                return new(timeOnly.Hour, timeOnly.Minute, timeOnly.Second);
            }
        );
        fixture.Register<TimeSpan>(() =>
            {
                var timeSpan = faker.Date.Timespan(new TimeSpan(0, 23, 59, 59));

                // We limit to seconds precision because not all database systems support a higher precision.
                return new(timeSpan.Hours, timeSpan.Minutes, timeSpan.Seconds);
            }
        );

        var cancellationTokenSource = new CancellationTokenSource();
        fixture.Register<CancellationToken>(() => cancellationTokenSource.Token);

        TypeAdapterConfig<Entity, EntityWithDifferentCasingProperties>
            .NewConfig()
            .NameMatchingStrategy(NameMatchingStrategy.IgnoreCase);

        TypeAdapterConfig<Entity, EntityWithColumnAttributes>
            .NewConfig()
            .ConstructUsing(entity => new()
                {
                    ValueId = entity.Id,
                    ValueBoolean = entity.BooleanValue,
                    ValueByte = entity.ByteValue,
                    ValueChar = entity.CharValue,
                    ValueDateOnly = entity.DateOnlyValue,
                    ValueDateTime = entity.DateTimeValue,
                    ValueDecimal = entity.DecimalValue,
                    ValueDouble = entity.DoubleValue,
                    ValueEnum = entity.EnumValue,
                    ValueGuid = entity.GuidValue,
                    ValueInt16 = entity.Int16Value,
                    ValueInt32 = entity.Int32Value,
                    ValueInt64 = entity.Int64Value,
                    ValueSingle = entity.SingleValue,
                    ValueString = entity.StringValue,
                    ValueTimeSpan = entity.TimeSpanValue,
                    ValueTimeOnly = entity.TimeOnlyValue
                }
            );
    }

    /// <summary>
    /// Generates an ID.
    /// </summary>
    /// <returns>An ID.</returns>
    public static Int64 Id() =>
        Interlocked.Increment(ref entityId);

    /// <summary>
    /// Generates the specified number of IDs.
    /// </summary>
    /// <param name="numberOfIds">
    /// The number of IDs to generate.
    /// If omitted a small random number (<see cref="Generate.SmallNumber" />) will be used.
    /// </param>
    /// <returns>A list of IDs.</returns>
    public static List<Int64> Ids(Int32? numberOfIds = null) =>
        [.. Enumerable.Range(0, numberOfIds ?? SmallNumber()).Select(_ => Interlocked.Increment(ref entityId))];

    /// <summary>
    /// Maps <paramref name="objects" /> to a list of <typeparamref name="TTarget" /> objects containing the same data.
    /// </summary>
    /// <typeparam name="TTarget">The type of objects to map <paramref name="objects" /> to.</typeparam>
    /// <param name="objects">The objects to map.</param>
    /// <returns>
    /// A list of <typeparamref name="TTarget" /> objects containing the same data as <paramref name="objects" />.
    /// </returns>
    public static List<TTarget> MapTo<TTarget>(IEnumerable<Object> objects) =>
        objects.Adapt<List<TTarget>>();

    /// <summary>
    /// Maps <paramref name="obj" /> to an instance of <typeparamref name="TTarget" /> containing the same data.
    /// </summary>
    /// <typeparam name="TTarget">The type of object to map <paramref name="obj" /> to.</typeparam>
    /// <param name="obj">The object to map.</param>
    /// <returns>
    /// An instance of <typeparamref name="TTarget" /> containing the same data as <paramref name="obj" />.
    /// </returns>
    public static TTarget MapTo<TTarget>(Object obj) =>
        obj.Adapt<TTarget>();

    /// <summary>
    /// Generates a list of instances of the type <typeparamref name="T" /> populated with test data.
    /// </summary>
    /// <typeparam name="T">The type of instances to generate.</typeparam>
    /// <param name="numberOfObjects">
    /// The number of objects to generate.
    /// If omitted a small random number (<see cref="Generate.SmallNumber" />) will be used.
    /// </param>
    /// <returns>A list of instances of the type <typeparamref name="T" /> populated with test data.</returns>
    public static List<T> Multiple<T>(Int32? numberOfObjects = null)
    {
        fixture.RepeatCount = numberOfObjects ?? SmallNumber();
        return fixture.Create<List<T>>();
    }

    /// <summary>
    /// Generates a list containing the specified number of random values of the type <typeparamref name="T" /> and
    /// <see langword="null" /> values.
    /// </summary>
    /// <typeparam name="T">The type of values to generate.</typeparam>
    /// <param name="numberOfValues">
    /// The number of values to generate.
    /// If omitted a small random number (<see cref="Generate.SmallNumber" />) will be used.
    /// </param>
    /// <returns>
    /// A list of random values of the type <typeparamref name="T" /> and <see langword="null" /> values.
    /// The list is guaranteed to have at least 50% of its values set to <see langword="null" />.
    /// </returns>
    public static List<T?> MultipleNullable<T>(Int32? numberOfValues = null)
        where T : struct
    {
        fixture.RepeatCount = numberOfValues ?? SmallNumber();

        var result = fixture.Create<List<T?>>();

        var nullsToSet = Math.Ceiling(result.Count / 2D);

        while (nullsToSet > 0)
        {
            var index = faker.Random.Int(0, result.Count - 1);

            if (result[index] is not null)
            {
                result[index] = null;
                nullsToSet--;
            }
        }

        return result;
    }

    /// <summary>
    /// Generates a random scalar value of a randomly chosen type from the following list:
    /// Boolean, Byte, Char, DateTimeOffset, DateTime, Decimal, Double, Guid, Int16, Int32, Int64, Single, String,
    /// or TimeSpan.
    /// </summary>
    /// <returns>A random scalar value.</returns>
    public static Object ScalarValue() =>
        faker.Random.Int(0, 14) switch
        {
            0 => fixture.Create<Boolean>(),
            1 => fixture.Create<Byte>(),
            2 => fixture.Create<Char>(),
            3 => fixture.Create<DateTimeOffset>(),
            4 => fixture.Create<DateTime>(),
            5 => fixture.Create<Decimal>(),
            6 => fixture.Create<Double>(),
            7 => fixture.Create<Guid>(),
            8 => fixture.Create<Int16>(),
            9 => fixture.Create<Int32>(),
            10 => fixture.Create<Int64>(),
            11 => fixture.Create<Single>(),
            12 => fixture.Create<String>(),
            13 => fixture.Create<TimeSpan>(),
            _ => fixture.Create<Int32>()
        };

    /// <summary>
    /// Generates an instance of the type <typeparamref name="T" /> populated with test data.
    /// </summary>
    /// <typeparam name="T">The type of object to generate.</typeparam>
    /// <returns>An instance of the type <typeparamref name="T" /> populated with test data.</returns>
    public static T Single<T>()
    {
        fixture.RepeatCount = SmallNumber(); // For array/collection properties within the type T.
        return fixture.Create<T>();
    }

    /// <summary>
    /// Generates a random number between 5 and 15.
    /// </summary>
    /// <returns>A random number between 5 and 15.</returns>
    public static Int32 SmallNumber() =>
        faker.Random.Int(5, 15);

    /// <summary>
    /// Creates a copy of <paramref name="entity" /> where all properties except the key property / properties have new
    /// values.
    /// </summary>
    /// <typeparam name="T">The type of entity to create an updated copy of.</typeparam>
    /// <param name="entity">The entity for which to create an updated copy.</param>
    /// <returns>
    /// A copy of <paramref name="entity" /> where all properties except the key property / properties have new values.
    /// </returns>
    public static T UpdateFor<T>(T entity)
    {
        var updatedEntity = Single<T>();
        CopyKeys(entity, updatedEntity);

        // For the rare case that all generated values are the same as in the original entity,
        // regenerate until at least one value is different.
        while (entity!.Equals(updatedEntity))
        {
            updatedEntity = Single<T>();
            CopyKeys(entity, updatedEntity);
        }

        return updatedEntity;
    }

    /// <summary>
    /// Creates a list with copies of <paramref name="entities" /> where all properties except the key property /
    /// properties have new values.
    /// </summary>
    /// <typeparam name="T">The type of entities to create updated copies of.</typeparam>
    /// <param name="entities">The entities for which to create updated copies.</param>
    /// <returns>
    /// A list with copies of <paramref name="entities" /> where all properties except the key property / properties
    /// have new values.
    /// </returns>
    public static List<T> UpdatesFor<T>(List<T> entities) =>
        [.. entities.Select(UpdateFor)];

    /// <summary>
    /// Copies the values of all key properties (properties denoted with a <see cref="KeyAttribute" />) from
    /// <paramref name="sourceEntity" /> to <paramref name="targetEntity" />.
    /// </summary>
    /// <typeparam name="T">The type of the entities to copy keys from and to.</typeparam>
    /// <param name="sourceEntity">The source entity to copy keys from.</param>
    /// <param name="targetEntity">The target entity to copy keys to.</param>
    private static void CopyKeys<T>(T sourceEntity, T targetEntity)
    {
        foreach (var keyProperty in EntityHelper.GetEntityTypeMetadata(typeof(T)).KeyProperties)
        {
            var keyPropertyValue = keyProperty.PropertyGetter!(sourceEntity);
            keyProperty.PropertySetter!(targetEntity, keyPropertyValue);
        }
    }

    /// <summary>
    /// The characters used for Char generation.
    /// We only use alphabetic characters for Char generation to avoid issues with databases that do not support
    /// certain characters.
    /// </summary>
    private static readonly Char[] characters = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz".ToCharArray();

    private static readonly Faker faker;
    private static readonly Fixture fixture;
    private static Int64 entityId = 1;
}
