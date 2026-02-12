# FrogTailGameServer - 보안 및 코드 리뷰 보고서

## 개요
FrogTailGameServer 코드베이스에 대한 보안 취약점 점검 및 코드 구조/최적화 분석 보고서입니다.

---

## Part 1: 보안 이슈 (수정 완료)

### CRITICAL

| # | 이슈 | 파일 | 수정 내용 |
|---|------|------|----------|
| 1 | SQL Injection | `AccountInfo.cs`, `AccountLinkInfo.cs` | 문자열 보간 → Dapper 파라미터 바인딩 (`@param`) |
| 2 | 잘못된 테이블명 | `AccountLinkInfo.cs` `GetAccountLinkInfos()` | `account` → `accountLink` 테이블 수정 |
| 3 | 약한 암호화 키 (2바이트) | `SecretManager.cs` | 하드코딩 `"Hi"` → 설정 기반 32바이트 AES-256 키 |
| 4 | 트랜잭션 로직 반전 | `DataBaseManager.cs` `DBContextExcuteTransaction()` | `isSuccess == false` → `isSuccess == true` |
| 5 | NullReferenceException | `Login.cs` `VerityLoginReqPacketHanlder()` | `ans = null` 후 즉시 접근 → 먼저 초기화 |
| 6 | 미들웨어 헤더 검증 취약 | `CustomMiddleWare.cs` | `ElementAt(0/1)` → 명시적 `X-UserId`, `Authorization` 헤더명 검증 |
| 7 | PacketHandler 미등록 패킷 크래시 | `PacketHandler.cs` | 직접 인덱싱 → `TryGetValue` + 에러 응답 |
| 8 | AccessToken 로그 노출 | `Login.cs` | 토큰 직접 출력 제거, 마스킹 처리 |
| 9 | HttpClient 소켓 누수 | `HttpManager.cs` | 매번 `new HttpClient()` → `static readonly` 싱글톤 재사용 |
| 10 | CORS 무제한 허용 | `Program.cs` | Dev에서만 AllowAny, 프로덕션은 설정 기반 origin 제한 |
| 11 | GameDB UserInfo 미저장 | `Login.cs` 로그인 트랜잭션 | INSERT/SELECT 로직 추가 + NullRef 방지 |
| 12 | SendResponse null 반환 | `CustomMiddleWare.cs` | `return null` → `return Task.CompletedTask` + null 체크 추가 |
| 13 | 미사용 import 정리 | 다수 파일 | 중복/미사용/내부API using 제거 |

### 설정 변경 사항
- `appsettings.json`에 `Security` 섹션 추가:
  - `EncryptionKey`: AES-256 암호화 키 (반드시 프로덕션 배포 전 변경 필요)
  - `AllowedOrigins`: CORS 허용 origin 목록

### 신규 파일
- `Data/Logic/GameDBLogic/UserInfoData.cs`: UserInfo DB 조회/삽입 로직

---

## Part 2: 코드 구조 및 최적화 분석 (미수정 - 개선 권장)

### CRITICAL - 반드시 개선 필요

#### 1. `do-while(false)` 안티패턴
**파일:** `Login.cs` 전체

C에서 유래한 패턴으로 C#에서는 부적절합니다. Guard Clause 패턴으로 리팩토링을 권장합니다.

```csharp
// 현재 (안티패턴)
do {
    if (error) { break; }
    // ...logic...
} while (false);

// 권장 (Guard Clause)
if (error) { return errorResponse; }
// ...logic...
```

#### 2. 예외 삼킴 (Swallowed Exceptions)
**파일:** `RedisClient.cs`, `DataBaseManager.cs`, `Login.cs`

모든 catch 블록이 로그만 남기고 예외를 재throw하지 않아 호출자가 실패를 감지할 수 없습니다.

```csharp
// 현재 (문제)
catch (Exception ex) {
    _logger.LogError(ex.Message); // 호출자는 실패를 모름
}

// 권장
catch (Exception ex) {
    _logger.LogError(ex, "Operation failed");
    throw; // 또는 Result<T> 패턴 사용
}
```

#### 3. Static 싱글톤 vs DI 혼용
**파일:** `SecretManager.cs`, `HttpManager.cs`, `GameTableManager.cs`

DI 컨테이너와 수동 `GetInstance()` 싱글톤이 혼용되어 있습니다.

