import { EditArea } from './EditArea'

export class EditEllipseArea extends EditArea {

	public constructor( centerX: number, centerY: number, width: number, height: number, angle: number, count:number, mapPhi: number ) {
		super( centerX, centerY );

		this.width	= width;
		this.height	= height;
		this.angle	= angle;
		this.count	= count;
		this.mapPhi	= mapPhi;

		this.calcPoints();
	}

	private calcPoints() {
		this.points = [];

		this.points = [];

		for ( var i = 0; i < this.count; i++ ) {
			this.points.push( [
				this.width * Math.sin( i * Math.PI * 2 / this.count ),
				this.height * Math.cos( i * Math.PI * 2 / this.count )
			] );
		}

		var a = (this.angle - this.mapPhi) * Math.PI / 180;
		for ( var i = 0; i < this.count; i++ ) {
			var x =  this.points[i][0] * Math.cos( a ) + this.points[i][1] * Math.sin( a );
			var y = -this.points[i][0] * Math.sin( a ) + this.points[i][1] * Math.cos( a );

			this.points[i][0] = Math.round( (this.center.x + x) * 100 ) / 100;
			this.points[i][1] = Math.round( (this.center.y + y) * 100 ) / 100;
		}

		this.points.push( this.points[0] );
	}

	public get isEllipse(): boolean { return true; }

	public width!:  number;
	public height!: number;
	public angle!:  number;
	public count!:  number;
	public mapPhi!: number;

	public apply(): void {
		this.calcPoints();
	}

	public creatorString(): string {
		return "{EditEllipseArea:{centerX:" + this.center.x + ",centerY:" + this.center.y + ",width: " + this.width + ",height: " + this.height + ",angle: " + this.angle + ",count: " + this.count + "}}";
	}
}
