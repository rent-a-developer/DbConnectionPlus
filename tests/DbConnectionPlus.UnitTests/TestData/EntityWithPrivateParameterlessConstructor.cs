namespace RentADeveloper.DbConnectionPlus.UnitTests.TestData;

public record EntityWithPrivateParameterlessConstructor : Entity
{
    private EntityWithPrivateParameterlessConstructor()
    {
    }
}
