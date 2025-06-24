
export interface RegionData {
	name:			string,
	area_size:		number,
	effective_area:	string,
	is_learn:		boolean,
	id:				number,
	points:			string,
	center_point:	{x: number, y: number}
}

// {"blade_height":40,"blade_speed":2800,"plan_angle":179,"plan_mode":2,"region_id":1741501631030,"work_gap":2,"work_speed":2}
export interface RegionParamter {
	blade_height: number,
	blade_speed:  number,
	plan_angle:	  number,
	plan_mode:	  number,
	region_id:	  number,
	work_gap:	  number,
	work_speed:	  number
}
