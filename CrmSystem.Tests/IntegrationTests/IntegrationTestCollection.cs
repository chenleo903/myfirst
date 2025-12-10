using Xunit;

namespace CrmSystem.Tests.IntegrationTests;

/// <summary>
/// Collection definition for integration tests.
/// All tests in this collection share the same CrmApiFactory instance,
/// which means they share the same PostgreSQL container.
/// </summary>
[CollectionDefinition("Integration")]
public class IntegrationTestCollection : ICollectionFixture<CrmApiFactory>
{
    // This class has no code, and is never created.
    // Its purpose is to be the place to apply [CollectionDefinition]
    // and all the ICollectionFixture<> interfaces.
}
