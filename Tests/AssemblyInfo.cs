using Xunit;

[assembly: TestCollectionOrderer("DCB.Tests.CustomTestCollectionOrderer", "Tests")]
[assembly: CollectionBehavior(DisableTestParallelization = true)] 