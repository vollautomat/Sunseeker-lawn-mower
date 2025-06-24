import { EditArea } from './EditArea'

export class EditRectArea extends EditArea {

	public constructor( centerX: number, centerY: number, width: number, height: number, angle: number, mapPhi: number ) {
		super( centerX, centerY );

		this.width	= width;
		this.height	= height;
		this.angle	= angle;
		this.mapPhi = mapPhi;

		this.calcPoints();
	}

	private calcPoints() {
		this.points = [];

		this.points.push( [-this.width / 2, -this.height / 2] );
		this.points.push( [+this.width / 2, -this.height / 2] );
		this.points.push( [+this.width / 2, +this.height / 2] );
		this.points.push( [-this.width / 2, +this.height / 2] );

		var a = (this.angle - this.mapPhi) * Math.PI / 180;
		for ( var i = 0; i < 4; i++ ) {
			var x =  this.points[i][0] * Math.cos( a ) + this.points[i][1] * Math.sin( a );
			var y = -this.points[i][0] * Math.sin( a ) + this.points[i][1] * Math.cos( a );

			this.points[i][0] = Math.round( (this.center.x + x) * 100 ) / 100;
			this.points[i][1] = Math.round( (this.center.y + y) * 100 ) / 100;
		}

		this.points.push( this.points[0] );
	}

	public get isRect(): boolean { return true; }

	public width!:  number;
	public height!: number;
	public angle!:  number;
	public mapPhi!: number;

	public apply(): void {
		this.calcPoints();
	}

	public creatorString(): string {
		return "{EditRectArea:{centerX:" + this.center.x + ",centerY:" + this.center.y + ",width: " + this.width + ",height: " + this.height + ",angle: " + this.angle + "}}";
	}
}
