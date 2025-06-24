
export interface DeviceData {
	deviceId:			string,
	deviceSn:			string,
	deviceName:			string,
	workStatusCode:		number,
	faultStatusCode:	string,
	electricity:		number,
	wifiLv:				number,
	staWifiLv:			number,
	deviceOnlineFlag:	boolean,
	deviceModelName:	string,
	modelName:			string,
	picUrl:				string,
	picUrlDetail:		string,
	firmwareVersion:	string,
	ipAddr:				string,
	stationOnlineFlag:	boolean,
	onConnect:			boolean,
	satelliteIntensity:	number,
	deviceModelId:		string,
	topUp:				boolean,
	lock:				string | null,
	stationSn:			string,
	rainCountdown:		number,
	lat:				number,
	lng:				number,
	custom:				CustomData[],
	mapId:				number
}

export interface CustomData {
	blade_height: number,
	blade_speed:  number,
	plan_angle:	  number
	plan_mode:	  number,
	region_id:	  number,
	region_size:  number,
	setting:	  boolean,
	work_gap:	  number,
	work_speed:	  number
}
