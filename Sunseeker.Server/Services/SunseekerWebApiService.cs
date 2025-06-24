using Microsoft.AspNetCore.Http.HttpResults;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Specialized;
using System.Text;
using System.Web;
using static System.Net.WebRequestMethods;

namespace Sunseeker.Server.Services
{
	public class SunseekerWebApiService
	{
		private readonly HttpClient client = new HttpClient();
		private readonly string		url	   = "https://wirefree-specific.sk-robot.com/api/";

		private readonly SunseekerLoginService loginService;

		public SunseekerWebApiService( SunseekerLoginService loginService )
		{
			this.loginService = loginService;
		}

		private string ConvertToQueryString( JObject jObject )
		{
			if ( null == jObject )
				return "";

			NameValueCollection query = HttpUtility.ParseQueryString( string.Empty );

			foreach ( var kvp in jObject )
			{
				query[kvp.Key] = kvp.Value.ToString();
			}

			string queryString = query.ToString();

			if ( 0 < queryString.Length )
				return "?" + queryString;
			else
				return "";
		}

		public int GetUserId()
		{
			return loginService.LoginData.user_id;
		}

		public async Task<string> Get( string endpoint, JObject param = null )
		{
			int trials = 10;
			while ( 0 < trials && null == loginService.LoginData )
			{
				await Task.Delay( 1000 );
				trials--;
			}

			if ( 0 == trials )
				return "{ error: 'Login failed' }";

			using var cts = new CancellationTokenSource( TimeSpan.FromSeconds( 10 ) );

			try
			{
				var request = new HttpRequestMessage( HttpMethod.Get, url + endpoint + ConvertToQueryString( param ) )
				{
					Headers =
					{
						{ "Authorization",	 "Bearer " + loginService.LoginData.access_token },
						{ "Connection",		 "Keep-Alive" },
						//{ "Accept-Language", "de-de" },
						//{ "Version",		 "1.9.3" },
						//{ "app",			 "neutral" },
						//{ "Accept-Encoding", "gzip" },
						//{ "User-Agent",		 "okhttp / 4.8.1" }
					}
				};

				var response = await client.SendAsync( request, cts.Token );

				if ( response.IsSuccessStatusCode )
				{
					string answer = await response.Content.ReadAsStringAsync();
					Console.WriteLine( answer );

					return answer;
				}
			}
			catch (TaskCanceledException)
			{
				Console.WriteLine( "Timeout" );
			}
			catch (Exception ex)
			{
				Console.WriteLine( $"Error: {ex.Message}");
			}

			return "{ error: 'GET failed' }";
		}

		public async Task<string> Post( string endpoint, JObject body )
		{
			using var cts = new CancellationTokenSource( TimeSpan.FromSeconds( 10 ) );

			string bodyJson = JsonConvert.SerializeObject( body, Formatting.None );
			
			Console.WriteLine( "POST: " + endpoint );
			Console.WriteLine( "Body: " + bodyJson );

			try
			{
				var request = new HttpRequestMessage( HttpMethod.Post, url + endpoint )
				{
					Headers =
					{
						{ "Authorization",	 "Bearer " + loginService.LoginData.access_token },
						{ "Connection",		 "Keep-Alive" },
						//{ "Accept-Language", "de-de" },
						//{ "Version",		 "1.9.3" },
						//{ "app",			 "neutral" },
						//{ "Accept-Encoding", "gzip" },
						//{ "User-Agent",		 "okhttp / 4.8.1" }
					},
					Content = new StringContent( bodyJson, Encoding.UTF8, "application/json" )
				};

				var response = await client.SendAsync( request, cts.Token );

				if ( response.IsSuccessStatusCode )
				{
					string answer = await response.Content.ReadAsStringAsync();
					Console.WriteLine( answer );

					return answer;
				}
			}
			catch ( TaskCanceledException )
			{
				Console.WriteLine( "Timeout" );
			}
			catch ( Exception ex )
			{
				Console.WriteLine( $"Error: {ex.Message}" );
			}

			return "{ error: 'POST failed' }";
		}
	}
}
