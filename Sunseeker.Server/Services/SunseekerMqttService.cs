
using System.Security.Cryptography;
using System.Text;

using Newtonsoft.Json;

using MQTTnet;
using Newtonsoft.Json.Linq;

namespace Sunseeker.Server.Services
{
	public class MqttData
	{
		public MqttData( DateTime utcTimestamp, JProperty prop )
		{
			Date  = utcTimestamp;
			Count = 1;
			Set( utcTimestamp, prop );
		}

		private static object mutex = new();
		public static  object Mutex { get { return mutex; } }

		public void Update( DateTime utcTimestamp, JProperty prop )
		{
			Date = utcTimestamp;
			Count++;
			Set( utcTimestamp, prop );
		}

		private void Set( DateTime utcTimestamp, JProperty prop )
		{
			if ( prop.Value is JObject )
			{
				if ( null == Children )
					Children = new();

				foreach ( JProperty childProp in ((JObject) prop.Value).Properties() )
				{
					lock( Mutex )
					{
						if ( ! Children.TryGetValue( childProp.Name, out MqttData mqttData ) )
						{
							mqttData = new MqttData( utcTimestamp, childProp );
							Children.Add( childProp.Name, mqttData );
						}
						else
						{
							mqttData.Update( utcTimestamp, childProp );
						}
					}
				}
			}
			else
			{
				Value = prop.Value.ToString( Formatting.None );
			}
		}

		public DateTime						Date;
		public int							Count;
		public string						Value;
		public Dictionary<string, MqttData>	Children;
	}

	public delegate void MqttAllDataChangedHandler( JObject mqttAllData );

	public class SunseekerMqttService : IDisposable
	{
		private readonly SunseekerWebApiService webApiService;

		public SunseekerMqttService( SunseekerWebApiService webApiService )
		{
			this.webApiService = webApiService;

			instance = this;

			string hostname = System.Net.Dns.GetHostName();
			this.appId		= ComputeSha256Hash( hostname ).Substring( 0, 16 );
			//this.appId		= Guid.NewGuid().ToString().Replace( "-", "" ).Substring( 0, 16 );
			this.mqttPasswd	= Guid.NewGuid().ToString().Replace( "-", "" ).Substring( 0, 24 );
		}
		public void Dispose()
		{
			if ( null != this.mqttClient )
				this.mqttClient.DisconnectAsync().GetAwaiter().GetResult();
		}

		private static SunseekerMqttService instance;
		public static SunseekerMqttService Instance
		{
			get { return instance; }
		}

		private readonly HttpClient	client	   = new HttpClient();
		private readonly string		url		   = "https://wirefree-specific.sk-robot.com/api/admin/user/edit";
		private readonly string		appId;
		private readonly string		mqttPasswd;
		private readonly string		publicKey  = string.Join( "\n",
												 "-----BEGIN PUBLIC KEY-----",
												 "MIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEA0f7mbMVc/YIYQbR8Ty3u",
												 "7yx0cKX6Gt7JkVQrWynI7xM6/yVPMC1I7nXdjMlVPpc06UXoc5ClQNsTbQ4vumFg",
												 "2RZPQwAOc7yL1Y8t1W0b9jMTztu32ZzlobfzIVkIO1R7x1I+pkyp6QDm/MnvWyeu",
												 "CM77gS2bDv47H9COQn/gy/fy9uecyWCY3u+dXQhujLPrSJ2FFs6SwD0t5QEJjdrC",
												 "ftkKQFsflm+i5RQZBMNGT3LdAMnPK4avG642Afum0SzmNrEZrIo7pr2w0fvokbWB",
												 "SOOeEdGAx7UVI1kHssOohqW37yJzzFMIlahZSEJ0A3Dm6yrtgobp2mQlCisqsVW4",
												 "XwIDAQAB",
												 "-----END PUBLIC KEY-----" );

		private Dictionary<string, Dictionary<string, MqttData>> mqttAllData = new();
		public Dictionary<string, Dictionary<string, MqttData>>  MqttAllData {  get { return this.mqttAllData; } }

		private IMqttClient					   mqttClient;
		public event MqttAllDataChangedHandler MqttAllDataChanged;

		public async Task Start( LoginData loginData )
		{
			string passwd = EncryptRsaBase64( mqttPasswd, publicKey );
			await UserEditPasswdAsync( loginData.access_token, passwd );
			await StartMqttAsync( loginData.username + appId, mqttPasswd, loginData.user_id );
		}

		private string EncryptRsaBase64( string text, string publicKeyPem )
		{
			byte[] dataToEncrypt = Encoding.UTF8.GetBytes( text );

			using ( RSACryptoServiceProvider rsa = new RSACryptoServiceProvider() )
			{
				rsa.ImportFromPem(publicKeyPem.ToCharArray());

				byte[] encryptedData = rsa.Encrypt( dataToEncrypt, false ); // PKCS1 Padding
				return Convert.ToBase64String( encryptedData );
			}
		}

