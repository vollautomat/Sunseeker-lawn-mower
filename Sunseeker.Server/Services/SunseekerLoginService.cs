using System.Threading;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace Sunseeker.Server.Services
{
	public class LoginData
	{
		public string access_token;
		public string token_type;
		public string refresh_token;
		public int	  expires_in;
		public string scope;
		public int	  tenant_id;
		public string license;
		public bool	  bind_flag;
		public int	  test;
		public int	  user_id;
		public int	  refresh_expires_in;
		public string nickname;
		public string user_level_code;
		public bool	  active;
		public string dept_id;
		public string username;
	}

	public class Account
	{
		public string User { get; set; }
		public string Pass { get; set; }
	}

	public class SunseekerLoginService
	{
		private readonly HttpClient client = new HttpClient();
		private readonly string		url	   = "https://wirefree-specific.sk-robot.com/api/";
		private readonly string		user;
		private readonly string		passwd;

		private LoginData loginData = null;
		private Timer	  refreshTimer;

		public SunseekerLoginService( IConfigurationSection settings )
		{
			Account account = settings.Get<Account>();

			this.user   = account.User;
			this.passwd = account.Pass;

			Task.Run( async () =>
			{
				await Task.Delay( 1000 );
				await LoginAsync();
			} );
		}

		public LoginData LoginData { get { return this.loginData; } }

		private async Task LoginAsync()
		{
			var formData = new FormUrlEncodedContent( new[]
			{
				new KeyValuePair<string, string>( "username",   user ),
				new KeyValuePair<string, string>( "password",   passwd ),
				new KeyValuePair<string, string>( "grant_type",	"password" ),
				new KeyValuePair<string, string>( "scope",		"server" )
			} );

			int  loginAttempt = 0;
			bool success	  = false;

			while ( loginAttempt++ < 10 )
			{
				using var cts = new CancellationTokenSource( TimeSpan.FromSeconds( 10 ) );

				try
				{
					var request = new HttpRequestMessage( HttpMethod.Post, url + "auth/oauth/token" )
					{
						Headers =
						{
							{ "Authorization", "Basic YXBwOmFwcA==" },
							{ "Connection",	   "Keep-Alive" }
						},
						Content = formData
					};

					var response = await client.SendAsync( request, cts.Token );

					if ( response.IsSuccessStatusCode )
					{
						string login = await response.Content.ReadAsStringAsync();
						Console.WriteLine( "Login erfolgreich:" );
						Console.WriteLine( login );

						this.loginData = JsonConvert.DeserializeObject<LoginData>( login );

						int refreshPeriod = Math.Min( this.loginData.expires_in, 4 * 3600 ) * 1000;
						refreshTimer = new Timer( RefreshTimerCallback, null, refreshPeriod, refreshPeriod );

						await SunseekerWorkerService.Instance.Start();
						await SunseekerMqttService.Instance.Start( this.loginData );

						success = true;
						break;
					}
				}
				catch ( TaskCanceledException )
				{
					Console.WriteLine( "Timeout überschritten, neuer Versuch..." );
				}
				catch ( Exception ex )
				{
					Console.WriteLine( $"Fehler: {ex.Message}" );
				}
			}

			if ( ! success )
				Console.WriteLine( "Login auch nach 10 Versuchen fehlgeschlagen" );
		}

		private void RefreshTimerCallback( object state )
		{
			Task.Run( async () =>
			{
				await RefreshLoginAsync();
			} );
		}

		private async Task RefreshLoginAsync()
		{
			var formData = new FormUrlEncodedContent( new[]
			{
				new KeyValuePair<string, string>( "refresh_token", this.loginData.refresh_token ),
				new KeyValuePair<string, string>( "grant_type",	   "refresh_token" ),
				new KeyValuePair<string, string>( "scope",		   "server" )
			} );

			int  loginAttempt = 0;
			bool success	  = false;

			while ( loginAttempt++ < 10 )
			{
				using var cts = new CancellationTokenSource( TimeSpan.FromSeconds( 10 ) );

				try
				{
					var request = new HttpRequestMessage( HttpMethod.Post, url + "auth/oauth/token" )
					{
						Headers =
						{
							{ "Authorization", "Basic YXBwOmFwcA==" },
							{ "Connection",	   "Keep-Alive" }
						},
						Content = formData
					};

					var response = await client.SendAsync( request, cts.Token );

					if ( response.IsSuccessStatusCode )
					{
						string login = await response.Content.ReadAsStringAsync();
						Console.WriteLine( "Refresh Login erfolgreich:" );
						Console.WriteLine( login );

						this.loginData = JsonConvert.DeserializeObject<LoginData>( login );

						await SunseekerMqttService.Instance.Start( this.loginData );

						success = true;
						break;
					}
				}
				catch ( TaskCanceledException )
				{
					Console.WriteLine( "Timeout überschritten, neuer Versuch..." );
				}
				catch ( Exception ex )
				{
					Console.WriteLine( $"Fehler: {ex.Message}" );
				}
			}

			if ( ! success )
				Console.WriteLine( "Refresh Login auch nach 10 Versuchen fehlgeschlagen" );
		}
	}
}
