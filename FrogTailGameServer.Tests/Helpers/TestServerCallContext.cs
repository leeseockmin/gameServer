using Grpc.Core;

namespace FrogTailGameServer.Tests.Helpers
{
    /// <summary>
    /// gRPC 단위 테스트에서 ServerCallContext 대신 사용하는 최소 구현체.
    /// </summary>
    public class TestServerCallContext : ServerCallContext
    {
        private readonly Metadata _requestHeaders;
        private readonly CancellationToken _cancellationToken;

        private TestServerCallContext(Metadata requestHeaders, CancellationToken cancellationToken)
        {
            _requestHeaders    = requestHeaders;
            _cancellationToken = cancellationToken;
        }

        public static TestServerCallContext Create(
            Metadata? requestHeaders = null,
            CancellationToken cancellationToken = default)
            => new(requestHeaders ?? new Metadata(), cancellationToken);

        // --- 추상 멤버 구현 (테스트에서는 사용 안 함) ---
        protected override string MethodCore        => "TestMethod";
        protected override string HostCore          => "TestHost";
        protected override string PeerCore          => "TestPeer";
        protected override DateTime DeadlineCore    => DateTime.MaxValue;
        protected override Metadata RequestHeadersCore => _requestHeaders;
        protected override CancellationToken CancellationTokenCore => _cancellationToken;
        protected override Metadata ResponseTrailersCore => new Metadata();
        protected override Status StatusCore         { get; set; }
        protected override WriteOptions? WriteOptionsCore { get; set; }
        protected override AuthContext AuthContextCore =>
            new AuthContext(null, new Dictionary<string, List<AuthProperty>>());

        protected override ContextPropagationToken CreatePropagationTokenCore(ContextPropagationOptions? options)
            => throw new NotImplementedException();

        protected override Task WriteResponseHeadersAsyncCore(Metadata responseHeaders)
            => Task.CompletedTask;
    }
}
