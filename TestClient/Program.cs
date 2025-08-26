using Newtonsoft.Json;
using ProtoBuf;
using System;
using System.Net.Http;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

class Program
{
	public enum ErrrorCode
	{
		NONE = 0,
		SUCCESS = 1,
		INVAILD_PACKET_INFO = 2,
	}
	public class PacketReqeustBase
	{
		public PacketReqeustBase(PacketId packetId)
		{
			RequestId = packetId;
			PacketBody = String.Empty;
		}
		public PacketId RequestId;
		public string PacketBody;
	}
	public class PacketAnsPacket
	{
		public PacketAnsPacket()
		{

		}
		public ErrrorCode ErrorCode;
	}
	public enum PacketId
	{
		None = 0,
		CG_Login_Req_Packet_Id = 1,
		GC_Login_Ans_Packet_Id = 2,

	}

	public class CGLoginReqPacket : PacketReqeustBase
	{
		public CGLoginReqPacket() : base(PacketId.CG_Login_Req_Packet_Id)
		{

		}
		public string UserToken { get; set; }
	}
	public class GCLoginAnsPacket : PacketAnsPacket
	{
		public GCLoginAnsPacket() 
		{

		}
		public long UserId { get; set; }
	}

	private static readonly HttpClient httpClient = new HttpClient();

	static async Task Main(string[] args)
	{
		try
		{
			CGLoginReqPacket request = new CGLoginReqPacket();
			request.UserToken = "hello";
			await SendPostRequestAsync(request);
		}
		catch (Exception ex)
		{
			Console.WriteLine($"Error: {ex.Message}");
		}
	}
	public static string authHeader = "";

	private static async Task SendPostRequestAsync(PacketReqeustBase packet)
	{
		string url = "http://119.69.50.205:9000/Packet";
		HttpClient httpClient = new HttpClient();
		while (true)
		{
			PacketReqeustBase sendPacket = packet;
			sendPacket.PacketBody = JsonConvert.SerializeObject(packet);


			var json = JsonConvert.SerializeObject(sendPacket);

			// HTTP 요청 설정
			var content = new StringContent(json, Encoding.UTF8, "application/json");
			if(string.IsNullOrEmpty(authHeader) == false)
			{
				
			}
			httpClient.DefaultRequestHeaders.Add("Authorization", "456465465");

			// HTTP POST 요청 보내기
			var response = httpClient.PostAsync(url, content).Result;

			if (response.IsSuccessStatusCode)
			{
				// 응답 처리
				string responseJson = response.Content.ReadAsStringAsync().Result;
				IEnumerable<string> test = null;
				response.Headers.TryGetValues("Authorization", out test);
				if(test == null || test.Count() <= 0)
				{
					// 여기서 재로그인 태우거나 해야될듯
					return;
				}

				authHeader = test.FirstOrDefault();


				Console.WriteLine($"Received Data: {responseJson}");
			}
			else
			{
				Console.WriteLine($"Error: {response.StatusCode}");
			}
		}
		
	}
}