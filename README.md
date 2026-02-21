# FrogTailGameServer

.NET 8 기반 gRPC 게임 서버입니다. Firebase 인증, MySQL 이중 DB(Account/Game), Redis 세션 관리를 핵심으로 하며, Claude Code 에이전트 팀(CTO - 시니어 - 개발자 - DBA - QA) 워크플로우로 개발됩니다.

---

## 목차

1. [프로젝트 개요](#1-프로젝트-개요)
2. [개발 환경 세팅](#2-개발-환경-세팅)
3. [서버 구동 방법](#3-서버-구동-방법)
4. [패킷 생성 방법](#4-패킷-생성-방법-신규-api-추가-시작점)
5. [gRPC API 추가 방법](#5-grpc-api-추가-방법)
6. [DB 작업 방법](#6-db-작업-방법)
7. [에러 코드 추가 방법](#7-에러-코드-추가-방법)
8. [테스트 방법](#8-테스트-방법)
9. [클라이언트 적용 방법](#9-클라이언트-적용-방법)
10. [멀티 클라이언트 Proto 적용 가이드](#10-멀티-클라이언트-proto-적용-가이드)
11. [브랜치 전략 / 개발 워크플로우](#11-브랜치-전략--개발-워크플로우)
12. [코딩 컨벤션](#12-코딩-컨벤션)

---

## 1. 프로젝트 개요

### 기술 스택

| 분류 | 기술 | 버전 |
|------|------|------|
| 런타임 | C# / .NET | 12 / 8.0 |
| 통신 프로토콜 | gRPC + Protobuf | Grpc.AspNetCore 2.63.0 |
| ORM | EF Core (MySQL) | 8.0.4 |
| 성능 쿼리 | Dapper | 2.1.35 |
| 데이터베이스 | MySQL | 8.x |
| 캐시 / 세션 | Redis (StackExchange.Redis) | 2.7.33 |
| 인증 | Firebase Admin SDK | 3.4.0 |
| 로깅 | Serilog + File Sink | 8.0.0 |
| Proto 자동화 | PacketToProtoGenerator (자체 제작) | - |

### 솔루션 전체 구조

```
gameServer/
├── FrogTailGameServer/          # 메인 gRPC 서버 (진입점)
│   ├── GrpcServices/            # gRPC 서비스 구현체 + AuthInterceptor
│   ├── Protos/                  # .proto 파일 (generate-proto.bat 으로 자동 생성)
│   ├── DB/                      # DataBaseManager (DB 접근 진입점)
│   ├── MiddleWare/
│   │   ├── Secret/              # SecretManager (AES-256 암복호화)
│   │   └── User/                # CustomIdentity / CustomPrincipal
│   ├── Logic/Utils/             # FireBase, UniqueKey, RandToken 등 유틸
│   ├── appsettings.json         # 운영 설정 (DB 연결, Firebase, Redis)
│   └── appsettings.Development.json  # 개발 환경 오버라이드
│
├── Common/                      # 공통 모듈
│   ├── Redis/                   # RedisClient (세션 CRUD)
│   ├── Http/                    # HttpManager
│   └── GameTable/               # GameTableManager (JSON 게임 데이터)
│
├── Data/  (프로젝트명: DB.Data)  # EF Core DBContext + Dapper Logic
│   ├── AccountDB/               # AccountDBContext
│   ├── GameDB/                  # GameDBContext
│   ├── Logic/
│   │   ├── AccountDBLogic/      # AccountInfo, AccountLinkInfo Dapper 쿼리
│   │   └── GameDBLogic/         # UserInfoData 등 Dapper 쿼리
│   └── Migrations/
│       ├── AccountMigrations/
│       └── GameMigrations/
│
├── Share/                       # 공유 라이브러리 (클라이언트-서버 공통)
│   ├── Common/                  # ErrorCode, PacketId, LoginType, OsType 등
│   └── Packet/                  # C# 패킷 클래스 (-> proto 자동 생성 원본)
│       ├── LoginPacket/         # Login.cs
│       └── ShopPacket/          # Shop.cs
│
├── PacketToProtoGenerator/      # Share/Packet -> Protos/*.proto 변환 도구
├── generate-proto.bat           # proto 생성 실행 스크립트 (루트에서 실행)
│
├── WpfTestClient/               # GUI 테스트 클라이언트 (Windows)
├── TestClient/                  # 콘솔 통합 테스트 클라이언트
└── FrogTailGameServer.Tests/    # 단위 테스트 프로젝트
```

### 핵심 아키텍처 흐름

```
클라이언트
    |  gRPC (HTTP/2, 포트 9001)
    v
AuthInterceptor  <-- 모든 요청 통과 (인증 필요 API만 세션 검증)
    |
GrpcXxxService   <-- 요청 파싱, 검증, 응답 조립
    |
DataBaseManager  <-- 트랜잭션 관리, DB 연결 제공
    |
DB.Data.Logic.*  <-- 실제 SQL (Dapper)
    |
MySQL (accountdb / gamedb)

별도: RedisClient <-- 사용자 세션 저장/조회
```

---

## 2. 개발 환경 세팅

### 필수 도구

| 도구 | 버전 | 비고 |
|------|------|------|
| .NET SDK | 8.0 이상 | https://dotnet.microsoft.com/download |
| MySQL | 8.x | 로컬 또는 Docker |
| Redis | 7.x 이상 | 로컬 또는 Docker |
| Visual Studio 2022 | 17.8 이상 | Community 이상, ASP.NET 워크로드 필수 |
| JetBrains Rider | 2024.x 이상 | VS 대안 |

.NET SDK 버전 확인:

```bash
dotnet --version
```

### appsettings.json 설정

`FrogTailGameServer/appsettings.json` 파일을 열어 아래 항목을 로컬 환경에 맞게 수정합니다.

```json
{
  "ConnectionStrings": {
    "RedisConnection": "localhost:6379,password=<YOUR_REDIS_PASSWORD>,ssl=false,abortConnect=False,connectTimeout=5000",
    "GameDbConnection": "Server=localhost;Database=gamedb;Uid=root;Pwd=<YOUR_MYSQL_PASSWORD>;",
    "AccountDbConnection": "Server=localhost;Database=accountdb;Uid=root;Pwd=<YOUR_MYSQL_PASSWORD>;"
  },
  "FirebaseConfig": {
    "type": "service_account",
    "project_id": "<YOUR_FIREBASE_PROJECT_ID>",
    "private_key_id": "<KEY_ID>",
    "private_key": "-----BEGIN PRIVATE KEY-----\n...\n-----END PRIVATE KEY-----\n",
    "client_email": "<SERVICE_ACCOUNT_EMAIL>"
  },
  "Security": {
    "EncryptionKey": "<32바이트 이상의 임의 문자열>"
  }
}
```

**주의**: `appsettings.json`에는 실제 비밀 키가 포함됩니다. 운영 환경에서는 환경 변수 또는 별도 시크릿 관리 도구를 사용하세요.

#### 각 설정 항목 설명

| 설정 키 | 설명 |
|---------|------|
| `ConnectionStrings:RedisConnection` | Redis 연결 문자열. 비밀번호 포함 |
| `ConnectionStrings:GameDbConnection` | Game DB (gamedb) MySQL 연결 문자열 |
| `ConnectionStrings:AccountDbConnection` | Account DB (accountdb) MySQL 연결 문자열 |
| `FirebaseConfig` | Firebase Admin SDK 서비스 계정 JSON. Firebase 콘솔에서 다운로드 |
| `Security:EncryptionKey` | AES-256 암호화 키. 32바이트 이상 임의 문자열 설정 필수 |
| `AccountLinkValidator:ValidKeys` | 개발 환경 전용 Mock 토큰 목록 (`appsettings.Development.json`에 설정) |

#### Firebase 설정 방법

1. Firebase 콘솔 (https://console.firebase.google.com) 접속
2. 프로젝트 설정 > 서비스 계정 > "새 비공개 키 생성" 클릭
3. 다운로드된 JSON 파일의 내용을 `appsettings.json`의 `FirebaseConfig` 섹션에 펼쳐서 입력

### DB 마이그레이션 실행

마이그레이션은 `FrogTailGameServer` 프로젝트를 시작 프로젝트로, `DB.Data` 프로젝트를 대상으로 실행합니다.

Visual Studio Package Manager Console에서 실행:

```powershell
# Account DB 마이그레이션
Add-Migration InitialCreate -Context AccountDBContext -Project DB.Data -OutputDir "Migrations\AccountMigrations"
Update-Database -Context AccountDBContext -Project DB.Data -StartupProject FrogTailGameServer

# Game DB 마이그레이션
Add-Migration InitialCreate -Context GameDBContext -Project DB.Data -OutputDir "Migrations\GameMigrations"
Update-Database -Context GameDBContext -Project DB.Data -StartupProject FrogTailGameServer
```

CLI에서 실행 (솔루션 루트에서):

```bash
dotnet ef migrations add InitialCreate --context AccountDBContext --project Data/DB.Data.csproj --startup-project FrogTailGameServer/FrogTailGameServer.csproj --output-dir Migrations/AccountMigrations

dotnet ef database update --context AccountDBContext --project Data/DB.Data.csproj --startup-project FrogTailGameServer/FrogTailGameServer.csproj
```

---

## 3. 서버 구동 방법

### Visual Studio에서 실행

1. `FrogTailGameServer.sln` 열기
2. 시작 프로젝트를 `FrogTailGameServer`로 설정
3. F5 또는 Ctrl+F5 실행

### CLI에서 실행

```bash
cd FrogTailGameServer
dotnet run
```

운영 환경으로 실행:

```bash
ASPNETCORE_ENVIRONMENT=Production dotnet run
```

### 포트 정보

| 포트 | 프로토콜 | 설명 |
|------|---------|------|
| 9001 | HTTP/2 (gRPC) | gRPC 서비스 (Kestrel, `ListenAnyIP(9001)`) |

### 정상 실행 시 확인할 로그

서버가 정상 기동되면 아래와 같은 로그가 출력됩니다:

```
[yyyy-MM-dd HH:mm:ss INF] Now listening on: http://0.0.0.0:9001
[yyyy-MM-dd HH:mm:ss INF] Application started. Press Ctrl+C to shut down.
```

Redis 연결 실패 시 서버가 즉시 종료됩니다:

```
Exception: Redis Connect Error
```

로그 파일 위치: `FrogTailGameServer/Logs/log-{yyyy-MM-dd}.txt`

### 개발 / 운영 환경 전환

`ASPNETCORE_ENVIRONMENT` 환경 변수로 제어합니다:

- `Development`: `appsettings.Development.json` 설정이 `appsettings.json`을 오버라이드. Mock 토큰 허용, userId 평문 전송 허용
- `Production`: `appsettings.json`만 적용. userId AES-256 암호화 필수

`AuthInterceptor`는 `Development` 환경에서 userId를 평문으로 받고, `Production` 환경에서는 `SecretManager`로 복호화합니다.

---

## 4. 패킷 생성 방법 (신규 API 추가 시작점)

신규 API를 추가할 때는 반드시 패킷 정의부터 시작합니다. `Share/Packet/`에 C# 클래스를 작성하면 `generate-proto.bat`이 `.proto` 파일을 자동으로 생성합니다.

### Step 1: Share/Packet/ 에 C# 패킷 클래스 작성

#### 파일 위치 규칙

```
Share/Packet/{기능명}Packet/{기능명}.cs
```

예시:
- `Share/Packet/LoginPacket/Login.cs`
- `Share/Packet/ShopPacket/Shop.cs`

#### 클래스 명명 규칙

| 방향 | 접두사 | 접미사 | 예시 |
|------|-------|-------|------|
| 클라이언트 -> 서버 (요청) | `CG` | `ReqPacket` | `CGShopListReqPacket` |
| 서버 -> 클라이언트 (응답) | `GC` | `AnsPacket` | `GCShopListAnsPacket` |

`PacketToProtoGenerator`는 이 명명 규칙으로 Request/Response 메시지를 구분합니다. 규칙을 반드시 준수하세요.

#### 기존 예시: LoginPacket

```csharp
// Share/Packet/LoginPacket/Login.cs
namespace Share.Packet
{
    public class CGLoginReqPacket : PacketRequestBase
    {
        public CGLoginReqPacket() : base(PacketId.CG_Login_Req_Packet_Id) { }

        public string DeviceId { get; set; }
        public string NickName { get; set; }
        public OsType OsType { get; set; }
        public LoginType LoginType { get; set; }
        public string AccessToken { get; set; }
    }

    public class GCLoginAnsPacket : PacketAnsPacket
    {
        public GCLoginAnsPacket()
        {
            ErrorCode = Share.Common.ErrorCode.SUCCESS;
        }

        public string UserToken { get; set; }
        public long UserId { get; set; }
    }
}
```

#### 기존 예시: ShopPacket (중첩 데이터 클래스 포함)

```csharp
// Share/Packet/ShopPacket/Shop.cs
namespace Share.Packet.ShopPacket
{
    // 데이터 클래스 (CG/GC 접두사 없음 -> proto message로 생성됨)
    public class ShopData
    {
        public int ShopId { get; set; }
        public List<ShopItemData> ShopItemDatas { get; set; }
    }

    public class ShopItemData
    {
        public int ShopItemId { get; set; }
        public long BuyCount { get; set; }
    }

    public class CGShopListReqPacket : PacketRequestBase
    {
        public CGShopListReqPacket() : base(PacketId.CG_ShopList_Req_Packet_Id) { }
    }

    public class GCShopListAnsPacket : PacketAnsPacket
    {
        public GCShopListAnsPacket()
        {
            ErrorCode = Share.Common.ErrorCode.SUCCESS;
        }

        public List<ShopData> ShopDatas { get; set; }
    }
}
```

#### PacketToProtoGenerator 파싱 규칙 요약

| C# 타입 | proto 타입 |
|---------|-----------|
| `int` | `int32` |
| `long` | `int64` |
| `float` | `float` |
| `double` | `double` |
| `bool` | `bool` |
| `string` | `string` |
| `List<T>` | `repeated T` |
| Enum / 커스텀 클래스 | 타입명 그대로 |

- `GCAnsPacket` 응답 메시지에는 `ErrorCode error_code`가 자동으로 첫 번째 필드로 삽입됩니다.
- 속성명은 PascalCase -> snake_case로 자동 변환됩니다. (예: `UserId` -> `user_id`)
- 스킵 파일: `PacketRequestBase.cs`, `PacketId.cs`, `Class1.cs`

### Step 2: generate-proto.bat 실행

솔루션 루트(`gameServer/`)에서 실행합니다:

```cmd
generate-proto.bat
```

실행 결과: `Share/Packet/` 하위 폴더 구조를 분석해서 `FrogTailGameServer/Protos/` 에 `.proto` 파일을 생성합니다.

```
=== PacketToProtoGenerator ===
  Packet 경로 : C:\...\Share\Packet
  Proto 출력  : C:\...\FrogTailGameServer\Protos

[생성] login.proto  (클래스 4개)
[생성] shop.proto   (클래스 4개)

완료: 2개 파일 생성됨
```

**주의사항:**
- `generate-proto.bat`은 기존 `.proto` 파일을 덮어씁니다. 수동으로 수정한 내용이 있다면 별도로 보관하세요.
- `common.proto`는 자동 생성 대상이 아닙니다. 수동으로 관리합니다.

### Step 3: 생성된 proto 파일 확인

`FrogTailGameServer/Protos/` 에 생성된 파일을 열어 검토합니다.

```protobuf
// FrogTailGameServer/Protos/login.proto (자동 생성 예시)
syntax = "proto3";
import "Protos/common.proto";
option csharp_namespace = "FrogTailGameServer.Grpc";
package login;

message LoginRequest {
  string device_id = 1;
  string nick_name = 2;
  OsType os_type = 3;
  LoginType login_type = 4;
  string access_token = 5;
}

message LoginResponse {
  ErrorCode error_code = 1;  // GCAnsPacket -> 자동 삽입
  string user_token = 2;
  int64 user_id = 3;
  string guest_token = 4;
}

service LoginService {
  rpc Login (LoginRequest) returns (LoginResponse);
  rpc VerityLogin (VerityLoginRequest) returns (VerityLoginResponse);
}
```

검토 포인트:
- 메시지 필드 번호가 올바른지
- `repeated` 키워드가 `List<T>` 필드에 붙었는지
- service/rpc 이름이 의도한 대로 생성되었는지
- 타입 매핑이 올바른지 (특히 enum 타입)

`.csproj`에 이미 `<Protobuf Include="Protos\*.proto" GrpcServices="Server" />`가 등록되어 있으므로, 새 `.proto` 파일을 추가해도 별도 등록이 필요 없습니다.

---

## 5. gRPC API 추가 방법

패킷 정의(Step 1~3) 완료 후 아래 단계를 진행합니다.

### Step 4: GrpcServices/ 에 서비스 클래스 작성

#### 파일 위치

```
FrogTailGameServer/GrpcServices/Grpc{기능명}Service.cs
```

#### 클래스 구조 템플릿

```csharp
// FrogTailGameServer/GrpcServices/GrpcShopService.cs (예시)
using Common.Redis;
using DB;
using Grpc.Core;
using Microsoft.Extensions.Logging;

namespace FrogTailGameServer.GrpcServices
{
    public class GrpcShopService : global::FrogTailGameServer.Grpc.ShopService.ShopServiceBase
    {
        private readonly DataBaseManager _dataBaseManager;
        private readonly RedisClient _redisClient;
        private readonly ILogger<GrpcShopService> _logger;

        // DI 컨테이너에서 생성자 주입 - static GetInstance() 절대 사용 금지
        public GrpcShopService(
            DataBaseManager dataBaseManager,
            RedisClient redisClient,
            ILogger<GrpcShopService> logger)
        {
            _dataBaseManager = dataBaseManager;
            _redisClient = redisClient;
            _logger = logger;
        }

        public override async Task<global::FrogTailGameServer.Grpc.ShopListResponse> ShopList(
            global::FrogTailGameServer.Grpc.ShopListRequest request, ServerCallContext context)
        {
            // 1. 입력 검증 (Guard Clause 패턴)
            // 2. DataBaseManager를 통해서만 DB 접근
            await _dataBaseManager.DBContextExcuteTransaction(
                DataBaseManager.DBtype.Game,
                async (gameConnection) =>
                {
                    // DB.Data.Logic.GameDBLogic.* 호출
                    return true;
                });

            // 3. 응답 조립
            return new global::FrogTailGameServer.Grpc.ShopListResponse
            {
                ErrorCode = global::FrogTailGameServer.Grpc.ErrorCode.Success
            };
        }
    }
}
```

#### 계층 구조 규칙

```
GrpcXxxService          (요청 수신, 검증, 응답 조립)
    |
DataBaseManager         (트랜잭션 관리, DB 연결 제공)
    |
DB.Data.Logic.XxxDBLogic.*  (실제 SQL - Dapper)
```

- gRPC Service에서 직접 `DbConnection`을 생성하거나 `DbContext`를 주입받아서는 안 됩니다.
- 모든 DB 접근은 반드시 `DataBaseManager`를 경유합니다.

### Step 5: Program.cs에 등록

`FrogTailGameServer/Program.cs`에 gRPC 라우팅 등록을 추가합니다:

```csharp
// gRPC 서비스 등록 (app.Run() 이전)
app.MapGrpcService<GrpcAuthService>();   // 기존 서비스
app.MapGrpcService<GrpcShopService>();   // 신규 추가 서비스
```

### Step 6: AuthInterceptor 인증 처리

#### 인증 불필요 API (Anonymous)

`FrogTailGameServer/GrpcServices/AuthInterceptor.cs`의 `AnonymousMethods` HashSet에 gRPC 메서드 경로를 추가합니다:

```csharp
private static readonly HashSet<string> AnonymousMethods = new()
{
    "/login.LoginService/Login",
    "/login.LoginService/VerityLogin",
    // 새로 추가할 인증 불필요 메서드:
    // "/{proto package}.{ServiceName}/{RpcName}",
};
```

메서드 경로 형식: `/{proto package}.{ServiceName}/{RpcName}`

예: `shop.proto`의 `package shop`, `service ShopService`, `rpc ShopList` -> `/shop.ShopService/ShopList`

#### 인증 필요 API (기본값)

`AnonymousMethods`에 등록하지 않으면 자동으로 세션 검증이 적용됩니다.

클라이언트는 다음 헤더를 포함해야 합니다:

```
x-userid: {userId 또는 암호화된 userId}
authorization: Bearer {userToken}
```

개발 환경(`Development`)에서는 userId를 평문으로 전송해도 됩니다.
운영 환경(`Production`)에서는 `SecretManager.EncryptString(userId)`로 암호화한 값을 전송해야 합니다.

### Step 7: DB 작업이 필요한 경우

DBA에게 다음 내용을 전달합니다:

1. 엔티티 클래스 (컬럼 목록, 타입, 제약 조건)
2. 어느 DB에 속하는지 (Account DB / Game DB)
3. 필요한 쿼리 목록 (SELECT/INSERT/UPDATE/DELETE)

DBA로부터 수령 후:
- DDL -> EF Core 마이그레이션으로 적용 (6. DB 작업 방법 참조)
- SELECT/INSERT 등 -> `Data/Logic/{Account|Game}DBLogic/` 에 Dapper 코드로 작성

**FK(Foreign Key) 사용 금지**: DDL에 `CONSTRAINT FK_...` 구문을 넣지 마세요. 참조 무결성은 애플리케이션 레이어에서 관리합니다.

---

## 6. DB 작업 방법

### Account DB vs Game DB

| 구분 | DB명 | DBContext | 용도 |
|------|------|-----------|------|
| Account DB | `accountdb` | `AccountDBContext` | 계정, 로그인 연동 정보 |
| Game DB | `gamedb` | `GameDBContext` | 유저 게임 데이터 (캐릭터, 아이템 등) |

### DataBaseManager API

모든 DB 접근은 `DataBaseManager`를 통해서만 가능합니다.

```csharp
// 단일 DB, 트랜잭션 없음
await _dataBaseManager.DBContextExcute(DataBaseManager.DBtype.Game, async (connection) =>
{
    var result = await DB.Data.Logic.GameDBLogic.UserInfoData.GetUserInfoByAccountId(connection, accountId);
});

// 단일 DB, 트랜잭션 있음 (bool 반환: true면 Commit, false면 Rollback)
await _dataBaseManager.DBContextExcuteTransaction(DataBaseManager.DBtype.Account, async (connection) =>
{
    long newId = await DB.Data.Logic.AccountDBLogic.AccountInfo.InsertAccountInfo(connection, newAccount);
    return newId > 0;  // true: 커밋, false: 롤백
});

// 두 DB 동시 트랜잭션
await _dataBaseManager.DBContextExcuteTransaction(
    DataBaseManager.DBtype.Account,
    DataBaseManager.DBtype.Game,
    async (accountConn, gameConn) =>
    {
        // 두 DB에 걸친 작업
        return true;
    });
```

### Dapper 쿼리 작성 위치

```
Data/Logic/AccountDBLogic/{기능명}.cs   <- Account DB 쿼리
Data/Logic/GameDBLogic/{기능명}.cs      <- Game DB 쿼리
```

Dapper 쿼리 작성 예시:

```csharp
// Data/Logic/GameDBLogic/UserInfoData.cs
public class UserInfoData
{
    public static async Task<UserInfo> GetUserInfoByAccountId(DbConnection gameConnection, long accountId)
    {
        var query = "SELECT * FROM userInfo WHERE accountId = @AccountId";
        return await gameConnection.QueryFirstOrDefaultAsync<UserInfo>(query, new { AccountId = accountId });
    }

    public static async Task<long> InsertUserInfo(DbConnection gameConnection, UserInfo userInfo)
    {
        var sql = @"
    INSERT INTO userInfo(nickName, accountId)
    VALUES (@NickName, @AccountId);
    SELECT LAST_INSERT_ID();";

        return await gameConnection.QuerySingleAsync<long>(sql, new
        {
            NickName = userInfo.nickName,
            AccountId = userInfo.accountId
        });
    }
}
```

### EF Core 마이그레이션 명령어 (전체)

```powershell
# [새 엔티티/컬럼 추가 후] 마이그레이션 생성
Add-Migration {마이그레이션명} -Context AccountDBContext -Project DB.Data -OutputDir "Migrations\AccountMigrations"
Add-Migration {마이그레이션명} -Context GameDBContext   -Project DB.Data -OutputDir "Migrations\GameMigrations"

# [확인] 실행될 SQL 스크립트 미리 보기
Script-Migration -From 0 -To {마이그레이션명} -Context AccountDBContext

# [적용] DB에 마이그레이션 실행
Update-Database -Context AccountDBContext -Project DB.Data -StartupProject FrogTailGameServer
Update-Database -Context GameDBContext   -Project DB.Data -StartupProject FrogTailGameServer

# [롤백] 이전 마이그레이션으로 되돌리기
Update-Database -Context AccountDBContext -Migration {이전마이그레이션명}
```

### 새 엔티티 추가 시 체크리스트

1. `Data/{Account|Game}DB/` 에 엔티티 클래스 생성 (`IModelCreateEntity` 구현)
2. `AccountDBContext` 또는 `GameDBContext`에 `DbSet<T>` 추가
3. 마이그레이션 생성 및 적용
4. `Data/Logic/` 에 Dapper 쿼리 클래스 작성
5. **FK 제약 조건 추가 금지**

---

## 7. 에러 코드 추가 방법

에러 코드는 두 곳을 동기화해서 관리합니다.

### Step 1: Share/Common/ErrorCode.cs 에 추가

```csharp
// Share/Common/ErrorCode.cs
public enum ErrorCode
{
    NONE = 0,
    SUCCESS = 1,
    CREATE_USER,
    UNKNOW_ERROR,
    INVAILD_PACKET_INFO,
    INVAILD_USER_TOKEN,
    INVAILD_NICK_NAME,

    // 매칭
    ALREADY_IN_MATCH_QUEUE,
    NOT_IN_MATCH_QUEUE,
    ALREADY_IN_GAME,

    // 게임 룸
    ROOM_NOT_FOUND,
    NOT_IN_ROOM,
    GAME_NOT_STARTED,
    GAME_ALREADY_ENDED,

    // 신규 추가 예시
    MY_NEW_ERROR_CODE,
}
```

### Step 2: FrogTailGameServer/Protos/common.proto 에 동기화

```protobuf
// FrogTailGameServer/Protos/common.proto
enum ErrorCode {
  Success = 0;
  CreateUser = 1;
  UnknowError = 2;
  InvalidPacketInfo = 3;
  InvalidUserToken = 4;
  InvalidNickName = 5;
  AlreadyInMatchQueue = 6;
  NotInMatchQueue = 7;
  AlreadyInGame = 8;
  RoomNotFound = 9;
  NotInRoom = 10;
  GameNotStarted = 11;
  GameAlreadyEnded = 12;

  // 신규 추가 (번호는 기존 마지막 번호 + 1)
  MyNewErrorCode = 13;
}
```

**주의사항:**
- `common.proto`의 번호는 한 번 배포된 후 절대 변경하지 않습니다. (Protobuf 하위 호환성)
- `Share/Common/ErrorCode.cs`의 값 순서와 `common.proto`의 번호가 의미상 일치해야 합니다.
- 값을 중간에 삽입하지 말고 항상 마지막에 추가합니다.

---

## 8. 테스트 방법

### TestClient (콘솔) 사용법

로그인부터 ShopList 조회까지 전체 시나리오를 자동으로 실행합니다.

```bash
# 서버 실행 후
cd TestClient
dotnet run
```

시나리오:
1. Guest 신규 로그인 (빈 AccessToken -> GuestToken 발급 확인)
2. ShopList 조회 (로그인 세션으로 인증 헤더 포함)
3. Guest 재로그인 (GuestToken으로 재로그인 -> 동일 UserId 확인)
4. ShopList 조회 (재로그인 세션 확인)

인증이 필요한 API 호출 시 헤더를 직접 추가하는 방법:

```csharp
var headers = new Metadata
{
    { "x-userid",      userId.ToString() },
    { "authorization", $"Bearer {userToken}" }
};
var response = await shopClient.ShopListAsync(new ShopListRequest(), headers);
```

### WpfTestClient (GUI) 사용법

Windows 환경에서 GUI로 각 API를 수동으로 호출할 수 있습니다.

```bash
cd WpfTestClient
dotnet run
```

### 단위 테스트 실행

```bash
dotnet test FrogTailGameServer.Tests/FrogTailGameServer.Tests.csproj
```

또는 솔루션 루트에서:

```bash
dotnet test
```

테스트 파일 위치: `FrogTailGameServer.Tests/GrpcAuthServiceTests.cs`

---

## 9. 클라이언트 적용 방법

### WpfTestClient (GUI 테스트 클라이언트)

#### 사전 조건

- Windows 환경 필수 (.NET 8 Windows TFM: `net8.0-windows`)
- 서버(`FrogTailGameServer`)가 먼저 실행되어 있어야 합니다.
- Visual Studio 2022 또는 .NET SDK 8.0 이상 설치 필요

#### 빌드 및 실행

Visual Studio에서:

1. `FrogTailGameServer.sln` 열기
2. 시작 프로젝트를 `WpfTestClient`로 변경
3. F5 또는 Ctrl+F5 실행

CLI에서:

```bash
cd WpfTestClient
dotnet run
```

#### WpfTestClient 화면 구성

| 영역 | 설명 |
|------|------|
| Server Address | gRPC 서버 주소 (기본값: `http://localhost:9001`) |
| DeviceId / NickName / LoginType / AccessToken | 로그인 파라미터 입력 |
| Login 버튼 | 단일 로그인 요청 실행 |
| GetShopList 버튼 | 로그인 후 인증 헤더를 포함한 ShopList 조회 |
| 시나리오 구성 버튼 | 패킷 목록 팝업에서 순서 지정 후 일괄 실행 |
| Request / Response Log | 요청/응답 내용 실시간 표시 |

#### Guest 신규 로그인 시나리오 (WpfTestClient)

1. LoginType = `Guest`, AccessToken = 빈 값으로 Login 실행
2. Response Log에서 GuestToken 확인 → AccessToken 필드에 자동 채움
3. 이후 로그인 시 자동 채워진 GuestToken을 그대로 사용하면 동일 UserId로 재로그인

---

### TestClient (콘솔 통합 테스트)

#### 사전 조건

- 서버(`FrogTailGameServer`)가 먼저 실행되어 있어야 합니다.
- 운영 환경이 아닌 `Development` 환경을 권장합니다. (userId 평문 전송 허용)

#### 실행

```bash
cd TestClient
dotnet run
```

#### 자동 실행 시나리오

TestClient는 아래 4개 시나리오를 자동으로 순차 실행합니다:

| 단계 | 내용 | 확인 포인트 |
|------|------|------------|
| 1/4 | Guest 신규 로그인 (`AccessToken=""`) | GuestToken 발급 여부, UserId > 0 |
| 2/4 | ShopList 조회 (신규 세션) | ErrorCode = Success |
| 3/4 | Guest 재로그인 (`AccessToken=GuestToken`) | 동일 UserId 반환, GuestToken 미발급 |
| 4/4 | ShopList 조회 (재로그인 세션) | ErrorCode = Success |

---

### 서버-클라이언트 연결 확인 방법

1. 서버 기동 후 아래 로그 확인:

```
[yyyy-MM-dd HH:mm:ss INF] Now listening on: http://0.0.0.0:9001
```

2. TestClient 또는 WpfTestClient 실행 후 시나리오 1 (Login) 결과 확인:

```
[1/4] ErrorCode:  Success
[1/4] UserId:     1234
[1/4] GuestToken: eyJ...
```

3. 시나리오 2 (ShopList) 결과 확인:

```
  ErrorCode: Success
  ShopCount: N
```

---

### gRPC 패킷 추가 시 클라이언트에서 해야 할 작업

새 API(예: `InventoryService.GetInventory`)를 추가했을 때 클라이언트 3곳을 수정합니다.

#### Step 1: PacketItem.All에 등록

`WpfTestClient/Models/PacketItem.cs`

```csharp
public static IReadOnlyList<PacketItem> All { get; } =
[
    new PacketItem { ServiceName = "LoginService",     RpcName = "Login",        RequiresAuth = false },
    new PacketItem { ServiceName = "LoginService",     RpcName = "VerityLogin",  RequiresAuth = false },
    new PacketItem { ServiceName = "ShopService",      RpcName = "ShopList",     RequiresAuth = true  },
    // 신규 추가
    new PacketItem { ServiceName = "InventoryService", RpcName = "GetInventory", RequiresAuth = true  },
];
```

- `RequiresAuth = true`이면 시나리오 실행 시 세션이 없을 경우 자동으로 스킵됩니다.

#### Step 2: GrpcClientService에 메서드 추가

`WpfTestClient/Services/GrpcClientService.cs`

```csharp
private InventoryService.InventoryServiceClient GetInventoryClient()
    => new(GetOrCreateChannel());

public async Task<GetInventoryResponse> GetInventoryAsync(CancellationToken ct = default)
{
    var headers = BuildAuthHeaders();
    return await GetInventoryClient().GetInventoryAsync(
        new GetInventoryRequest(), headers, cancellationToken: ct);
}
```

#### Step 3: MainViewModel의 ExecutePacketAsync에 case 추가

`WpfTestClient/ViewModels/MainViewModel.cs`

```csharp
case "InventoryService.GetInventory":
{
    var response = await _grpcService.GetInventoryAsync();
    AppendResponse($"  ErrorCode: {response.ErrorCode}, ItemCount: {response.Items.Count}");
    break;
}
```

단일 버튼으로 직접 실행하려면 추가로 `[RelayCommand]` 메서드를 작성하고 XAML에 버튼을 바인딩합니다.

---

### 트러블슈팅

#### 포트 연결 오류

```
gRPC Error: Unavailable — failed to connect
```

- 서버가 실행 중인지 확인: `http://localhost:9001`
- 방화벽/포트 차단 여부 확인
- WpfTestClient의 Server Address 필드가 `http://localhost:9001` 인지 확인 (https 아님)

#### 채널 주소 변경 후 연결 안 됨

- WpfTestClient: Server Address 변경 후 반드시 "채널 재구성" 버튼 클릭
- TestClient: `Program.cs`의 `ServerAddress` 상수를 직접 수정하고 재빌드

#### 인증 오류 (gRPC Status: Unauthenticated)

```
gRPC Error: Unauthenticated
```

- 로그인(Login) 먼저 실행한 뒤 인증이 필요한 API 호출 필요
- `Development` 환경에서는 userId를 평문으로 전송 (`x-userid: 1234`)
- `Production` 환경에서는 `SecretManager.EncryptString(userId)` 암호화 값을 전송

#### proto 파일 변경 후 클라이언트 빌드 오류

- 서버에서 `generate-proto.bat` 실행 후 생성된 `.proto` 파일을 클라이언트 프로젝트의 `Protos/` 디렉토리에도 복사
- 클라이언트 `.csproj`에 `<Protobuf Include="Protos\*.proto" GrpcServices="Client" />` 등록 확인
- 빌드 후 자동 생성된 stub 클래스(`*Grpc.cs`)를 확인하고 신규 클라이언트 코드 작성

---

## 10. 멀티 클라이언트 Proto 적용 가이드

서버에서 생성한 `.proto` 파일을 Unity, Unreal, Python, Java 등 다양한 클라이언트에 적용하는 방법을 설명합니다.

### Proto 파일 배포

서버에서 `generate-proto.bat`을 실행하면 `Protos/*.proto` 파일이 생성됩니다.
클라이언트는 이 파일을 **그대로** 복사해서 사용합니다.

```
gameServer/Protos/
├── auth.proto      → 클라이언트 Protos/ 폴더에 복사
├── shop.proto
└── common.proto
```

> **주의**: proto 파일 내부의 `import "Protos/common.proto"` 경로는 수정하지 않고,
> 컴파일 시 `--proto_path` 옵션으로 경로를 맞춰줍니다.

---

### 언어별 필드 접근 방식

proto 필드는 `snake_case`로 정의되지만, 각 언어의 protoc 플러그인이 자동으로 변환합니다.

| proto 필드 | C# (Unity) | C++ (Unreal) | Python | Java |
|---|---|---|---|---|
| `device_id` | `DeviceId` | `device_id()` / `set_device_id()` | `device_id` | `getDeviceId()` / `setDeviceId()` |
| `user_token` | `UserToken` | `user_token()` | `user_token` | `getUserToken()` |
| `error_code` | `ErrorCode` | `error_code()` | `error_code` | `getErrorCode()` |

---

### Unity (C#)

**패키지 설치** (`Packages/manifest.json`)

```json
{
  "dependencies": {
    "com.github.grpc.grpc-unity": "2.51.0"
  }
}
```

> iOS/Android 빌드 시 `YetAnotherHttpHandler` 라이브러리를 추가로 설치해야 합니다.

**proto 컴파일** (Windows)

```bash
protoc --proto_path=. --proto_path=Protos \
       --csharp_out=Assets/Scripts/Proto \
       --grpc_out=Assets/Scripts/Proto \
       --plugin=protoc-gen-grpc=grpc_csharp_plugin.exe \
       Protos/auth.proto Protos/common.proto
```

**사용 예시**

```csharp
// C# — PascalCase 자동 변환
var channel = GrpcChannel.ForAddress("https://서버IP:9001");
var client  = new Auth.AuthClient(channel);

var response = await client.LoginAsync(new LoginRequest {
    DeviceId  = SystemInfo.deviceUniqueIdentifier,
    NickName  = "플레이어1",
    LoginType = LoginTypeEnum.Guest
});

if (response.ErrorCode == ErrorCodeEnum.Success)
    Debug.Log($"로그인 성공, UserId={response.UserId}");
```

---

### Unreal Engine (C++)

**패키지 설치** (vcpkg)

```bash
vcpkg install grpc:x64-windows
```

**proto 컴파일**

```bash
protoc --proto_path=. --proto_path=Protos \
       --cpp_out=Source/Proto \
       --grpc_out=Source/Proto \
       --plugin=protoc-gen-grpc=grpc_cpp_plugin \
       Protos/auth.proto Protos/common.proto
```

**사용 예시**

```cpp
// C++ — snake_case getter/setter 사용
// !! gRPC 블로킹 호출은 반드시 Game Thread가 아닌 백그라운드 스레드에서 실행
AsyncTask(ENamedThreads::AnyBackgroundThreadNormalTask, [this]()
{
    LoginRequest req;
    req.set_device_id("device-123");
    req.set_nick_name("플레이어1");
    req.set_login_type(LOGIN_TYPE_GUEST);

    LoginResponse res;
    grpc::ClientContext ctx;
    auto stub = Auth::NewStub(channel_);
    grpc::Status status = stub->Login(&ctx, req, &res);

    if (status.ok() && res.error_code() == ERROR_CODE_SUCCESS)
    {
        int64_t userId = res.user_id();
        AsyncTask(ENamedThreads::GameThread, [userId]()
        {
            UE_LOG(LogTemp, Log, TEXT("로그인 성공 UserId=%lld"), userId);
        });
    }
});
```

---

### Python

**패키지 설치**

```bash
pip install grpcio grpcio-tools
```

**proto 컴파일**

```bash
python -m grpc_tools.protoc \
    --proto_path=. --proto_path=Protos \
    --python_out=client \
    --grpc_python_out=client \
    Protos/auth.proto Protos/common.proto
```

**사용 예시**

```python
# Python — snake_case 직접 접근
import grpc
import client.auth_pb2 as auth_pb2
import client.auth_pb2_grpc as auth_grpc

channel = grpc.insecure_channel('서버IP:9001')
stub    = auth_grpc.AuthStub(channel)

req = auth_pb2.LoginRequest(
    device_id  = "device-123",
    nick_name  = "플레이어1",
    login_type = auth_pb2.LOGIN_TYPE_GUEST
)
res = stub.Login(req)

if res.error_code == auth_pb2.ERROR_CODE_SUCCESS:
    print(f"로그인 성공, user_id={res.user_id}")
```

---

### Java / Kotlin

**Gradle 설정** (`build.gradle`)

```groovy
plugins {
    id "com.google.protobuf" version "0.9.4"
}

dependencies {
    implementation "io.grpc:grpc-okhttp:1.62.0"
    implementation "io.grpc:grpc-protobuf-lite:1.62.0"
    implementation "io.grpc:grpc-stub:1.62.0"
}

protobuf {
    protoc { artifact = "com.google.protobuf:protoc:3.25.1" }
    plugins { grpc { artifact = "io.grpc:protoc-gen-grpc-java:1.62.0" } }
    generateProtoTasks {
        all()*.plugins { grpc {} }
    }
    sourceSets.main.proto.srcDirs = ["src/main/proto", "src/main/proto/Protos"]
}
```

**사용 예시** (Kotlin)

```kotlin
// Java/Kotlin — camelCase getter/builder 사용
val channel = ManagedChannelBuilder.forAddress("서버IP", 9001)
    .usePlaintext().build()
val stub = AuthGrpc.newBlockingStub(channel)

val req = LoginRequest.newBuilder()
    .setDeviceId("device-123")
    .setNickName("플레이어1")
    .setLoginType(LoginTypeEnum.GUEST)
    .build()

val res = stub.login(req)
if (res.errorCode == ErrorCodeEnum.SUCCESS)
    println("로그인 성공, userId=${res.userId}")
```

---

### import 경로 문제 해결

서버 proto 파일에 `import "Protos/common.proto"`가 있는 경우,
`--proto_path`를 두 개 지정해 경로를 해결합니다.

```bash
# 올바른 방법 — proto 파일을 수정하지 않고 경로만 추가
protoc \
  --proto_path=.       \   # 프로젝트 루트
  --proto_path=Protos  \   # import 없는 단독 파일도 찾을 수 있도록
  --csharp_out=출력폴더 \
  Protos/auth.proto Protos/common.proto
```

> proto 파일 자체는 절대 수정하지 않습니다.
> 서버가 업데이트하면 클라이언트는 파일을 복사하기만 하면 됩니다.

---

## 11. 브랜치 전략 / 개발 워크플로우

### 브랜치 명명 규칙

```
feature/{기능명}    # 신규 기능 개발
bugfix/{이슈명}     # 버그 수정
chore/{작업명}      # 설정, 패키지 업데이트, 리팩토링 외 잡무
refactor/{대상}     # 코드 구조 개선
```

### Conventional Commits 형식

```
<type>(<scope>): <subject>

<body>  (선택)
```

| type | 용도 |
|------|------|
| `feat` | 신규 기능 |
| `fix` | 버그 수정 |
| `refactor` | 코드 구조 개선 (기능 변경 없음) |
| `chore` | 빌드/설정 변경 |
| `test` | 테스트 추가/수정 |
| `docs` | 문서 수정 |

예시:

```
feat(auth): Guest 재로그인 시 동일 UserId 반환

빈 AccessToken으로 최초 로그인 시 서버에서 GuestToken 발급.
재로그인 시 GuestToken을 AccessToken으로 사용하여 동일 계정 연속성 보장.
```

### Claude Code 에이전트 워크플로우

이 프로젝트는 Claude Code 에이전트 팀으로 기능을 개발합니다.

```
CTO -> 시니어 개발자 -> 개발자(A/B) -> [DBA] -> QA
```

신규 기능 개발 시 Claude Code에서 `/feature` 명령어를 사용합니다:

```
/feature {기능 설명}
```

예시:

```
/feature 인벤토리 조회 API 추가 (userId로 보유 아이템 목록 반환)
```

명령어 실행 시 자동으로 다음 순서가 진행됩니다:

1. **CTO** - 기술 타당성 검토 및 구현 방향 결정
2. **시니어 개발자** - gRPC 서비스 구조 설계, 클래스 구조 설계
3. **개발자(A 또는 B)** - feature 브랜치 생성, 패킷/서비스 구현
   - DB 작업 있으면 **DBA** 협업 (DDL + Dapper 쿼리)
4. **QA** - 테스트 시나리오 검증, 배포 전 체크리스트

QA 미승인 시 개발자에게 피드백 -> 재작업 -> QA 재검증 반복.

에이전트 정의 파일 위치: `.claude/agents/`

---

## 12. 코딩 컨벤션

신규 코드 작성 전 반드시 숙지하고 PR 제출 전 자기 검증을 완료하세요.

### async/await

모든 I/O 작업은 `async/await`를 사용합니다.

```csharp
// 올바른 예시
public override async Task<LoginResponse> Login(LoginRequest request, ServerCallContext context)
{
    var result = await _dataBaseManager.DBContextExcuteTransaction(...);
}

// 금지 - 블로킹 호출
var result = _dataBaseManager.DBContextExcuteTransaction(...).Result;
```

`.ConfigureAwait(false)`는 **사용하지 않습니다**. ASP.NET Core / gRPC 환경에서는 `SynchronizationContext`가 없으므로 불필요합니다.

### Guard Clause 패턴

중첩을 최소화하기 위해 조건이 맞지 않으면 조기 반환합니다.

```csharp
// 올바른 예시 (Guard Clause)
public override async Task<LoginResponse> Login(LoginRequest request, ServerCallContext context)
{
    if (string.IsNullOrEmpty(request.NickName))
    {
        return new LoginResponse { ErrorCode = ErrorCode.InvalidNickName };
    }

    if (!IsValidOsType(request.OsType))
    {
        return new LoginResponse { ErrorCode = ErrorCode.InvalidPacketInfo };
    }

    // 메인 로직
}

// 금지 - do-while(false) 안티패턴
do
{
    if (string.IsNullOrEmpty(request.NickName)) break;
} while (false);
```

### DI 원칙

```csharp
// 올바른 예시 - 생성자 주입
public class GrpcShopService : ShopService.ShopServiceBase
{
    private readonly DataBaseManager _dataBaseManager;

    public GrpcShopService(DataBaseManager dataBaseManager)
    {
        _dataBaseManager = dataBaseManager;
    }
}

// 금지 - static GetInstance() 패턴
public static DataBaseManager GetInstance() => _instance;
```

### 예외 처리

예외를 삼키지 않습니다. 반드시 로그를 남기고 에러코드를 반환하거나 재throw합니다.

```csharp
// 올바른 예시
try
{
    await SomeOperationAsync();
}
catch (Exception ex)
{
    _logger.LogError(ex, "[GrpcShopService] ShopList 처리 중 오류 발생");
    return new ShopListResponse { ErrorCode = ErrorCode.UnknowError };
}

// 금지 - 예외 삼키기
try { ... }
catch { }
```

### Nullable 처리

`#pragma warning disable nullable` 또는 `<NoWarn>`으로 경고를 숨기지 않습니다.

```csharp
// 올바른 예시 - 명시적 null 처리
string? userToken = await GetUserTokenAsync();
if (userToken is null)
{
    return new LoginResponse { ErrorCode = ErrorCode.InvalidUserToken };
}

// 금지 - null-forgiving 남용
string userToken = await GetUserTokenAsync()!;
```

### FK 사용 금지

DDL에 Foreign Key 제약 조건을 추가하지 않습니다. 참조 무결성은 애플리케이션에서 관리합니다.

```sql
-- 금지
CREATE TABLE userInfo (
    accountId BIGINT NOT NULL,
    CONSTRAINT FK_userInfo_account FOREIGN KEY (accountId) REFERENCES account(accountId)
);

-- 올바름
CREATE TABLE userInfo (
    accountId BIGINT NOT NULL
    -- FK 없음, 앱에서 무결성 보장
);
```

### 서버 권한 원칙

클라이언트에서 전달된 데이터를 신뢰하지 않습니다. 모든 검증은 서버에서 수행합니다.

```csharp
// 올바른 예시 - 서버에서 LoginType 검증
var resolvedType = await FireBase.GetLoginProviderAsync(accessToken);
if (resolvedType != loginType)
{
    return new LoginResponse { ErrorCode = ErrorCode.InvalidPacketInfo };
}
```

### PR 제출 전 자기 검증 체크리스트

- [ ] 모든 I/O가 `async/await`로 처리되었는가? (`.ConfigureAwait(false)` 사용 금지)
- [ ] `catch` 블록이 예외를 삼키지 않는가? (로그 + 에러코드 반환 또는 재throw)
- [ ] gRPC Service가 DataBaseManager를 통해서만 DB에 접근하는가?
- [ ] Nullable 경고가 `NoWarn`으로 숨겨지지 않고 명시적으로 처리되었는가?
- [ ] DI 컨테이너로 등록해야 할 클래스가 `static`으로 선언되지 않았는가?
- [ ] 새 gRPC 서비스 추가 시 `generate-proto.bat`을 실행했는가?
- [ ] `Program.cs`에 `app.MapGrpcService<T>()`가 등록되었는가?
- [ ] DDL에 `CONSTRAINT FK_...` 구문이 없는가?
- [ ] 모든 검증이 서버에서 수행되는가? (클라이언트 데이터 신뢰 금지)

---

*FrogTailGameServer 팀에 오신 것을 환영합니다. 궁금한 점은 시니어 개발자 또는 팀 채널에 질문하세요.*
