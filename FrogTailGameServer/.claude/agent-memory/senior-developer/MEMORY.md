# Senior Developer Memory

## 프로젝트 핵심 정보

### Proto 파일 실제 현황 (2026-02-21 기준)
- `FrogTailGameServer/Protos/common.proto` — ErrorCode, LoginType, OsType enum
- `FrogTailGameServer/Protos/login.proto` — LoginService (Login, VerityLogin) // "Verify" 오타 주의
- `FrogTailGameServer/Protos/shop.proto` — ShopService (ShopList)
- namespace: `FrogTailGameServer.Grpc`, proto package: `login`, `shop`

### TestClient 불일치 사항 (수정 필요)
- `TestClient.csproj`가 `auth.proto`를 참조 → 실제 파일명은 `login.proto`
- `Program.cs`가 `AuthService.AuthServiceClient` 사용 → 실제는 `LoginService.LoginServiceClient`
- `VerifyLoginAsync` 호출 → proto에는 `VerityLogin` (오타)
- `GetShopListAsync` 호출 → proto에는 `ShopList`

### 인증 헤더 (AuthInterceptor)
- `x-userid`: userId 문자열
- `authorization`: `Bearer {userToken}`
- Anonymous 메서드: `/login.LoginService/Login`, `/login.LoginService/VerityLogin`

### 서버 포트
- gRPC: http://localhost:9001 (h2c, HTTP/2 without TLS)

### 현재 등록된 gRPC 서비스
- `GrpcAuthService` (LoginService.LoginServiceBase) — Program.cs에 MapGrpcService 등록됨
- ShopService — 아직 미등록 (구현 필요)

### 계층 구조
gRPC Service → DataBaseManager → DB.Data.Logic.[AccountDB|GameDB]Logic
- `DataBaseManager.DBContextExcuteTransaction(DBtype.Account | DBtype.Game, ...)`

### Proto 링크 참조 방식 (TestClient.csproj 패턴)
```xml
<Protobuf Include="..\FrogTailGameServer\Protos\login.proto"
          GrpcServices="Client"
          Link="Protos\login.proto"
          ProtoRoot="..\FrogTailGameServer" />
```

## 상세 자료
- `patterns.md`: 코딩 패턴 및 컨벤션
