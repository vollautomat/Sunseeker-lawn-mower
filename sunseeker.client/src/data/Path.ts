export interface Point {
	x: number,
	y: number,
	a: number
}
export interface Path {
	type:	  string,
	id:		  number,
	is_learn: boolean,
	points:	  Point[],
	sections: boolean
}
