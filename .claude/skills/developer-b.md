# 🛠 C# Developer Skills & Best Practices

C# 및 .NET 환경에서 고품질의 코드를 작성하고 유지보수하기 위해 준수해야 할 기술 스택 및 원칙 가이드입니다.

---

## 1. 코딩 컨벤션 (Naming & Style)
가독성은 협업의 기본입니다. [Microsoft 공식 가이드라인](https://learn.microsoft.com/dotnet/csharp/fundamentals/coding-style/coding-conventions)을 지향합니다.

.
    
* **PascalCase**: `Class`, `Method`, `Property`, `Public Field`, `Enum` 명칭에 사용합니다.
* **camelCase**: `parameter`, `localVariable`에 사용합니다.
* 예: `var userCount = users.Count();`
* 예: `public class A { int a; int apple; int userInfo;}`
* **_camelCase**: `private field`는 언더스코어(`_`)로 시작하여 구분합니다.
* **Interface**: 반드시 `I` 접두사를 사용합니다. (예: `IUserService`)
* **File Scoped Namespaces**: C# 10부터 도입된 파일 범위 네임스페이스를 사용하여 들여쓰기 깊이를 줄입니다.

---

## 2. 필수 C# 핵심 기술 (Modern C#)
최신 C# 버전의 기능을 활용하여 더 안전하고 간결한 코드를 작성합니다.

* **Async/Await**: I/O 바운드 작업(DB, Network)은 반드시 비동기로 처리합니다.
    * `async void` 대신 `async Task`를 사용하세요. (이벤트 핸들러 제외)
* **LINQ (Language Integrated Query)**: 복잡한 반복문 대신 선언적인 데이터 처리를 지향합니다.
* **Nullable Reference Types**: `null` 가능성을 명시적으로 제어하여 `NullReferenceException`을 방지합니다.
* **Pattern Matching**: `switch` 표현식과 `is` 연산자를 활용해 조건 로직을 간소화합니다.
* **Record types**: 불변(Immutable) 데이터 모델링 시 `record`를 적극 활용합니다.

---

## 3. 설계 원칙 및 패턴 (Design)
유연하고 확장이 용이한 구조를 위해 다음 원칙을 준수합니다.

* **SOLID 원칙**:
    * **S**: 하나의 클래스는 하나의 책임만 가집니다.
    * **O**: 확장에는 열려 있고 수정에는 닫혀 있어야 합니다.
    * **L**: 자식은 부모의 역할을 온전히 수행할 수 있어야 합니다.
    * **I**: 사용하지 않는 인터페이스를 강제로 구현하지 않습니다.
    * **D**: 구체 클래스가 아닌 인터페이스(추상화)에 의존합니다.
* **의존성 주입 (Dependency Injection)**: 생성자 주입을 통해 결합도를 낮추고 테스트 가능성을 높입니다.
* **DRY (Don't Repeat Yourself)**: 중복 코드를 지양하고 공통 로직은 캡슐화합니다.

---

## 4. 자원 관리 및 성능 (Performance)
관리되는 환경(Managed)이지만 효율적인 자원 사용은 필수입니다.

* **IDisposable & Using**: 파일, DB 커넥션 등 비관리 자원을 사용하면 반드시 `using` 문이나 `Dispose()`를 통해 즉시 해제합니다.
* **StringBuilder**: 루프 내 문자열 결합은 반드시 `StringBuilder`를 사용합니다.
* **ValueType vs ReferenceType**: 성능이 민감한 구간에서는 `struct`와 `class`의 차이를 이해하고 적절히 선택합니다.
* **Any() vs Count()**: 데이터 존재 여부만 확인할 때는 `Count() == 0`보다 `Any()`가 성능상 유리한 경우가 많습니다.

---

## 5. 테스트 및 품질 (Quality)
신뢰할 수 있는 소프트웨어를 위해 품질 관리에 투자합니다.

* **Unit Testing**: xUnit, NUnit 등을 사용하여 핵심 비즈니스 로직에 대한 단위 테스트를 작성합니다.
* **Exception Handling**: 예외는 구체적으로 잡고(`catch (Exception)` 지양), 의미 있는 정보를 포함하여 상위로 전파하거나 로깅합니다.
* **Logging**: `ILogger`를 사용하여 중요한 흐름과 에러를 기록하며, 적절한 LogLevel(Information, Warning, Error)을 지정합니다.

---

## 6. 도구 및 환경 (Tooling)
* **EditorConfig**: 팀원 간 동일한 코드 스타일 유지를 위해 `.editorconfig` 파일을 프로젝트에 포함합니다.
* **NuGet**: 패키지 의존성을 명확히 관리하고 보안 취약점이 있는 라이브러리는 주기적으로 업데이트합니다.
* **Git**: 의미 있는 단위로 커밋하고, 명확한 커밋 메시지 규칙을 따릅니다.

## 7. 코드 규칙
사용금지 ConfigureAwait(false); 
변수명 패턴은 camelCase 로 진행한다.