		async Task UserEditPasswdAsync( string accessToken, string passwd )
		{
			var requestBody = new
			{
				appIdCode			= appId,
				appType				= 2,
				mqttsPassword		= passwd,
				operatingSystemCode	= "android"
			};

			var jsonContent = new StringContent( JsonConvert.SerializeObject(requestBody), Encoding.UTF8, "application/json" );

			using var cts = new CancellationTokenSource( TimeSpan.FromSeconds( 10 ) );

			try
			{
				var request = new HttpRequestMessage( HttpMethod.Put, url )
				{
					Headers =
					{
						{ "Authorization", $"Bearer {accessToken}" }
					},
					Content = jsonContent
				};

				var response = await client.SendAsync( request, cts.Token );

				if ( response.IsSuccessStatusCode )
				{
					string data = await response.Content.ReadAsStringAsync();
					Console.WriteLine( "---------------------" );
					Console.WriteLine( "userEditPasswd:" );
					Console.WriteLine( data );
				}
			}
			catch ( TaskCanceledException )
			{
				Console.WriteLine( "Timeout reached, nect trial..." );
			}
			catch ( Exception ex )
			{
				Console.WriteLine( $"Error: {ex.Message}" );
			}
		}

		private async Task StartMqttAsync( string username, string password, int userId )
		{
			if ( null != this.mqttClient )
				this.mqttClient.DisconnectAsync().GetAwaiter().GetResult();

			var options = new MqttClientOptionsBuilder()
			.WithClientId( "MyClientId" )
			.WithTcpServer( "wfsmqtt-specific.sk-robot.com", 1884 )
			.WithCredentials( username, password )
			.WithTlsOptions( new MqttClientTlsOptions { UseTls = true } )
			.Build();

			var mqttFactory = new MqttClientFactory();

			this.mqttClient = mqttFactory.CreateMqttClient();

			this.mqttClient.ConnectedAsync += async e =>
			{
				Console.WriteLine( "Successfully connected with MQTT-Broker!" );

				await this.mqttClient.SubscribeAsync( "/wirelessdevice/" + userId.ToString() + "/get" );

				Console.WriteLine( "Subscribed to topic /wirelessdevice/" + userId.ToString() + "/get" );
			};

			this.mqttClient.ApplicationMessageReceivedAsync += e =>
			{
				string  data  = Encoding.UTF8.GetString( e.ApplicationMessage.Payload );

				data = data.Replace( ":\"{", ":{" ).Replace( "}\",", "}," )
						   .Replace( ":\"[", ":[" ).Replace( "]\",", "]," )
						   .Replace( "{\\\"", "{\"" )
						   .Replace( ",\\\"", ",\"" ).Replace( "\\\":", "\":" );

				JObject jData = JsonConvert.DeserializeObject<JObject>( data );

				DateTime utcTimestamp = DateTime.MinValue;
				if ( jData.TryGetValue( "timestamp", out JToken jTimestamp ) )
				{
					long unixTimestamp = jTimestamp.Value<long>();
					utcTimestamp = DateTimeOffset.FromUnixTimeMilliseconds( unixTimestamp ).UtcDateTime;
				}

				string  key	  = "other_data";
				if ( jData.TryGetValue( "deviceSn", out JToken jDeviceSn ) )
					key = jDeviceSn.ToString();

				if ( ! this.mqttAllData.TryGetValue( key, out Dictionary<string, MqttData> mqttKeyData ) )
				{
					mqttKeyData = new();
					this.mqttAllData.Add( key, mqttKeyData );
				}

				foreach ( JProperty prop in jData.Properties() )
				{
					lock ( MqttData.Mutex )
					{
						if ( ! mqttKeyData.TryGetValue( prop.Name, out MqttData mqttData ) )
						{
							mqttData = new MqttData( utcTimestamp, prop );
							mqttKeyData.Add( prop.Name, mqttData );
						}
						else
						{
							mqttData.Update( utcTimestamp, prop );
						}
					}
				}

				//Console.WriteLine( $"Message received for topic: {e.ApplicationMessage.Topic}" );
				//Console.WriteLine( $"{data}" );

				if ( null != this.MqttAllDataChanged )
				{
					JObject jAllData = null;
					lock ( MqttData.Mutex )
					{
						jAllData = JObject.FromObject( this.mqttAllData );
					}
					this.MqttAllDataChanged( jAllData );
				}
				
				return Task.CompletedTask;
			};

			this.mqttClient.DisconnectedAsync += async e =>
			{
				Console.WriteLine( "Connection lost! Retry..." );
				await Task.Delay( 5000 );
				await this.mqttClient.ConnectAsync( options );
			};

			await this.mqttClient.ConnectAsync( options );
		}

		public static string ComputeSha256Hash( string rawData )
		{
			using ( SHA256 sha256Hash = SHA256.Create() )
			{
				byte[] bytes = sha256Hash.ComputeHash( Encoding.UTF8.GetBytes( rawData ) );

				StringBuilder builder = new StringBuilder();
				foreach ( byte b in bytes )
				{
					builder.Append( b.ToString( "x2" ) ); // Hexadezimalformat
				}
				return builder.ToString();
			}
		}
	}
}
