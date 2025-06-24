export abstract class EditArea {

	public constructor( centerX: number, centerY: number ) {
		this.center = { x: Math.round( centerX * 100 ) / 100, y: Math.round( centerY * 100 ) / 100 };
	}

	public  center!: { x: number, y: number };
	private offset:  { x: number, y: number } = { x: 0, y: 0 };
	
	protected points: number[][] = [];

	public get path(): number[][] {
		var path = [];

		for ( var i in this.points ) {
			path.push( [
				Math.round( (this.points[i][0] + this.offset.x) * 100 ) / 100,
				Math.round( (this.points[i][1] + this.offset.y) * 100 ) / 100,
			] );
		}

		return path;
	}

	public setOffset( x: number, y: number ): void {
		this.offset.x = x;
		this.offset.y = y;
	}

	public shift(): void {
		this.center.x = Math.round( (this.center.x + this.offset.x) * 100 ) / 100;
		this.center.y = Math.round( (this.center.y + this.offset.y) * 100 ) / 100;

		this.points = this.path;
		this.setOffset( 0, 0 );
	}

	public get isCircle():	boolean { return false; }
	public get isEllipse():	boolean { return false; }
	public get isRect():	boolean { return false; }
	public get isFree():	boolean { return false; }

	public abstract apply(): void;

	public abstract creatorString(): string;

	public get svg(): string {
		var svg = "M";

		for ( var i = 0; i < this.path.length - 1; i++ )
			svg += " " + this.path[i][0].toFixed( 2 ) + "," + this.path[i][1].toFixed( 2 );

		if ( this.path[0][0] == this.path[0][this.path.length - 1] && this.path[0][1] == this.path[this.path.length - 1][1] )
			svg += " " + this.path[0][0].toFixed( 2 ) + "," + this.path[0][1].toFixed( 2 );

		svg += " Z";

		return svg;
	}

	public set svg( val: string ) {
	}
}
