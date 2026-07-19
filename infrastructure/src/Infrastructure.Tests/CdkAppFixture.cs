using System;
using Amazon.CDK;
using Xunit;

namespace Infrastructure.Tests
{
    /// <summary>
    /// Shared CDK App fixture that initializes the JSII runtime once across all
    /// infrastructure tests. Without this, parallel test execution causes concurrent
    /// writes to the same JSII asset .tgz temp file, producing file-lock errors.
    /// </summary>
    public class CdkAppFixture : IDisposable
    {
        public App App { get; }

        public CdkAppFixture()
        {
            App = new App();
        }

        public void Dispose()
        {
            // App does not implement IDisposable; nothing to clean up.
        }
    }

    /// <summary>
    /// xUnit collection definition that groups all CDK construct tests under a
    /// single shared fixture and forces sequential execution within the group.
    /// </summary>
    [CollectionDefinition("CDK Tests", DisableParallelization = true)]
    public class CdkTestCollection : ICollectionFixture<CdkAppFixture>
    {
    }
}
