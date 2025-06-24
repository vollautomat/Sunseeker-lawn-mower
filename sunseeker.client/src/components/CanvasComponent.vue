<template>
	<div class="canvas-container" ref="canvasContainer">
		<canvas ref="canvasBackRef"></canvas>
		<canvas ref="canvasForeRef"></canvas>

		<div class="button-container">
			<button class="canvas-btn" @click="zoomIn">+</button>
			<button class="canvas-btn" @click="zoomRobot">
				<svg width="30" height="30" viewBox="0 0 100 100">
					<circle cx="30" cy="50" r="20" stroke="grey" stroke-width="5" fill="none" />
					<circle cx="30" cy="50" r="10" fill="grey" />
				</svg>
			</button>
			<button class="canvas-btn" @click="refresh">
				<svg width="30" height="30" viewBox="0 0 100 100">
					<path d="M 10,50 A 20,20 0 1 1 30,70" stroke="grey" stroke-width="5" fill="none" />
					<polygon points="22,70 30,65 30,75" fill="grey" stroke="grey" stroke-width="5" />
				</svg>
			</button>
			<button class="canvas-btn" @click="zoomEnd">
				<svg width="30" height="30" viewBox="0 0 100 100">
					<circle cx="30" cy="50" r="20" stroke="grey" stroke-width="5" fill="none" />
				</svg>
			</button>
			<button class="canvas-btn" @click="zoomOut">−</button>
		</div>
		<div class="compass-container">
			<div class="compass">
				<svg width="50" height="50" viewBox="0 0 100 100">
					<circle cx="50" cy="50" r="40" stroke="grey" stroke-width="2" fill="none" />
					<polygon points="50,10 60,50 40,50" fill="red" stroke="red" stroke-width="2" :transform="`rotate(${-phi} 50 50)`" />
					<polygon points="50,90 60,50 40,50" fill="none" stroke="grey" stroke-width="2" :transform="`rotate(${-phi} 50 50)`" />
				</svg>
			</div>
		</div>
	</div>
</template>

