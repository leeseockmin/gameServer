# FrogTailGameServer - Agent Memory

## 아키텍처 현황 (2026-02)

### 전환 완료: REST -> gRPC 전용
- 포트: 9001 (HTTP/2, h2c)
- REST(Controllers/Services) 계층 완전 제거
- gRPC 서비스: `GrpcAuthService`, `GrpcShopService`
- 인증: `AuthInterceptor` (gRPC Interceptor) - `x-userid` + `authorization` 헤더 검증

### 주요 파일 경로
- `FrogTailGameServer/Program.cs` — 앱 진입점, DI/Kestrel/gRPC 설정
- `FrogTailGameServer/GrpcServices/GrpcAuthService.cs` — Login, VerifyLogin gRPC 구현
- `FrogTailGameServer/GrpcServices/GrpcShopService.cs` — GetShopList gRPC 구현
- `FrogTailGameServer/GrpcServices/AuthInterceptor.cs` — 세션 인증 인터셉터
- `FrogTailGameServer/DB/DataBaseManager.cs` — DBContextExecuteTransaction 제공
- `FrogTailGameServer/Protos/` — auth.proto, shop.proto, common.proto
- `Common/Redis/RedisClient.cs` — Redis 세션 관리 (partial class)
- `TestClient/Program.cs` — gRPC 클라이언트 테스트 코드

### Proto 관련 규칙
- 서버: `GrpcServices="Server"`, 클라이언트: `GrpcServices="Client"`
- TestClient에서 import 경로 해결: `ProtoRoot="..\FrogTailGameServer"` 필수
- proto 생성 ErrorCode: `UnknowError` (오타 그대로, proto 정의 기준)

### 공유 타입 위치
- `LoginType`, `OsType` — `Share.Common` 네임스페이스 (`Share/Common/LoginType.cs`)
- `ErrorCode` — `Share.Common` 네임스페이스 (`Share/Common/ErrorCode.cs`)
- GrpcAuthService에서 `using Share.Common;` 필수

### Anonymous 메서드 (인증 불필요)
- `/auth.AuthService/Login`
- `/auth.AuthService/VerifyLogin`

### 코딩 규칙 (확인됨)
- 모든 I/O에 `.ConfigureAwait(false)` 적용
- 예외는 `catch`에서 `throw` 재발생 (삼키지 않음)
- `DbContext`는 `IDbContextFactory<T>`로 관리 (Singleton factory)
- DI 등록: `RedisClient`, `DataBaseManager`, `HttpManager`, `GameTableManager`, `SecretManager` 모두 Singleton
- `AuthInterceptor`도 Singleton으로 등록 후 `options.Interceptors.Add<AuthInterceptor>()` 사용
- nullable 경고는 `NoWarn` 억제 금지 - 코드에서 명시적 처리

### 주의사항
- `DataBaseManager.GetDBContext()`에서 지원하지 않는 DBtype은 예외 throw
- Firebase 초기화는 `Program.cs`에서 `FireBase.InitFireBase(json)` 직접 호출
- UniqueKey는 `UniqueKey.LoadUniqueKey(1)` (serverId=1)로 초기화
