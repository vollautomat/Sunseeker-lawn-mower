using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Sunseeker.Server.Services;
using System.Threading.Tasks;

namespace Sunseeker.Server.Controllers
{
	[ApiController]
	[Route("[controller]")]
	public class SunseekerController : ControllerBase
	{
		private readonly SunseekerMqttService		  mgttService;
		private readonly SunseekerWebApiService		  webApiService;
		private readonly SunseekerWorkerService		  workerService;
		private readonly ILogger<SunseekerController> logger;

		public SunseekerController( SunseekerMqttService mgttService, SunseekerWebApiService webApiService,
									SunseekerWorkerService workerService, ILogger<SunseekerController> logger )
		{
			this.mgttService   = mgttService;
			this.webApiService = webApiService;
			this.workerService = workerService;
			this.logger		   = logger;
		}

		[HttpGet( "GetMqttData" )]
		public JObject GetMqttData()
		{
			lock ( MqttData.Mutex )
			{
				return JObject.FromObject( this.mgttService.MqttAllData );
			}
		}

		[HttpGet( "GetAllDevices" )]
		public async Task<JObject> GetAllDevices()
		{
			string answer = await this.webApiService.Get( "app_wireless_mower/device-user/allDevice" );
			return JObject.Parse( answer );
		}

		[HttpGet( "GetDeviceInfo" )]
		public async Task<JObject> GetDeviceInfo( string deviceId )
		{
			string answer = await this.webApiService.Get( "app_wireless_mower/device/info/" + deviceId );
			answer = answer.Replace( ":\"{", ":{" ).Replace( "}\",", "}," )
						   .Replace( ":\"[", ":[" ).Replace( "]\",", "]," )
						   .Replace( "{\\\"", "{\"" )
						   .Replace( ",\\\"", ",\"" ).Replace( "\\\":", "\":" );
			return JObject.Parse( answer );
		}

		[HttpGet( "GetDevAllProperties" )]
		public async Task<JObject> GetDevAllProperties( string deviceSn )
		{
			JObject body = JObject.FromObject( new
			{
				appId	 = this.webApiService.GetUserId().ToString(),
				deviceSn = deviceSn,
				id		 = "getDevAllProperty",
				key		 = "all",
				method	 = "get_property"
			} );

			string answer = await this.webApiService.Post( "iot_mower/wireless/device/get_property", body );
			return JObject.Parse( answer );

		}

		[HttpGet( "GetMap" )]
		public async Task<JObject> GetMap( string deviceSn )
		{
			string answer = await this.webApiService.Get( "wireless_map/wireless_device/get?deviceSn=" + deviceSn );
			return JObject.Parse( answer );
		}

		[HttpGet( "GetWorkerStatus" )]
		public async Task<JObject> GetWorkerStatus()
		{
			return JObject.FromObject( await this.workerService.WorkData() );
		}

		[HttpGet( "Start" )]
		public async Task<JObject> Start( string deviceSn, ulong regionId )
		{
			JObject body = JObject.FromObject( new
			{
				appId	 = this.webApiService.GetUserId().ToString(),
				cmd		 = "start",
				deviceSn = deviceSn,
				id		 = "startWork",
				method	 = "action",
				work_id	 = new ulong[] { regionId }
			} );

			string answer = await this.webApiService.Post( "iot_mower/wireless/device/action", body );

			Console.WriteLine( "Action Start: " + answer );

			return JObject.Parse( answer );
		}


		[HttpGet( "Pause" )]
		public async Task<JObject> Pause( string deviceSn )
		{
			JObject body = JObject.FromObject( new
			{
				appId	 = this.webApiService.GetUserId().ToString(),
				cmd		 = "pause",
				deviceSn = deviceSn,
				id		 = "pauseWork",
				method	 = "action"
			} );

			string answer = await this.webApiService.Post( "iot_mower/wireless/device/action", body );

			Console.WriteLine( "Action Pause: " + answer );

			return JObject.Parse( answer );
		}

		[HttpGet( "Home" )]
		public async Task<JObject> Home( string deviceSn )
		{
			JObject body = JObject.FromObject( new
			{
				appId	 = this.webApiService.GetUserId().ToString(),
				cmd		 = "start_find_charger",
				deviceSn = deviceSn,
				id		 = "startFindCharger",
				method	 = "action"
			} );

			string answer = await this.webApiService.Post( "iot_mower/wireless/device/action", body );

			Console.WriteLine( "Action Home: " + answer );

			return JObject.Parse( answer );
		}

		[HttpGet( "Stop" )]
		public async Task<JObject> Stop( string deviceSn )
		{
			JObject body = JObject.FromObject( new
			{
				appId	 = this.webApiService.GetUserId().ToString(),
				cmd		 = "stop",
				deviceSn = deviceSn,
				id		 = "stopWork",
				method	 = "action"
			} );

			string answer = await this.webApiService.Post( "iot_mower/wireless/device/action", body );

			Console.WriteLine( "Action Stop: " + answer );

			return JObject.Parse( answer );
		}

