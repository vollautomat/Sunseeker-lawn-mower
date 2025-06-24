using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Concurrent;

namespace Sunseeker.Server.Services
{
	public class WorkData
	{
		public DateTime		 timestamp = DateTime.MinValue;
		public string		 deviceName;
		public string		 deviceId;
		public int			 status;
		public int			 algoStatus;
		public int			 elec;
		public ulong		 areaId;
		public int			 coverArea;
		public int			 totalArea;
		public int			 rainStatus;
		public int			 rainCountdown;
		public RobotPos		 robotPos = new();
		public List<float[]> path = new();

		public DateTime restartTime = DateTime.MinValue;
	}

	public class RobotPos
	{
		public float	x;
		public float	y;
		public float	angle;
		public DateTime	date;
	}

	public delegate void WorkDataChangedHandler( JObject workData );

	public class SunseekerWorkerService
	{
		private readonly SunseekerWebApiService webApiService;
		private readonly SunseekerMqttService   mqttService;

		private Dictionary<string, WorkData> workData = new();

		private ConcurrentQueue<Task>		 tasks = new();
		private bool						 taskQueueWorking = false;

		public event WorkDataChangedHandler WorkDataChanged;

		public SunseekerWorkerService( SunseekerWebApiService webApiService, SunseekerMqttService mqttService )
		{
			this.webApiService = webApiService;
			this.mqttService   = mqttService;

			this.mqttService.MqttAllDataChanged += MqttService_MqttAllDataChanged;

			instance = this;
		}

		private static SunseekerWorkerService instance;
		public static SunseekerWorkerService Instance
		{
			get { return instance; }
		}

		public async Task<Dictionary<string, WorkData>> WorkData()
		{
			await InitData();
			return this.workData;
		}

		class UserSetting
		{
			public bool AutoProceed;
			
			static private string file = "usersetting.json";

			static public UserSetting Load()
			{
				if ( ! File.Exists( file ) )
					return new UserSetting();

				string jsonString = File.ReadAllText( file );
				return JsonConvert.DeserializeObject<UserSetting>( jsonString );
			}

			public void Save()
			{
				string jsonString = JsonConvert.SerializeObject( this, Formatting.Indented );
				File.WriteAllText( file, jsonString );
			}
		}

		private UserSetting userSetting = null;

		public bool AutoProceed
		{
			get
			{
				if ( null == this.userSetting )
					this.userSetting = UserSetting.Load();

				return this.userSetting.AutoProceed;
			}
			set
			{
				if ( null == this.userSetting )
					this.userSetting = UserSetting.Load();

				this.userSetting.AutoProceed = value;
				this.userSetting.Save();
			}
		}

		public async Task Start()
		{
			await InitData();
		}

		private void MqttService_MqttAllDataChanged( JObject mqttAllData )
		{
			Task task = new Task( () => MqttService_MqttAllDataChangedAsync( mqttAllData ) );
			this.tasks.Enqueue( task );

			Task.Run( () => DoTasks() );
		}

		private async Task DoTasks()
		{
			if ( this.taskQueueWorking )
				return;

			this.taskQueueWorking = true;

			while ( 0 < this.tasks.Count )
			{
				this.tasks.TryDequeue( out Task nextTask );

				try
				{
					if ( nextTask.Status == TaskStatus.Created )
						nextTask.Start();

					await nextTask;
				}
				catch ( Exception ex )
				{
					ex.GetType();
				}
			}

			this.taskQueueWorking = false;

			await TestRestartAfterCharging();
		}

		private async Task InitData()
		{
			string answer = await this.webApiService.Get( "app_wireless_mower/device-user/allDevice" );
			JObject jAllDevices = JObject.Parse( answer );
			JArray data = jAllDevices["data"] as JArray;

			Dictionary<string, WorkData> newWorkData = new();

			for ( int i = 0; i < data.Count; i++ )
			{
				string deviceSn = data[i]["deviceSn"].Value<string>();

				WorkData wd = new();

				newWorkData.Add( deviceSn, wd );

				wd.deviceName = data[i]["deviceName"].Value<string>();
				wd.deviceId	  = data[i]["deviceId"].Value<string>();

				string answer2 = await this.webApiService.Get( "app_wireless_mower/device/info/" + wd.deviceId );
				answer2 = answer2.Replace(":\"{", ":{").Replace("}\",", "},")
								 .Replace(":\"[", ":[").Replace("]\",", "],")
								 .Replace("{\\\"", "{\"")
								 .Replace(",\\\"", ",\"").Replace("\\\":", "\":");
				JObject jDevice = JObject.Parse( answer2 );

				JObject data2 = jDevice["data"] as JObject;

				if ( null != data2 )
				{
					wd.status		  = data2["workStatusCode"].Value<int>();
					wd.algoStatus	  = data2["algoStatus"].Value<int>();
					wd.elec			  = data2["electricity"].Value<int>();
					wd.areaId		  = data2["currentRegionId"].Value<ulong>();
					wd.coverArea	  = data2["taskCoverArea"].Value<int>();
					wd.totalArea	  = data2["taskTotalArea"].Value<int>();
					wd.rainStatus	  = data2["rainStatusCode"].Value<int>();
					wd.rainCountdown  = data2["rainCountdown"].Value<int>();

					JToken robotPos   = data2["robotPos"];
					wd.robotPos.angle = robotPos["angle"].Value<float>();
					JArray robotPoint = robotPos["point"] as JArray;
					wd.robotPos.x	  = robotPoint[0].Value<float>();
					wd.robotPos.y	  = robotPoint[1].Value<float>();
					wd.robotPos.date  = DateTime.UtcNow;

					wd.path			  = new();
				}
			}

			this.workData = newWorkData;
		}

