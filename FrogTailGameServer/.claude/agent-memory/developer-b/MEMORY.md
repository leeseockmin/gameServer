# Developer B - Persistent Memory

## 프로젝트 구조 핵심 경로

- 서버 루트: `C:\Users\user\Desktop\GameServer\gameServer\FrogTailGameServer\`
- 솔루션 루트 (git root): `C:\Users\user\Desktop\GameServer\gameServer\`
- gRPC 서비스: `FrogTailGameServer/GrpcServices/Grpc{기능}Service.cs`
- Proto 파일: `FrogTailGameServer/Protos/*.proto`
- DB Logic: `Data/Logic/{도메인}DBLogic/`
- 테스트 프로젝트: `FrogTailGameServer.Tests/`
- 테스트 헬퍼: `FrogTailGameServer.Tests/Helpers/TestServerCallContext.cs`

## Mock 패턴 (xUnit + Moq)

### DataBaseManager mock
```csharp
_dbManagerMock = new Mock<DataBaseManager>(
    new Mock<ILogger<DataBaseManager>>().Object,
    new Mock<IDbContextFactory<AccountDBContext>>().Object,
    new Mock<IDbContextFactory<GameDBContext>>().Object)
{ CallBase = false };
```
- `DBContextExcuteTransaction`은 virtual — mock 가능 (오타 주의: Excute, not Execute)

### RedisClient mock
```csharp
var configMock = new Mock<IConfiguration>();
configMock.Setup(c => c["ConnectionStrings:RedisConnection"])
          .Returns("localhost:6379,abortConnect=false");
_redisMock = new Mock<RedisClient>(configMock.Object, loggerMock.Object)
{ CallBase = false };
```
- 반드시 `c["ConnectionStrings:RedisConnection"]` 인덱서로 Setup (GetConnectionString 확장 메서드 아님)
- `SetUserSession`은 virtual — mock 가능

### TestServerCallContext.AuthContext 생성자
```csharp
new AuthContext(null, new Dictionary<string, List<AuthProperty>>())
// IEnumerable<AuthProperty>가 아닌 List<AuthProperty>
```

## 중요 설계 결정

- `DBContextExcuteTransaction` mock 시 콜백 미실행 → `accountId = 0` → Game DB 미호출 → `UnknowError` 반환
  - 테스트에서 DB 실패 케이스: `Times.Once()` (Account만), 반환은 `UnknowError`
- Protobuf 필드는 null 불가 → 빈 문자열 `""` 사용
- `gh` CLI 미설치 → PR은 GitHub 웹에서 직접 생성 필요

## 공통 오타 (원본 코드에 맞춰야 함)

- `DBContextExcuteTransaction` (Execute가 아닌 Excute)
- `ErrrorCode` (오타, 기존 코드) vs `ErrorCode` (올바른 enum)
- `UnknowError` (proto/enum에서의 표기)

## gh CLI 없음

현재 환경에 `gh` CLI가 설치되어 있지 않다. PR은 GitHub 웹 UI에서 직접 생성해야 한다.
- chore/remove-configure-await: https://github.com/leeseockmin/gameServer/pull/new/chore/remove-configure-await
- feature/guest-login: https://github.com/leeseockmin/gameServer/pull/new/feature/guest-login

## 세부 패턴 파일

- 상세 테스트 패턴: `testing.md`