		[HttpPost( "Apply" )]
		public async Task<JObject> Apply( [FromBody] JObject data )
		{
			string deviceSn   = data["deviceSn"].Value<string>();
			JArray parameters = data["parameters"] as JArray;

			// convert string parameter to ulong
			ulong regionId = parameters[0]["region_id"].Value<ulong>();
			parameters[0]["region_id"] = regionId;

			JObject body = JObject.FromObject( new
			{
				appId	 = this.webApiService.GetUserId().ToString(),
				deviceSn = deviceSn,
				id		 = "setCustom",
				key		 = "custom",
				method	 = "set_property",
				value	 = parameters
			} );

			string answer = await this.webApiService.Post( "iot_mower/wireless/device/set_property", body );
			return JObject.Parse( answer );
		}

		[HttpGet( "DeleteArea" )]
		public async Task<JObject> DeleteArea( string deviceSn, long areaId, int type, long mapId )
		{
			JObject body = JObject.FromObject( new
			{
				appId	  = this.webApiService.GetUserId().ToString(),
				deviceSn  = deviceSn,
				id		  = "deleteRegion",
				key		  = "region",
				method	  = "set_property",
				region_id = areaId,
				type	  = type
			} );

			string answer = await this.webApiService.Post( "iot_mower/wireless/device/set_property", body );

			Console.WriteLine( "Delete Area: " + answer );

			body = JObject.FromObject( new
			{
				appId	  = this.webApiService.GetUserId().ToString(),
				cmd		  = "backup_map",
				deviceSn  = deviceSn,
				id		  = "backupMap",
				map_id	  = mapId, //DateTimeOffset.Now.ToUnixTimeMilliseconds(),
				method	  = "action",
			} );

			answer = await this.webApiService.Post( "iot_mower/wireless/device/action", body );

			Console.WriteLine( "Backup Map: " + answer );

			return JObject.Parse( answer );
		}

		[HttpPost( "CreateArea" )]
		public async Task<JObject> CreateArea( [FromBody] JObject data )
		{
			string deviceSn	= data["deviceSn"].Value<string>();
			long   mapId	= data["mapId"].Value<long>();
			JArray points	= data["points"] as JArray;
			string name		= data["name"].Value<string>();
			JArray existing = data["existing"] as JArray;

			long areaId = DateTimeOffset.Now.ToUnixTimeMilliseconds();

			var area_info = new JArray();

			foreach ( var area in existing ) {
				area_info.Add( JObject.FromObject( new
				{
					map_id	= area["id"].Value<long>(),
					type	= "normal",
					vertexs	= JArray.Parse( area["points"].Value<string>() )
				} ) );
			}

			area_info.Add( JObject.FromObject( new
			{
				map_id	= areaId,
				type	= "normal",
				vertexs	= points
			} ) );

			JObject body = JObject.FromObject( new
			{
				appId	  = this.webApiService.GetUserId().ToString(),
				area_info = area_info,
				deviceSn  = deviceSn,
				id		  = "setForbidArea",
				key		  = "forbid_area",
				method	  = "set_property"
			} );

			string answer = await this.webApiService.Post( "iot_mower/wireless/device/set_property", body );

			Console.WriteLine( "Create Area: " + answer );

			body = JObject.FromObject( new
			{
				appId	  = this.webApiService.GetUserId().ToString(),
				cmd		  = "backup_map",
				deviceSn  = deviceSn,
				id		  = "backupMap",
				map_id	  = mapId,
				method	  = "action",
			} );

			answer = await this.webApiService.Post( "iot_mower/wireless/device/action", body );

			Console.WriteLine( "Backup Map: " + answer );

			foreach ( var area in existing )
				await RenameArea( deviceSn, area["id"].Value<long>(), 4, area["name"].Value<string>() );

			return await RenameArea( deviceSn, areaId, 4, name );
		}

		[HttpGet( "RenameArea" )]
		public async Task<JObject> RenameArea( string deviceSn, long areaId, int type, string name )
		{
			JObject body = JObject.FromObject( new
			{
				appId = this.webApiService.GetUserId().ToString(),
				deviceSn	= deviceSn,
				id			= "setRegionName",
				key			= "region_name",
				method		= "set_property",
				region_id	= areaId,
				region_name	= name,
				region_type	= type
			} );

			string answer = await this.webApiService.Post("iot_mower/wireless/device/set_property", body);

			Console.WriteLine( "Rename Area: " + answer );

			return JObject.Parse( answer );
		}

		[HttpGet( "GetAutoProceed" )]
		public bool GetAutoProceed()
		{
			return this.workerService.AutoProceed;
		}

		[HttpGet( "SetAutoProceed" )]
		public void SetAutoProceed( bool autoProceed )
		{
			this.workerService.AutoProceed = autoProceed;
		}
	}
}
