import { type RegionData } from './RegionData.ts'

export interface MapPathData {
	map_coordniate:			{phi: number, x: number, y: number},
	region_work:			RegionData[],
	region_forbidden:		RegionData[],
	region_obstacle:		RegionData[],
	region_channel:			RegionData[],
	region_charger_channel:	RegionData[],
}
