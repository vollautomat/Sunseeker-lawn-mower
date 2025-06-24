using Sunseeker.Server.Services;

namespace Sunseeker.Server
{
	public class Program
	{
		public static void Main(string[] args)
		{
			var builder = WebApplication.CreateBuilder( args );

			// Add services to the container.

			var services = builder.Services;

			services.AddCors( options =>
			{
				options.AddPolicy( "AllowAll", builder =>
					builder.AllowAnyOrigin()
						   .AllowAnyMethod()
						   .AllowAnyHeader() );
			} );

			services.AddControllers().AddNewtonsoftJson();
			// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
			services.AddEndpointsApiExplorer();
			services.AddSwaggerGen();

			var settings = builder.Configuration.GetSection( "Settings" );

			SunseekerLoginService loginService = new SunseekerLoginService( settings );
			services.AddSingleton<SunseekerLoginService>( loginService );
			SunseekerWebApiService webApiService = new SunseekerWebApiService( loginService );
			services.AddSingleton<SunseekerWebApiService>( webApiService );
			SunseekerMqttService mqttService = new SunseekerMqttService( webApiService );
			services.AddSingleton<SunseekerMqttService>( mqttService );
			SunseekerWorkerService workerService = new SunseekerWorkerService( webApiService, mqttService );
			services.AddSingleton<SunseekerWorkerService>( workerService );

			var app = builder.Build();

			app.UseDefaultFiles();
			app.UseStaticFiles();
			app.UseRouting();
			app.UseCors( "AllowAll" );

			// Configure the HTTP request pipeline.
			if (app.Environment.IsDevelopment())
			{
				app.UseSwagger();
				app.UseSwaggerUI();
			}

			app.UseHttpsRedirection();

			app.UseAuthorization();

			var webSocketOptions = new WebSocketOptions
			{
				KeepAliveInterval = TimeSpan.FromSeconds( 50 )
			};

			app.UseWebSockets( webSocketOptions );

			app.MapControllers();

			app.MapFallbackToFile("/index.html");

			app.Run();
		}
	}
}
