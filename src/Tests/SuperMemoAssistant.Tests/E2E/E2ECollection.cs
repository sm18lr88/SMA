// The end-to-end tests share a desktop name, the registry snapshot and one SuperMemo install, so they must not run in parallel.
namespace SuperMemoAssistant.Tests.E2E;

using Xunit;

[CollectionDefinition("SuperMemo E2E", DisableParallelization = true)]
public sealed class E2ECollection;