		private void MqttService_MqttAllDataChangedAsync( JObject mqttAllData )
		{
			try
			{
				foreach ( JProperty prop in mqttAllData.Properties() )
				{
					string deviceSn = prop.Name;
					JObject mqttData = prop.Value as JObject;

					WorkData wd = this.workData[deviceSn];

					JToken data = mqttData["data"];
					if ( null != data )
					{
						DateTime timestamp = data["Date"].Value<DateTime>();
						//if ( timestamp < wd.timestamp )
						//	return;

						data = data["Children"];

						if ( null != data )
						{
							wd.timestamp = timestamp;

							JToken dataStatus = data["status"];
							if ( null != dataStatus )
								wd.status = dataStatus["Value"].Value<int>();

							JToken dataAlgoStatus = data["algo_status"];
							if ( null != dataAlgoStatus)
								wd.algoStatus = dataAlgoStatus["Value"].Value<int>();

							JToken dataElec = data["elec"];
							if ( null != dataElec )
								wd.elec = dataElec["Value"].Value<int>();

							JToken dataCoverArea = data["task_cover_area"];
							if ( null != dataCoverArea )
								wd.coverArea = dataCoverArea["Value"].Value<int>();

							JToken dataTotalArea = data["task_total_area"];
							if ( null != dataTotalArea )
								wd.totalArea = dataTotalArea["Value"].Value<int>();

							JToken dataAreaId = data["current_region_id"];
							if ( null != dataAreaId )
							{
								if ( dataAreaId["Value"].Type == JTokenType.String )
								{
									if ( ! ulong.TryParse( dataAreaId["Value"].Value<string>(), out wd.areaId ) )
										wd.areaId = 0;
								}
								else
								{
									wd.areaId = dataAreaId["Value"].Value<ulong>();
								}
							}

							JToken dataRainStatus = data["rain_status"];
							if ( null != dataRainStatus )
								wd.rainStatus = dataRainStatus["Value"].Value<int>();

							JToken dataRainCountdown = data["rain_countdown"];
							if ( null != dataRainCountdown )
								wd.rainCountdown = dataRainCountdown["Value"].Value<int>();

							if ( wd.robotPos.date < timestamp )
							{
								JToken dataRobotPos = data["robot_pos"];
								if ( null != dataRobotPos )
								{
									dataRobotPos = dataRobotPos["Children"];

									JToken dataAngle = dataRobotPos["angle"];
									wd.robotPos.angle = dataAngle["Value"].Value<float>();

									JToken dataPoint = dataRobotPos["point"];
									string pointJson = dataPoint["Value"].Value<string>();
									float[] point = JsonConvert.DeserializeObject<float[]>( pointJson );
									wd.robotPos.x	 = point[0];
									wd.robotPos.y	 = point[1];
									wd.robotPos.date = timestamp;
								}
							}

							JToken dataPathInfo = data["path_info"];
							if ( null != dataPathInfo )
							{
								dataPathInfo = dataPathInfo["Children"];

								JToken dataPath = dataPathInfo["path"];
								string pointsJson = dataPath["Value"].Value<string>();
								float[][] points = JsonConvert.DeserializeObject<float[][]>( pointsJson );

								foreach ( float[] point in points )
									wd.path.Add( point );
							}
						}
					}
				}

				Console.WriteLine( "---> WorkerService hat neue workData erkannt" );

				if ( null != this.WorkDataChanged )
				{
					Console.WriteLine( "---> WorkerService sendet neue workData" );
					this.WorkDataChanged( JObject.FromObject( this.workData ) );
				}
			}
			catch ( Exception ex )
			{
				Console.WriteLine( "MqttService_MqttAllDataChangedAsync: " + ex.ToString() );
			}
		}

		private async Task TestRestartAfterCharging()
		{
			if ( ! AutoProceed )
				return;

			if ( 18 < DateTime.Now.Hour )
			{
				AutoProceed = false;
				return;
			}

			foreach ( string deviceSn in this.workData.Keys )
			{
				WorkData wd = this.workData[deviceSn];

				int minSOC = 90;
				
				if ( 0 != wd.areaId
				  && ((wd.status == 9 && minSOC < wd.elec) || wd.status == 10 || wd.status == 1)
				  && wd.rainStatus == 0
				  && 10 < (DateTime.Now - wd.restartTime).TotalSeconds )
				{
					await InitData();
					if ( 0 != wd.areaId
					  && ((wd.status == 9 && minSOC < wd.elec) || wd.status == 10 || wd.status == 1)
					  && wd.rainStatus == 0 )	// to be shure
					{
						wd.restartTime = DateTime.Now;
						Console.WriteLine( "TestRestartAfterCharging um " + wd.restartTime.ToShortTimeString() );
					
						JObject body = JObject.FromObject( new
						{
							appId	 = this.webApiService.GetUserId().ToString(),
							cmd		 = "start",
							deviceSn = deviceSn,
							id		 = "startWork",
							method	 = "action",
							work_id	 = new ulong[] { wd.areaId }
						} );

						string answer = await this.webApiService.Post( "iot_mower/wireless/device/action", body );
					}
				}
			}
		}
	}
}
