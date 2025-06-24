
export enum Status {
	Unknown		 = 0,
	Idle		 = 1,
	Working		 = 2,
	Pause		 = 3,
	Error		 = 6,
	Return		 = 7,
	ReturnPause	 = 8,
	Charging	 = 9,
	ChargingFull = 10,
	Offline		 = 13,
	Locating	 = 15,
	Stopp		 = 18
}

export enum AlgoStatus {
	Moving = 0,
	Mowing = 1
}

export enum RainStatus {
	Sunny	= 0,
	Raining	= 1,
	Drying	= 2
}

export interface Position {
	x:	   number,
	y:	   number,
	angle: number,
	date?: Date
}

export interface WorkData {
	deviceName:	   string;
	deviceId:	   string;
	status:		   Status;
	algoStatus:	   AlgoStatus;
	elec:		   number;
	areaId:		   number;
	coverArea:	   number;
	totalArea:	   number;
	rainStatus:	   RainStatus;
	rainCountdown: number;
	robotPos:	   Position;
	path:		   number[][];
}
