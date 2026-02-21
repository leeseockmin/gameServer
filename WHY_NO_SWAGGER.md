# Swagger를 사용하지 않는 이유

## 결론

FrogTailGameServer는 **순수 gRPC 서버(HTTP/2 전용)** 입니다.
Swagger(OpenAPI)는 HTTP/1.1 + JSON 기반이므로 구조적으로 호환되지 않습니다.

---

## 기술적 근거

### 1. 프로토콜 불호환

| 항목 | Swagger (OpenAPI) | FrogTailGameServer (gRPC) |
|------|-------------------|---------------------------|
| 전송 프로토콜 | HTTP/1.1 (또는 HTTP/2 fallback) | HTTP/2 전용 |
| 직렬화 | JSON | Protocol Buffers (Binary) |
| 계약 정의 | OpenAPI YAML/JSON | `.proto` 파일 |
| 스트리밍 | 미지원 | 서버/클라이언트/양방향 스트리밍 가능 |

### 2. Kestrel 설정

```csharp
// Program.cs
serverOptions.ListenAnyIP(9001, o =>
    o.Protocols = HttpProtocols.Http2);
```

포트 9001은 **HTTP/2 전용**으로 설정되어 있습니다.
Swagger UI는 브라우저에서 HTTP/1.1로 접속하여 REST 요청을 보내므로
이 포트에 연결 자체가 불가능합니다.

### 3. gRPC의 직렬화 방식

gRPC 요청/응답은 Protobuf 바이너리 포맷으로 직렬화됩니다.
HTTP 본문을 JSON으로 직접 작성하여 전송하는 Swagger의 방식과
근본적으로 다릅니다.

### 4. `Grpc.AspNetCore`와 OpenAPI

`Grpc.AspNetCore`는 ASP.NET Core의 Controller/Endpoint 라우팅을
사용하지 않습니다. gRPC 서비스는 `MapGrpcService<T>()`로 별도 등록되며,
OpenAPI의 `[ApiController]` / `[Route]` 어트리뷰트 기반 탐색과
호환되지 않습니다.

---

## 대안: WPF gRPC 테스트 클라이언트

Swagger 대신 `WpfTestClient` 프로젝트를 사용합니다.

```
WpfTestClient/
├── Services/GrpcClientService.cs   -- 채널/스텁 관리
└── ViewModels/MainViewModel.cs     -- Command 기반 시나리오
```

### 지원 시나리오

| 시나리오 | 설명 |
|----------|------|
| Guest 신규 로그인 | AccessToken 빈 값 전송, GuestToken 발급 확인 |
| Guest 재로그인 | 발급된 GuestToken으로 동일 UserId 반환 확인 |
| ShopList 조회 | x-userid + authorization Bearer 헤더 자동 첨부 |

### Proto 파일 참조 방식

파일을 복사하지 않고 서버 경로를 **링크**로 참조합니다.
proto 변경 시 WpfTestClient를 별도로 수정할 필요가 없습니다.

```xml
<Protobuf Include="..\FrogTailGameServer\Protos\login.proto"
          GrpcServices="Client"
          Link="Protos\login.proto"
          ProtoRoot="..\FrogTailGameServer" />
```

---

## 참고: gRPC 테스트 도구 비교

| 도구 | 특징 | 우리 선택 이유 |
|------|------|----------------|
| Swagger UI | HTTP REST 전용, gRPC 미지원 | 불가 |
| Postman (gRPC) | proto 파일 수동 임포트, 팀 공유 필요 | 관리 부담 |
| grpcurl | CLI, 스크립트 가능 | 비개발자 접근 어려움 |
| **WpfTestClient** | 프로젝트 내 포함, 시나리오 자동화, MVVM | **채택** |