<script lang="ts">
	import { Component, Vue, Watch, Prop, Emit, toNative } from 'vue-facing-decorator'
	import { type Point, type Path } from '../data/Path'
	import { type Position } from '../data/WorkData'

	@Component({
		components: {}
	})
	export class CanvasComponent extends Vue {
		@Prop({ required: true })
		readonly path!: Path[];

		@Prop({ required: true })
		readonly phi!: number;

		@Prop({ required: true })
		readonly selectedId!: number;

		@Prop({ required: true })
		readonly robotPic!: string;
		private robot: ImageBitmap | undefined;

		@Prop({ required: true })
		readonly robotPos!: Position;

		@Prop({ required: true })
		readonly showCursor!: boolean;

		@Emit( 'refreshMap' )
		triggerRefreshMapEvent() {
		}

		@Emit( 'regionSelected' )
		triggerRegionSelected( regionId: number ) {
			return regionId;
		}

		@Emit( 'clicked' )
		triggerClicked( x: number, y: number ) {
			return { x: x, y: y };
		}

		@Emit( 'editAreaMoving' )
		triggerEditAreaMoving( shiftX: number, shiftY: number) {
			return { shiftX: shiftX, shiftY: shiftY };
		}
		@Emit( 'editAreaMoved' )
		triggerEditAreaMoved() {
		}

		private canvasBackground: HTMLCanvasElement | undefined;
		private canvasForeground: HTMLCanvasElement | undefined;

		private lastPos: Position	= { x: 0, y: 0, angle: 0 };
		private animateStep: number	= 0;
		private animateStepMax		= 8;
		private followRobot			= false;

		private left   = 0;
		private right  = 0;
		private bottom	   = 0;
		private top = 0;
		private margin = 5;

		private zoom	= 1;
		private zoomMin	= 1;

		private centerX = 0;
		private centerY = 0;

		private isDragging	= false;
		private startX		= 0;
		private startY		= 0;
		private shiftStartX	= 0;
		private shiftStartY	= 0;
		private shiftX		= 0;
		private shiftY		= 0;

		private clickX		 = 0;
		private clickY		 = 0;
		private editAreaMove = false;

		private path2Ds: { [id: string]: Path2D } = {};

		@Watch( "path" )
		pathChanged( newValue: Path[] ) {
			this.drawCanvas();
		}

		@Watch( "selectedId" )
		selectedIdChanged( newValue: number ) {
			this.drawCanvas();
		}

		@Watch("robotPos")
		robotPosChanged( newValue: Position, oldValue: Position ) {
			if ( undefined !== oldValue ) {
				this.lastPos = oldValue;
				this.animateStep = 0;
			}
		}

		created() {
		}

		async mounted() {
			if ( this.robotPic ) {
				const response = await fetch( this.robotPic );
				const blob = await response.blob();
				this.robot = await createImageBitmap( blob );
			}

			for ( var i in this.path ) {
				for ( var j in this.path[i].points ) {
					this.right	= Math.max( this.right,	 this.path[i].points[j].x );
					this.left	= Math.min( this.left,	 this.path[i].points[j].x );
					this.top	= Math.max( this.top,	 this.path[i].points[j].y );
					this.bottom	= Math.min( this.bottom, this.path[i].points[j].y );
				}
			}

			this.setSize();

			this.canvasBackground = this.$refs.canvasBackRef as HTMLCanvasElement;
			this.canvasForeground = this.$refs.canvasForeRef as HTMLCanvasElement;

			window.addEventListener("resize", this.setSize);
			this.canvasForeground.addEventListener( "wheel",	   this.handleWheel );
			this.canvasForeground.addEventListener( "pointerdown", this.handleMouseDown );
			this.canvasForeground.addEventListener( "pointermove", this.handleMouseMove );
			this.canvasForeground.addEventListener( "pointerup",   this.handleMouseUp );
			this.canvasForeground.addEventListener( "click",	   this.handleClick );

			setInterval( this.drawRobot, 50 );
		}

		private setSize() {
			this.canvasBackground = this.$refs.canvasBackRef as HTMLCanvasElement;
			this.canvasForeground = this.$refs.canvasForeRef as HTMLCanvasElement;
			const canvasContainer = this.$refs.canvasContainer as HTMLDivElement;

			if ( ! canvasContainer )
				return;

			const width0 = canvasContainer.clientWidth;
			const width = width0 + 2 * this.margin;
			const height = width0 * (this.top - this.bottom) / (this.right - this.left) + 2 * this.margin;

			const dpr = 2 * window.devicePixelRatio || 1;

			this.canvasForeground.width			= this.canvasBackground.width		 = width * dpr;
			this.canvasForeground.height		= this.canvasBackground.height		 = height * dpr;
			this.canvasForeground.style.width	= this.canvasBackground.style.width	 = width + 'px';
			this.canvasForeground.style.height	= this.canvasBackground.style.height = height + 'px';

			canvasContainer.style.height = height + 'px';

			this.zoom	= this.zoomMin = width0 / (this.right - this.left);
			this.shiftX	= 0;
			this.shiftY	= 0;
			this.followRobot = false;

			this.centerX = this.zoomMin * (this.right + this.left) / 2;
			this.centerY = this.zoomMin * (this.bottom + this.top) / 2;

			const bgctx = this.canvasBackground.getContext( "2d" );
			if ( ! bgctx )
				return;

			bgctx.setTransform( dpr, 0, 0, dpr, 0, 0 );
			bgctx.translate( -this.zoom * this.left + this.margin, this.zoom * this.top + this.margin );
			bgctx.scale( 1, -1 );

			const fgctx = this.canvasForeground.getContext( "2d" );
			if ( ! fgctx )
				return;

			fgctx.setTransform( dpr, 0, 0, dpr, 0, 0 );
			fgctx.translate( -this.zoom * this.left + this.margin, this.zoom * this.top + this.margin );
			fgctx.scale( 1, -1 );

			this.drawCanvas();
			this.drawRobot( true );
		}

		public zoomIn() {
			var zoomAlt = this.zoom;

			this.zoom	*= 1.1;
			this.shiftX += this.centerX / this.zoom - this.centerX / zoomAlt;
			this.shiftY += this.centerY / this.zoom - this.centerY / zoomAlt;

			this.drawCanvas();
			this.drawRobot( true );
		}

		public zoomRobot() {
			this.zoom		 = this.zoomMin * 3.5;
			this.shiftX		 = this.centerX / this.zoom - this.robotPos.x;
			this.shiftY		 = this.centerY / this.zoom - this.robotPos.y;
			this.followRobot = true;

			this.drawCanvas();
			this.drawRobot( true );
		}

		public refresh() {
			this.triggerRefreshMapEvent();
		}

		public zoomEnd() {
			this.zoom		 = this.zoomMin;
			this.shiftX		 = 0;
			this.shiftY		 = 0;
			this.followRobot = false;

			this.drawCanvas();
			this.drawRobot( true );
		}

		public zoomOut() {
			var zoomAlt = this.zoom;

			this.zoom	= Math.max( this.zoomMin, 0.9 * this.zoom );
			this.shiftX += this.centerX / this.zoom - this.centerX / zoomAlt;
			this.shiftY += this.centerY / this.zoom - this.centerY / zoomAlt;

			this.drawCanvas();
			this.drawRobot( true );
		}

		public handleWheel( ev: WheelEvent ) {
			ev.preventDefault();

			this.zoom		-= ev.deltaY / 10;
			this.zoom		 = Math.max( this.zoomMin, this.zoom );
			this.followRobot = false;

			this.drawCanvas();
			this.drawRobot( true );
		}

		public handleMouseDown( ev: PointerEvent ) {
			this.isDragging	 = true;
			this.shiftStartX = this.shiftX;
			this.shiftStartY = this.shiftY;
			this.startX		 = ev.clientX;
			this.startY		 = ev.clientY;

			this.canvasForeground!.setPointerCapture( ev.pointerId );

			this.editAreaMove = false;
			const ctx = this.canvasBackground!.getContext( "2d" );
			if ( ctx ) {
				ctx.save();
				ctx.setTransform( 1, 0, 0, 1, 0, 0 );

				var x = ev.offsetX;
				var y = ev.offsetY;

				x = x - (-this.zoomMin * this.left + this.margin);
				y = (this.zoomMin * this.top + this.margin) - y;

				var pathIds = Object.keys( this.path2Ds );
				var path2D = this.path2Ds[-100];

				if ( path2D !== undefined && ctx.isPointInPath( path2D, x, y ) ) {
					this.editAreaMove = true;
				}

				ctx.restore();
			}
		}

		public handleMouseMove( ev: PointerEvent ) {
			if ( this.isDragging ) {
				const deltaX = ev.clientX - this.startX;
				const deltaY = ev.clientY - this.startY;

				const dpr = 2 * window.devicePixelRatio || 1;
				const f   = dpr * (this.right - this.left) / this.canvasBackground!.width;

				if ( this.editAreaMove ) {
					this.triggerEditAreaMoving( deltaX * f * this.zoomMin / this.zoom, -deltaY * f * this.zoomMin / this.zoom );
				}
				else {
					this.shiftX		 = this.shiftStartX + (deltaX * f * this.zoomMin / this.zoom);
					this.shiftY		 = this.shiftStartY - (deltaY * f * this.zoomMin / this.zoom);
					this.followRobot = false;

					this.animateStep = this.animateStepMax - 1;
					this.drawCanvas();
				}
			}
		}

		public handleMouseUp( ev: PointerEvent ) {
			this.isDragging = false;
			this.canvasForeground!.releasePointerCapture( ev.pointerId );

			if ( this.editAreaMove ) {
				this.triggerEditAreaMoved();
				this.editAreaMove = false;
			}
		}

		public handleClick( ev: MouseEvent ) {
			const ctx = this.canvasBackground!.getContext( "2d" );
			if ( ! ctx )
				return;

			ctx.save();
			ctx.setTransform( 1, 0, 0, 1, 0, 0 );

			var x = ev.offsetX;
			var y = ev.offsetY;

			x = x - (-this.zoomMin * this.left + this.margin);
			y = (this.zoomMin * this.top + this.margin) - y;

			this.clickX = x / this.zoom - this.shiftX;
			this.clickY = y / this.zoom - this.shiftY;

			this.triggerClicked( this.clickX, this.clickY );

			var pathIds = Object.keys( this.path2Ds ).reverse();
			var found = false;
			for ( var i in pathIds ) {
				var id	   = pathIds[i];
				var path2D = this.path2Ds[id];

				if ( ctx.isPointInPath( path2D, x, y ) ) {
					this.triggerRegionSelected( Number( id ) );
					found = true;
					break;
				}
			}

			if ( ! found )
				this.triggerRegionSelected( 0 );

			ctx.restore();

			this.drawCanvas();
			this.drawRobot( true );
		}

		private tx( x: number ): number {
			return this.zoom * (x + this.shiftX);
		}

		private ty( y: number ): number {
			return this.zoom * (y + this.shiftY);
		}

		private drawCanvas() {
			const ctx = this.canvasBackground!.getContext( "2d" );
			if ( ! ctx )
				return;

			if ( this.followRobot ) {
				this.shiftX = this.centerX / this.zoom - this.robotPos.x;
				this.shiftY = this.centerY / this.zoom - this.robotPos.y;
			}

			ctx.clearRect( this.zoom * (this.left - 5), this.zoom * (this.bottom - 5), this.zoom * (this.right - this.left + 10), this.zoom * (this.top - this.bottom + 10) );

			for ( var i in this.path ) {
				if ( ! this.path[i].sections ) {
					const shape = new Path2D();

					for ( var j in this.path[i].points ) {
						if ( (j as unknown as number) == 0 )
							shape.moveTo( this.tx( this.path[i].points[0].x ), this.ty( this.path[i].points[0].y ) );
						else
							shape.lineTo( this.tx( this.path[i].points[j].x ), this.ty( this.path[i].points[j].y ) );
					}

					shape.closePath();

					var fill = false;
					switch ( this.path[i].type ) {
						case "CHARGER":
							ctx.strokeStyle = "grey";
							ctx.fillStyle = "grey";
							fill = true;
							break;
						case "CHANNEL":
							ctx.strokeStyle = "green";
							fill = false;
							break;
						case "INNER":
							ctx.strokeStyle = "green";
							ctx.fillStyle = "#b3ffb3";
							fill = true;

							this.path2Ds[this.path[i].id] = shape;
							break;
						case "OUTER":
							if ( this.path[i].is_learn ) {
								ctx.strokeStyle = "grey";
								ctx.fillStyle = "grey";
							}
							else {
								ctx.strokeStyle = "red";
								ctx.fillStyle = "#ffb3b3";

								this.path2Ds[this.path[i].id] = shape;
							}
							fill = true;
							break;
						case "EDIT":
							ctx.strokeStyle = "blue";
							ctx.fillStyle = 'rgba(112, 219, 219, 0.5)';
							fill = true;

							this.path2Ds[this.path[i].id] = shape;
							break;
						default:
							ctx.strokeStyle = "black";
					}

					if ( fill )
						ctx.fill( shape );

					ctx.lineWidth = (this.path[i].id == this.selectedId) ? 2 : 0.5;
					ctx.stroke( shape );
				}
				else {
					ctx.beginPath();
					var start	= true;
					var a		= -1;

					for ( var j in this.path[i].points ) {
						if ( this.path[i].points[j].a != a ) {
							if ( a != -1 ) {
								var working = (a == 9 || a == 11 || a == 12 || a == 18 || a == 16);
								var moving  = (a == 1);

								ctx.lineTo(this.tx(this.path[i].points[j].x), this.ty(this.path[i].points[j].y));
								ctx.strokeStyle = working ? "green" : (moving ? "grey" : "blue");
								ctx.lineWidth   = working ? 1 : 0.4;
								ctx.stroke();
								ctx.beginPath();
							}

							start = true;
							a = this.path[i].points[j].a;
						}

						if ( start ) {
							ctx.moveTo( this.tx( this.path[i].points[j].x ), this.ty( this.path[i].points[j].y ) );
							start = false;
						}
						else {
							ctx.lineTo( this.tx( this.path[i].points[j].x ), this.ty( this.path[i].points[j].y ) );
						}
					}

					ctx.stroke();
				}

			//	ctx.beginPath();
			//	ctx.moveTo( this.centerX - 10, this.centerY );
			//	ctx.lineTo( this.centerX + 10, this.centerY );
			//	ctx.strokeStyle	= "red";
			//	ctx.lineWidth	= 2;
			//	ctx.stroke();
			//	ctx.beginPath();
			//	ctx.moveTo( this.centerX, this.centerY - 10 );
			//	ctx.lineTo( this.centerX, this.centerY + 10 );
			//	ctx.strokeStyle	= "red";
			//	ctx.lineWidth	= 2;
			//	ctx.stroke();

				if ( this.showCursor ) {
					ctx.beginPath();
					ctx.moveTo( this.tx( this.clickX ) - 10, this.ty( this.clickY ) );
					ctx.lineTo( this.tx( this.clickX ) + 10, this.ty( this.clickY ) );
					ctx.strokeStyle	= "red";
					ctx.lineWidth	= 1;
					ctx.stroke();
					ctx.beginPath();
					ctx.moveTo( this.tx( this.clickX ), this.ty( this.clickY ) - 10 );
					ctx.lineTo( this.tx( this.clickX ), this.ty( this.clickY ) + 10 );
					ctx.strokeStyle	= "red";
					ctx.lineWidth	= 1;
					ctx.stroke();
				}
			}
		}

		private drawRobot( immediately: boolean = false ) {
			if ( ! this.robot || ! this.robotPos )
				return;

			if ( immediately )
				this.animateStep = this.animateStepMax;
			else
				this.animateStep++;

			if ( this.animateStepMax < this.animateStep )
				return;

			var dangle = this.robotPos.angle - this.lastPos.angle;
			if ( Math.PI < dangle )
				dangle = - (dangle - 2 * Math.PI);
			else if ( dangle < -Math.PI )
				dangle = - (dangle + 2 * Math.PI);

			const newPos: Position = { 
				x:	   this.lastPos.x + (this.robotPos.x - this.lastPos.x) * this.animateStep / this.animateStepMax,
				y:	   this.lastPos.y + (this.robotPos.y - this.lastPos.y) * this.animateStep / this.animateStepMax,
				angle: ((this.lastPos.angle + dangle * this.animateStep / this.animateStepMax + Math.PI) % (2 * Math.PI)) - Math.PI
			}

			if ( this.followRobot ) {
				this.shiftX = this.centerX / this.zoom - newPos.x;
				this.shiftY = this.centerY / this.zoom - newPos.y;
			}

			this.stepRobot( newPos );
		}

		private stepRobot( pos: Position ) {
			const ctx = this.canvasForeground!.getContext( "2d" );

			if ( ! ctx || this.robot === undefined )
				return;

			ctx.clearRect( this.zoom * (this.left - 5), this.zoom * (this.bottom - 5), this.zoom * (this.right - this.left + 10), this.zoom * (this.top - this.bottom + 10) );
			ctx.save();

			ctx.translate( this.tx( pos.x ), this.ty( pos.y ) );

			ctx.rotate( pos.angle + Math.PI ); // * 995 / 1000 );

			const scale	= this.zoom / 500;
			const rw	= this.robot.width;
			const rh	= this.robot.height;
			ctx.drawImage( this.robot, -rw / 2 * scale, -rh / 2 * scale, rw * scale, rh * scale );

			ctx.restore();
		}
	}
	export default toNative( CanvasComponent );
</script>

<style scoped>
	.canvas-container {
		position: relative;
		margin: 20px auto auto 0px;
	}

	canvas {
		border: 1px solid black;
		position: absolute;
		top: 0;
		left: 0;
	}

	.button-container {
		position: absolute;
		top: 5px;
		right: 5px;
		display: flex;
		flex-direction: column;
	}

	.canvas-btn {
		width: 30px;
		height: 30px;
		/*margin: 2px;*/
		background: rgba(255, 255, 255, 0.2); /* Leicht transparent */
		border: none;
		font-size: 24px;
		color: rgba(0, 0, 0, 0.5); /* Symbol leicht durchsichtig */
		cursor: pointer;
	}

		.canvas-btn:hover {
			background: rgb(200, 200, 200, 0.3); /* Leichter Effekt beim Hover */
		}

	.compass-container {
		position: absolute;
		top: 5px;
		right: 35px;
		display: flex;
		flex-direction: column;
	}

	.compass {
		width: 50px;
		height: 50px;
		background: rgba(255, 255, 255, 0.2); /* Leicht transparent */
	}

/*	#mapContainer {
		width: 100%;
		height: 800px;
		border: 1px solid black;
	}*/
</style>
