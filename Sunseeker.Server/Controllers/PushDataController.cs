using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using Sunseeker.Server.Services;
using System.Net.WebSockets;
using System.Text;

namespace Sunseeker.Server.Controllers
{
	[Route( "[controller]" )]
	[ApiController]
	public class PushDataController : ControllerBase, IDisposable
	{
		private readonly SunseekerMqttService	mqttService;
		private readonly SunseekerWorkerService	workerService;

		public PushDataController( SunseekerMqttService mqttService, SunseekerWorkerService workerService )
		{
			this.mqttService = mqttService;
			this.mqttService.MqttAllDataChanged += MqttService_MqttAllDataChanged;

			this.workerService = workerService;
			this.workerService.WorkDataChanged += WorkerService_WorkDataChanged;
		}

		public void Dispose()
		{
			this.mqttService.MqttAllDataChanged -= MqttService_MqttAllDataChanged;
			this.workerService.WorkDataChanged  -= WorkerService_WorkDataChanged;
		}

		static private List<WebSocket> webSockets = new List<WebSocket>();

		static public bool HasListeners()
		{
			return 0 < webSockets.Count;
		}

		private void MqttService_MqttAllDataChanged( JObject mqttAllData )
		{
			Push( "MQTT", mqttAllData ).GetAwaiter().GetResult();
		}

		private void WorkerService_WorkDataChanged( JObject workData )
		{
			Push( "WORK", workData ).GetAwaiter().GetResult();
		}

		[HttpGet( "/ws/register" )]
		//[EnableCors( "AllowAll" )]
		public async Task Register()
		{
			if ( HttpContext.WebSockets.IsWebSocketRequest )
			{
				var webSocket = await HttpContext.WebSockets.AcceptWebSocketAsync();
				webSockets.Add( webSocket );
				Console.WriteLine( "Listener eingetragen" );

				await Echo( HttpContext, webSocket );
			}
			else
			{
				HttpContext.Response.StatusCode = 400;
			}
		}

		private async Task Echo( HttpContext context, WebSocket webSocket )
		{
			var buffer = new byte[1024 * 4];
			WebSocketReceiveResult result = null;
			try
			{
				result = await webSocket.ReceiveAsync( new ArraySegment<byte>( buffer ), CancellationToken.None );
			}
			catch ( Exception ex )
			{
				webSockets.Remove( webSocket );
				Console.WriteLine( "Listener wieder ausgetragen, Error: " + ex.Message );
				return;
			}

			Console.WriteLine( "Echoschleife Start" );

			while ( ! result.CloseStatus.HasValue )
			{
				string msg = Encoding.UTF8.GetString( buffer ).Substring( 0, result.Count );
				int count = result.Count;

				Console.WriteLine( "Nachricht empfangen: " + msg );

				var responseMsg = Encoding.UTF8.GetBytes( msg );
				Array.Copy( responseMsg, buffer, count );

				await webSocket.SendAsync( new ArraySegment<byte>( buffer, 0, count ), result.MessageType, result.EndOfMessage, CancellationToken.None );
				Console.WriteLine( "Antwort gesendet: " + msg );

				result = await webSocket.ReceiveAsync( new ArraySegment<byte>( buffer ), CancellationToken.None );
			}

			await webSocket.CloseAsync( result.CloseStatus.Value, result.CloseStatusDescription, CancellationToken.None );

			webSockets.Remove( webSocket );
			Console.WriteLine( "Listener ausgetragen" );
		}

		static public async Task Push( string type, JObject data )
		{
			if ( 0 == webSockets.Count )
				return;

			//Console.WriteLine( "---> PushDataController leitet weiter: " + type );

			string json   = data.ToString();
			byte[] buffer = Encoding.UTF8.GetBytes( type + "|" + json );
			int	   count  = buffer.Length;

			foreach ( WebSocket webSocket in webSockets.ToArray() )
			{
				try
				{
					await webSocket.SendAsync( new ArraySegment<byte>( buffer, 0, count ), WebSocketMessageType.Text, true, CancellationToken.None );
				}
				catch ( Exception ex )
				{
					webSockets.Remove( webSocket );
					Console.WriteLine( "Listener ausgetragen, Error: " + ex.Message );
				}
			}
		}
	}
}
