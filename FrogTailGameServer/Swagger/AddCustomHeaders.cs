using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
namespace FrogTailGameServer.Swagger
{

	public class AddCustomHeaders : IOperationFilter
	{
		public void Apply(OpenApiOperation operation, OperationFilterContext context)
		{
			if (operation.Parameters == null)
				operation.Parameters = new List<OpenApiParameter>();

			// Authorization 헤더
			operation.Parameters.Add(new OpenApiParameter
			{
				Name = "Authorization",
				In = ParameterLocation.Header,
				Required = true,
				Description = "Bearer 토큰"
			});

			// X-UserId 헤더
			operation.Parameters.Add(new OpenApiParameter
			{
				Name = "X-UserId",
				In = ParameterLocation.Header,
				Required = true,
				Description = "사용자 ID"
			});
		}
	}

}
