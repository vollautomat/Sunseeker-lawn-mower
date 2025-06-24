import { EditArea } from './EditArea'

export class EditFreeArea extends EditArea {

	public constructor( centerX: number, centerY: number );
	public constructor( points: number[][] );
	public constructor( centerXOrPoints: number | number[][], centerY?: number ) {
		if ( typeof centerXOrPoints === 'number' && typeof centerY === 'number' ) {
			var centerX = centerXOrPoints;
			super( centerX, centerY );

			this.points.push( [centerX - 0.5, centerY - 0.5] );
			this.points.push( [centerX + 0.5, centerY - 0.5] );
			this.points.push( [centerX + 0.5, centerY + 0.5] );
			this.points.push( [centerX - 0.5, centerY + 0.5] );
			this.points.push( [centerX - 0.5, centerY - 0.5] );
		}
		else if ( Array.isArray( centerXOrPoints ) ) {
			var cx = 0;
			var cy = 0;

			for ( var i = 0; i < centerXOrPoints.length - 1; i++ ) {
				cx += centerXOrPoints[i][0];
				cy += centerXOrPoints[i][1];
			}

			cx /= (centerXOrPoints.length - 1);
			cy /= (centerXOrPoints.length - 1);

			super( cx, cy );

			this.points = centerXOrPoints;
		}
	}

	public get isFree(): boolean { return true; }

	public apply(): void {
		var cx = 0;
		var cy = 0;

		for ( var i = 0; i < this.points.length - 1; i++ ) {
			cx += this.points[i][0];
			cy += this.points[i][1];
		}

		var dx = this.center.x - Math.round( cx / (this.points.length - 1) * 100 ) / 100;
		var dy = this.center.y - Math.round( cy / (this.points.length - 1) * 100 ) / 100;

		for ( var i = 0; i < this.points.length; i++ ) {
			this.points[i][0] += dx;
			this.points[i][1] += dy;
		}
	}

	public creatorString(): string {
		return "{EditFreeArea:{count:" + this.points.length + "}}";
	}

	public get svg(): string {
		return super.svg;
	}

	public set svg( val: string ) {
		if ( val.slice( 0, 1 ) != "M" || val.slice( -1 ) != "Z" )
			return;

		// Regulärer Ausdruck für ein Zahlenpaar mit optionalen Leerzeichen um das Komma
		const pairRegex = /(-?(?:\d*\.\d+|\d+))\s*,\s*(-?(?:\d*\.\d+|\d+))/g;

		this.points = [];
		let match: RegExpExecArray | null;

		while ( (match = pairRegex.exec( val )) !== null ) {
			this.points.push( [parseFloat( match[1] ), parseFloat( match[2] )] );
		}

		if ( this.points[0][0] != this.points[this.points.length - 1][0] || this.points[0][1] != this.points[this.points.length - 1][1] )
			this.points.push( this.points[0] );

		var cx = 0;
		var cy = 0;

		for ( var i = 0; i < this.points.length - 1; i++ ) {
			cx += this.points[i][0];
			cy += this.points[i][1];
		}

		this.center.x = Math.round( cx / (this.points.length - 1) * 100 ) / 100;
		this.center.y = Math.round( cy / (this.points.length - 1) * 100 ) / 100;
	}
}
