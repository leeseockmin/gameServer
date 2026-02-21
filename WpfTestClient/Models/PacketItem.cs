namespace WpfTestClient.Models;

/// <summary>
/// 시나리오 구성에서 선택 가능한 단일 패킷 항목.
/// </summary>
public sealed class PacketItem
{
    public string ServiceName  { get; init; } = string.Empty;
    public string RpcName      { get; init; } = string.Empty;
    public bool   RequiresAuth { get; init; }

    public string DisplayName => $"{ServiceName}.{RpcName}";

    /// <summary>
    /// 정적으로 등록된 전체 사용 가능 패킷 목록.
    /// 새 RPC 추가 시 여기에 등록하세요.
    /// </summary>
    public static IReadOnlyList<PacketItem> All { get; } =
    [
        new PacketItem { ServiceName = "LoginService", RpcName = "Login",       RequiresAuth = false },
        new PacketItem { ServiceName = "LoginService", RpcName = "VerityLogin", RequiresAuth = false },
        new PacketItem { ServiceName = "ShopService",  RpcName = "ShopList",    RequiresAuth = true  },
    ];
}
