using System.Text;
using System.Text.RegularExpressions;

// ──────────────────────────────────────────────
//  경로 설정
//  args[0] = Packet 루트 (없으면 기본값)
//  args[1] = Proto 출력 경로 (없으면 기본값)
// ──────────────────────────────────────────────
var baseDir = AppContext.BaseDirectory;

var packetRootPath = args.Length > 0
    ? args[0]
    : Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", "..", "Share", "Packet"));

var protoOutputPath = args.Length > 1
    ? args[1]
    : Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", "..", "FrogTailGameServer", "Protos"));

Console.WriteLine("=== PacketToProtoGenerator ===");
Console.WriteLine($"  Packet 경로 : {packetRootPath}");
Console.WriteLine($"  Proto 출력  : {protoOutputPath}");
Console.WriteLine();

if (!Directory.Exists(packetRootPath))
{
    Console.Error.WriteLine($"[ERROR] Packet 경로를 찾을 수 없습니다: {packetRootPath}");
    return 1;
}

Directory.CreateDirectory(protoOutputPath);

// ──────────────────────────────────────────────
//  스킵 파일 (베이스/유틸 파일)
// ──────────────────────────────────────────────
var skipFileNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
{
    "PacketRequestBase.cs",
    "PacketId.cs",
    "Class1.cs",
};

// ──────────────────────────────────────────────
//  C# 타입 → Proto 타입 매핑
// ──────────────────────────────────────────────
var primitiveTypeMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
{
    { "int",    "int32"  },
    { "long",   "int64"  },
    { "uint",   "uint32" },
    { "ulong",  "uint64" },
    { "short",  "int32"  },
    { "ushort", "uint32" },
    { "float",  "float"  },
    { "double", "double" },
    { "bool",   "bool"   },
    { "string", "string" },
    { "byte",   "bytes"  },
};

var generatedFiles = new List<string>();

// 하위 폴더 처리 (폴더명 → proto 파일 1:1 매핑)
foreach (var dir in Directory.GetDirectories(packetRootPath))
    ProcessDirectory(dir);

Console.WriteLine();
Console.WriteLine($"완료: {generatedFiles.Count}개 파일 생성됨");
foreach (var f in generatedFiles)
    Console.WriteLine($"  → {f}");

return 0;

// ======================================================
//  로컬 함수
// ======================================================

void ProcessDirectory(string dir)
{
    var csFiles = Directory.GetFiles(dir, "*.cs")
        .Where(f => !skipFileNames.Contains(Path.GetFileName(f)))
        .ToList();

    if (!csFiles.Any()) return;

    var dirName     = Path.GetFileName(dir);
    var protoName   = DirToProtoFileName(dirName);
    var svcName     = DirToServiceName(dirName);
    var packageName = Path.GetFileNameWithoutExtension(protoName);

    var allClasses  = csFiles.SelectMany(ParseCsFile).ToList();
    if (!allClasses.Any()) return;

    var content    = GenerateProto(allClasses, svcName, packageName);
    var outputPath = Path.Combine(protoOutputPath, protoName);

    File.WriteAllText(outputPath, content, new UTF8Encoding(false));
    generatedFiles.Add(protoName);

    Console.WriteLine($"[생성] {protoName}  (클래스 {allClasses.Count}개)");
}

// ──────────────────────────────────────────────
//  C# 파일 파싱 (Roslyn)
// ──────────────────────────────────────────────
List<PacketClass> ParseCsFile(string filePath)
{
    var code = File.ReadAllText(filePath);
    var root = CSharpSyntaxTree.ParseText(code).GetRoot();

    return root.DescendantNodes()
        .OfType<ClassDeclarationSyntax>()
        .Select(cls =>
        {
            var baseRaw  = cls.BaseList?.Types.FirstOrDefault()?.ToString() ?? "";
            var baseName = Regex.Replace(baseRaw, @"<.*>", "").Trim();

            var props = cls.Members
                .OfType<PropertyDeclarationSyntax>()
                .Select(p => new PacketProperty(p.Identifier.Text, p.Type.ToString().Trim()))
                .ToList();

            return new PacketClass(cls.Identifier.Text, baseName, props);
        })
        .ToList();
}