```csharp
// 권장: 모두 DI로 통일
builder.Services.AddSingleton<HttpManager>();
builder.Services.AddSingleton<GameTableManager>();
```

---

### HIGH - 개선 권장

#### 4. 네이밍 오타 (전체 코드베이스 영향)
| 현재 | 올바른 이름 | 위치 |
|------|------------|------|
| `ErrrorCode` | `ErrorCode` | `Share/Common/ErrorCode.cs` + 모든 참조 |
| `PacketReqeustBase` | `PacketRequestBase` | `Share/Packet/PacketReqeustBase.cs` + 모든 참조 |
| `Hanlder` | `Handler` | `Login.cs`, `Shop.cs`, `PacketHandler.cs` |
| `Excute` | `Execute` | `HttpManager.cs`, `DataBaseManager.cs`, `PacketHandler.cs` |
| `_acountContextFactory` | `_accountContextFactory` | `DataBaseManager.cs` |
| `Invailid` / `Invaild` | `Invalid` | `Login.cs` 로그 메시지 다수 |

> 클라이언트와 공유하는 패킷/Enum 이름 변경 시 클라이언트도 동시에 수정 필요

#### 5. Login.cs 로직 반전 의심 (line 100-112)
```csharp
// AccessToken이 비어있지 않으면 에러? (일반적으로는 반대)
if(string.IsNullOrEmpty(recvPacket.AccessToken) == false) → 에러
// NickName이 비어있지 않으면 에러?
if(string.IsNullOrEmpty(recvPacket.NickName) == false) → 에러
```

의도한 로직인지 확인이 필요합니다.

#### 6. `SELECT *` 사용
**파일:** `AccountInfo.cs`, `AccountLinkInfo.cs`, `UserInfoData.cs`

필요한 컬럼만 명시적으로 지정하면 네트워크 대역폭과 메모리 사용량을 줄일 수 있습니다.

---

### MEDIUM - 점진적 개선

#### 7. Dead Code 제거
| 파일 | 내용 |
|------|------|
| `Common/Class1.cs` | 빈 클래스 |
| `Shop.cs` | 비어있는 핸들러 (빈 do-while 루프) |
| `RedisClient._server` 필드 | 미사용 (빌드 경고) |
| `CustomMiddleWare.GetAuthHeader()` | 미사용 메서드 (제거됨) |

#### 8. 비동기 패턴 개선
- `RedisClient` 메서드들에 `.ConfigureAwait(false)` 미적용
- `ExpiryAsync` 메서드에 try-catch 미적용 (다른 Redis 메서드와 불일치)
- `TestClient/Program.cs`에 `.Result` 블로킹 호출 존재

#### 9. appsettings.json 보안
- DB 비밀번호, Redis 비밀번호, Firebase 서비스 계정 키가 설정 파일에 평문 저장
- 프로덕션 환경에서는 환경변수, Azure Key Vault, AWS Secrets Manager 등 사용 권장
- `.gitignore`에 `appsettings.Production.json` 추가 권장

---

## 개선 우선순위

1. **즉시:** Login.cs 로직 반전 확인 (line 100-112) - 의도한 것인지 확인
2. **1주 내:** 네이밍 오타 전체 리팩토링
3. **1주 내:** `do-while(false)` → guard clause 리팩토링
4. **2주 내:** DI 통일, 예외 처리 개선
5. **점진적:** Dead code 제거, `SELECT *` 개선, ConfigureAwait 적용
6. **배포 전:** appsettings.json 비밀정보 외부화

---

## 변경된 파일 목록

### 수정된 파일
- `FrogTailGameServer/Program.cs`
- `FrogTailGameServer/ControllerLogic/Login.cs`
- `FrogTailGameServer/ControllerLogic/PacketHandler.cs`
- `FrogTailGameServer/Controllers/PacketController.cs`
- `FrogTailGameServer/MiddleWare/CustomMiddleWare.cs`
- `FrogTailGameServer/MiddleWare/Secret/SecretManager.cs`
- `FrogTailGameServer/DB/DataBaseManager.cs`
- `FrogTailGameServer/appsettings.json`
- `Common/Http/HttpManager.cs`
- `Common/Redis/RedisClient.cs`
- `Data/Logic/AccountDBLogic/AccountInfo.cs`
- `Data/Logic/AccountDBLogic/AccountLinkInfo.cs`

### 신규 파일
- `Data/Logic/GameDBLogic/UserInfoData.cs`
