import { EditArea } from './EditArea'

export class EditCircleArea extends EditArea {

	public constructor( centerX: number, centerY: number, radius: number, count: number ) {
		super( centerX, centerY );

		this.radius = radius;
		this.count  = count;

		this.calcPoints();
	}

	private calcPoints() {
		this.points = [];

		for ( var i = 0; i < this.count; i++ ) {
			this.points.push( [
				Math.round( (this.center.x + this.radius * Math.sin( i * Math.PI * 2 / this.count )) * 100 ) / 100,
				Math.round( (this.center.y + this.radius * Math.cos( i * Math.PI * 2 / this.count )) * 100 ) / 100
			] );
		}

		this.points.push( this.points[0] );
	}

	public get isCircle(): boolean { return true; }

	public radius!: number;
	public count!:  number;

	public apply(): void {
		this.calcPoints();
	}

	public creatorString(): string {
		return "{EditCircleArea:{centerX:" + this.center.x + ",centerY:" + this.center.y + ",radius: " + this.radius + ",count: " + this.count + "}}";
	}
}