// ──────────────────────────────────────────────
//  proto 파일 생성
// ──────────────────────────────────────────────
string GenerateProto(List<PacketClass> classes, string serviceName, string packageName)
{
    var dataClasses = classes.Where(c => !IsCg(c.ClassName) && !IsGc(c.ClassName)).ToList();
    var cgPackets   = classes.Where(c => IsCg(c.ClassName)).ToList();
    var gcPackets   = classes.Where(c => IsGc(c.ClassName)).ToList();

    var sb = new StringBuilder();

    // 헤더
    sb.AppendLine("syntax = \"proto3\";");
    sb.AppendLine("import \"Protos/common.proto\";");
    sb.AppendLine("option csharp_namespace = \"FrogTailGameServer.Grpc\";");
    sb.AppendLine($"package {packageName};");
    sb.AppendLine();

    // 1) 데이터 메시지 (CG/GC 아닌 순수 데이터 클래스)
    foreach (var cls in dataClasses)
    {
        WriteMessage(sb, cls.ClassName, cls.Properties, isResponse: false);
        sb.AppendLine();
    }

    // 2) Request 메시지
    foreach (var cg in cgPackets)
    {
        WriteMessage(sb, CgToRequestName(cg.ClassName), cg.Properties, isResponse: false);
        sb.AppendLine();
    }

    // 3) Response 메시지 (ErrorCode 자동 삽입)
    foreach (var gc in gcPackets)
    {
        WriteMessage(sb, GcToResponseName(gc.ClassName), gc.Properties, isResponse: true);
        sb.AppendLine();
    }

    // 4) Service 블록
    if (cgPackets.Any())
    {
        sb.AppendLine($"service {serviceName} {{");
        foreach (var cg in cgPackets)
        {
            var rpcName  = ExtractRpcName(cg.ClassName);
            var reqName  = CgToRequestName(cg.ClassName);
            var gcMatch  = gcPackets.FirstOrDefault(g => ExtractRpcName(g.ClassName) == rpcName);
            var respName = gcMatch != null
                ? GcToResponseName(gcMatch.ClassName)
                : reqName.Replace("Request", "Response");

            sb.AppendLine($"  rpc {rpcName} ({reqName}) returns ({respName});");
        }
        sb.AppendLine("}");
    }

    return sb.ToString();
}

void WriteMessage(StringBuilder sb, string msgName, List<PacketProperty> props, bool isResponse)
{
    sb.AppendLine($"message {msgName} {{");
    var idx = 1;

    if (isResponse)
        sb.AppendLine($"  ErrorCode error_code = {idx++};");

    foreach (var prop in props)
    {
        var (protoType, repeated) = MapType(prop.TypeName);
        var fieldName = ToSnakeCase(prop.Name);
        var prefix    = repeated ? "repeated " : "";
        sb.AppendLine($"  {prefix}{protoType} {fieldName} = {idx++};");
    }

    sb.AppendLine("}");
}

// ──────────────────────────────────────────────
//  타입 변환
// ──────────────────────────────────────────────
(string protoType, bool repeated) MapType(string csType)
{
    csType = csType.Trim();

    // List<T> / IList<T> / IEnumerable<T> → repeated
    var listMatch = Regex.Match(csType, @"^(?:List|IList|IEnumerable|ICollection)<(.+)>$");
    if (listMatch.Success)
    {
        var (inner, _) = MapType(listMatch.Groups[1].Value);
        return (inner, true);
    }

    // nullable T?
    if (csType.EndsWith("?"))
        csType = csType[..^1];

    if (primitiveTypeMap.TryGetValue(csType, out var mapped))
        return (mapped, false);

    // enum / 커스텀 클래스 → 그대로
    return (csType, false);
}

// ──────────────────────────────────────────────
//  이름 변환 헬퍼
// ──────────────────────────────────────────────
bool IsCg(string name) =>
    name.StartsWith("CG", StringComparison.Ordinal) && name.EndsWith("ReqPacket", StringComparison.Ordinal);

bool IsGc(string name) =>
    name.StartsWith("GC", StringComparison.Ordinal) && name.EndsWith("AnsPacket", StringComparison.Ordinal);

string CgToRequestName(string name)  => name[2..^"ReqPacket".Length] + "Request";
string GcToResponseName(string name) => name[2..^"AnsPacket".Length] + "Response";

string ExtractRpcName(string name)
{
    if (IsCg(name)) return name[2..^"ReqPacket".Length];
    if (IsGc(name)) return name[2..^"AnsPacket".Length];
    return name;
}

// PascalCase → snake_case
string ToSnakeCase(string name) =>
    Regex.Replace(name, @"(?<!^)([A-Z])", "_$1").ToLower();

// LoginPacket → login.proto,  ShopPacket → shop.proto
string DirToProtoFileName(string dirName) =>
    Regex.Replace(dirName, "Packet$", "", RegexOptions.IgnoreCase).ToLower() + ".proto";

// LoginPacket → LoginService,  ShopPacket → ShopService
string DirToServiceName(string dirName) =>
    Regex.Replace(dirName, "Packet$", "", RegexOptions.IgnoreCase) + "Service";

// ======================================================
//  모델 (top-level statements 아래에 위치)
// ======================================================
record PacketProperty(string Name, string TypeName);

record PacketClass(
    string ClassName,
    string BaseTypeName,
    List<PacketProperty> Properties);
