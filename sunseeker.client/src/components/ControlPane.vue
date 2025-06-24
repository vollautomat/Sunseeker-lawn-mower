<template>
	<div>
		<h1>Sunseeker Control</h1>
		<hr />
		<div class="device-list">
			<div v-for="device in allDevices.data">
				<div :class="{'device-item': true, 'selected': device === selectedDevice}"
					 @click="selectDevice( device )">
					<h2>{{device.deviceName}}</h2>
					<img :src="device.picUrl" height="100px" />
				</div>
			</div>
		</div>
		<hr />
		<div class="region-list">
			<div v-for="region in regions">
				<div :class="{'region-item': true, 'selected': region === selectedRegion}" @click="selectRegion( region )">
					{{ region.name }}
				</div>
			</div>
		</div>

		<div>
			<CanvasComponent style="width: 100%;" :path="paths" :selectedId="selectedId"
							 :phi="mapPhi" :robotPic="selectedDevice?.picUrlDetail"
							 :robotPos="(isWorkDataInvalid || !selectedDevice) ? undefined : workData[selectedDevice.deviceSn]?.robotPos"
							 :showCursor="editorOptions"
							 @refreshMap="refreshMap" @regionSelected="regionSelected" @clicked="mapClicked"
							 @editAreaMoving="editAreaMoving" @editAreaMoved="editAreaMoved"
							 v-if="0 < regions.length"></CanvasComponent>
		</div>

		<div v-if="selectedDevice && workData !== undefined && workData[selectedDevice.deviceSn]">
			<div>
				<label>Status: {{statusText}}, </label>
				<label>Batt: {{battery ?? 0}} %, </label>
				<label v-if="areaDone && areaToDo">Area: {{areaDone ?? 0}} m² / {{areaToDo ?? 0}} m² ({{(0 < areaDone && 0 < areaToDo) ? Math.round(100 * areaDone / areaToDo) : 0}} %), </label>
				<label>Active: {{currentAreaText}}</label>
			</div>
			<div>
				<label>Wether: {{rainStatusText}}</label>
				<label v-if="rainStatus == 2">, {{rainCountdown}} minutes</label>
			</div>
		</div>
		<div>
			<label class="cb-label" for="auto-proceed-cb">
				<input type="checkbox" id="auto-proceed-cb" v-model="autoProceed" @change="onAutoProceedChanged" />
				<span>Auto Proceed</span>
			</label>
			<label class="cb-label" for="editor-options-cb">
				<input type="checkbox" id="editor-options-cb" v-model="editorOptions" />
				<span>Show Editor Options</span>
			</label>
		</div>

		<div class="button-row">
			<button ref="start" :class="{ button: true, pressed: startPressed }" :disabled="startDisabled"
					@click="start" @mousedown="startPressed = true" @mouseup="startPressed = false">
				Start
			</button>
			<button ref="pause" :class="{ button: true, pressed: pausePressed }" :disabled="pauseDisabled"
					@click="pause" @mousedown="pausePressed = true" @mouseup="pausePressed = false">
				Pause
			</button>
			<button ref="home" :class="{ button: true, pressed: homePressed }" :disabled="homeDisabled"
					@click="home" @mousedown="homePressed = true" @mouseup="homePressed = false">
				Go Home
			</button>
			<button ref="stop" :class="{ button: true, pressed: stopPressed }" :disabled="stopDisabled"
					@click="stop" @mousedown="stopPressed = true" @mouseup="stopPressed = false">
				Stop
			</button>
		</div>
	</div>

	<div class="editorOptions" v-show="editorOptions">
		<div class="areaSelection">
			<hr />
			<VueSelect v-model="selectedArea" :options="optionAreas"
					   placeholder="Select an area" @option-selected="areaSelected">
			</VueSelect>
		</div>

		<div class="regionParams" v-if="selectedRegion">
			<hr />
			<h2>Region Parameters</h2>
			<div class="parameter">
				<p>Cut Direction:</p>
				<input class="param-input" type="number" v-model="planAngle" />
			</div>
			<div class="button-subrow margin-top">
				<button :class="{ button: true, pressed: applyPressed }" :disabled="applyDisabled"
						@click="applyRegionParams" @mousedown="applyPressed = true" @mouseup="applyPressed = false">
					Apply
				</button>
			</div>
			<div class="insert-option-buttons">
			</div>
		</div>

		<div class="areaParams" v-else-if="selectedArea">
			<hr />
			<h2>Area Parameters</h2>

			<div class="parameter">
				<p>Area ID:</p>
				<input class="param-input" readonly type="number" v-model="selectedArea.id" />
			</div>
			<div class="parameter">
				<p>Area Name:</p>
				<input class="param-input" type="text" v-model="selectedArea.name" />
			</div>
			<div class="button-subrow margin-top">
				<button :class="{ button: true, pressed: delAreaPressed }"
						@click="delArea" @mousedown="delAreaPressed = true" @mouseup="delAreaPressed = false">
					Delete Area
				</button>
				<button :class="{ button: true, pressed: renameAreaPressed }"
						@click="renameArea" @mousedown="renameAreaPressed = true" @mouseup="renameAreaPressed = false">
					Rename Area
				</button>
				<button :class="{ button: true, pressed: freeAreaPressed }"
						@click="createFreeAreaFromArea" @mousedown="freeAreaPressed = true" @mouseup="freeAreaPressed = false">
					Create Edit Area
				</button>
			</div>
		</div>

		<div class="areaEditor" v-else-if="editAreaSelected">
			<hr />
			<h2>Area Editor</h2>
			<p>Cursor x={{clickX.toFixed( 2 )}} y={{clickY.toFixed( 2 )}}</p>
			<div class="parameter-lang">
				<p>Points:</p>
				<input class="param-input" v-if="!! editArea" type="text" v-model="editArea.svg" :disabled="! editArea.isFree" />
			</div>

			<div v-if="editArea && editArea.isCircle">
				<div class="parameter">
					<p>Center X:</p>
					<input class="param-input no-spinner" type="number" inputmode="numeric" v-model="editArea.center.x" />
				</div>
				<div class="parameter">
					<p>Center Y:</p>
					<input class="param-input no-spinner" type="number" inputmode="numeric" v-model="editArea.center.y" />
				</div>
				<div class="parameter">
					<p>Radius:</p>
					<input class="param-input no-spinner" type="number" inputmode="numeric" v-model="(editArea as EditCircleArea).radius" />
				</div>
				<div class="parameter">
					<p>Count Points:</p>
					<input class="param-input no-spinner" type="number" inputmode="numeric" v-model="(editArea as EditCircleArea).count" />
				</div>
				<div class="button-subrow margin-top">
					<button :class="{ button: true, pressed: applyPressed }"
							@click="applyEditArea" @mousedown="applyPressed = true" @mouseup="applyPressed = false">
						Apply Changes
					</button>

					<button :class="{ button: true, pressed: commitAreaPressed }"
							@click="commitArea" @mousedown="commitAreaPressed = true" @mouseup="commitAreaPressed = false">
						Commit Area to Map
					</button>
				</div>
			</div>

			<div v-if="editArea && editArea.isEllipse">
				<div class="parameter">
					<p>Center X:</p>
					<input class="param-input no-spinner" type="number" inputmode="numeric" v-model="editArea.center.x" />
				</div>
				<div class="parameter">
					<p>Center Y:</p>
					<input class="param-input no-spinner" type="number" inputmode="numeric" v-model="editArea.center.y" />
				</div>
				<div class="parameter">
					<p>Width:</p>
					<input class="param-input no-spinner" type="number" inputmode="numeric" v-model="(editArea as EditEllipseArea).width" />
				</div>
				<div class="parameter">
					<p>Height:</p>
					<input class="param-input no-spinner" type="number" inputmode="numeric" v-model="(editArea as EditEllipseArea).height" />
				</div>
				<div class="parameter-with-legende">
					<p>Orientation:</p>
					<input class="param-input no-spinner" type="number" inputmode="numeric" v-model="(editArea as EditEllipseArea).angle" />
					<label class="parameter-legende">N: 0°, E: 90°, S: 180°, W: 270°</label>
				</div>
				<div class="parameter">
					<p>Count Points:</p>
					<input class="param-input no-spinner" type="number" inputmode="numeric" v-model="(editArea as EditEllipseArea).count" />
				</div>
				<div class="button-subrow margin-top">
					<button :class="{ button: true, pressed: applyPressed }"
							@click="applyEditArea" @mousedown="applyPressed = true" @mouseup="applyPressed = false">
						Apply Changes
					</button>

					<button :class="{ button: true, pressed: commitAreaPressed }"
							@click="commitArea" @mousedown="commitAreaPressed = true" @mouseup="commitAreaPressed = false">
						Commit Area to Map
					</button>
				</div>
			</div>

			<div v-if="editArea && editArea.isRect">
				<div class="parameter">
					<p>Center X:</p>
					<input class="param-input no-spinner" type="number" inputmode="numeric" v-model="editArea.center.x" />
				</div>
				<div class="parameter">
					<p>Center Y:</p>
					<input class="param-input no-spinner" type="number" inputmode="numeric" v-model="editArea.center.y" />
				</div>
				<div class="parameter">
					<p>Width:</p>
					<input class="param-input no-spinner" type="number" inputmode="numeric" v-model="(editArea as EditRectArea).width" />
				</div>
				<div class="parameter">
					<p>Height:</p>
					<input class="param-input no-spinner" type="number" inputmode="numeric" v-model="(editArea as EditRectArea).height" />
				</div>
				<div class="parameter-with-legende">
					<p>Orientation:</p>
					<input class="param-input no-spinner" type="number" inputmode="numeric" v-model="(editArea as EditRectArea).angle" />
					<label class="parameter-legende">N: 0°, E: 90°, S: 180°, W: 270°</label>
				</div>
				<div class="button-subrow margin-top">
					<button :class="{ button: true, pressed: applyPressed }"
							@click="applyEditArea" @mousedown="applyPressed = true" @mouseup="applyPressed = false">
						Apply Changes
					</button>

					<button :class="{ button: true, pressed: commitAreaPressed }"
							@click="commitArea" @mousedown="commitAreaPressed = true" @mouseup="commitAreaPressed = false">
						Commit Area to Map
					</button>
				</div>
			</div>

			<div v-if="editArea && editArea.isFree">
				<div class="parameter">
					<p>Center X:</p>
					<input class="param-input no-spinner" type="number" inputmode="numeric" v-model="editArea.center.x" />
				</div>
				<div class="parameter">
					<p>Center Y:</p>
					<input class="param-input no-spinner" type="number" inputmode="numeric" v-model="editArea.center.y" />
				</div>

				<div class="button-subrow margin-top">
					<button :class="{ button: true, pressed: applyPressed }"
							@click="applyEditArea" @mousedown="applyPressed = true" @mouseup="applyPressed = false">
						Apply Center
					</button>

					<button :class="{ button: true, pressed: commitAreaPressed }"
							@click="commitArea" @mousedown="commitAreaPressed = true" @mouseup="commitAreaPressed = false">
						Commit Area to Map
					</button>
				</div>
			</div>
		</div>

		<div class="areaEditor" v-if="! editAreaSelected">
			<hr />
			<h2>Editable Area</h2>

			<div class="option-buttons" v-if="! editArea">
				<div class="button-subrow">
					<button :class="{ button: true, pressed: circleAreaPressed }"
							@click="createCircleArea" @mousedown="circleAreaPressed = true" @mouseup="circleAreaPressed = false">
						Create Circle Area
					</button>
					<button :class="{ button: true, pressed: ellipseAreaPressed }"
							@click="createEllipseArea" @mousedown="ellipseAreaPressed = true" @mouseup="ellipseAreaPressed = false">
						Create Ellipse Area
					</button>
					<button :class="{ button: true, pressed: rectAreaPressed }"
							@click="createRectArea" @mousedown="rectAreaPressed = true" @mouseup="rectAreaPressed = false">
						Create Rectangle Area
					</button>
					<button :class="{ button: true, pressed: freeAreaPressed }"
							@click="createFreeArea" @mousedown="freeAreaPressed = true" @mouseup="freeAreaPressed = false">
						Create Free Area
					</button>
				</div>
			</div>
			<div v-else>
				<div class="button-subrow">
					<button :class="{ button: true, pressed: delEditAreaPressed }"
							@click="delEditArea" @mousedown="delEditAreaPressed = true" @mouseup="delEditAreaPressed = false">
						Delete Editable Area
					</button>
				</div>
			</div>
		</div>
	</div>
</template>

<script lang="ts">
	import { Component, Vue, Watch, Emit, toNative } from 'vue-facing-decorator'
	import VueSelect from 'vue3-select-component'

	import MainPane from './MainPane.vue'
	import CanvasComponent from './CanvasComponent.vue'
	import JsonTreeView from './JsonTreeView.vue'

	import { type DeviceData, type CustomData } from '../data/DeviceData'
	import { type MapData } from '../data/MapData'
	import { type MapPathData } from '../data/MapPathData'
	import { type RegionData, type RegionParamter } from '../data/RegionData'
	import { type Point, type Path } from '../data/Path'
	import { type WorkData, Status, AlgoStatus, RainStatus } from '../data/WorkData'

	import { EditArea } from '../editArea/EditArea'
	import { EditCircleArea } from '../editArea/EditCircleArea'
	import { EditEllipseArea } from '../editArea/EditEllipseArea'
	import { EditRectArea } from '../editArea/EditRectArea'
	import { EditFreeArea } from '../editArea/EditFreeArea'

	@Component( {
		mixins: [MainPane],
		components: {
			VueSelect,
			CanvasComponent,
			JsonTreeView
		}
	} )
	export class ControlPane extends Vue {
		public mqttData:			  any = {};
		public workData!:			  { [id: string]: WorkData };

		public allDevices:			  { data: DeviceData[] } = { data: [] };
		public selectedDevice:		  DeviceData | undefined = undefined;
		public regions:				  RegionData[] = [];
		public selectedRegion:		  RegionData | undefined = undefined;
		public selectedArea:		  RegionData | undefined = undefined;
		public mapPhi:				  number = 0;

		private connection:			  WebSocket | null = null;
		private connectTimer:		  number = 0;
		public  map:				  MapData | undefined = undefined;
		public  mapPath:			  MapPathData | undefined = undefined;
		private devicePath:			  number[][] | undefined = undefined;
		private realPath:			  number[][] | undefined = undefined;
		public  editArea:			  EditArea | undefined = undefined;
		public  editAreaSelected:	  boolean = false;

		public startPressed:		  boolean = false;
		public pausePressed:		  boolean = false;
		public homePressed:			  boolean = false;
		public stopPressed:			  boolean = false;
		public applyPressed:		  boolean = false;
		public delAreaPressed:		  boolean = false;
		public renameAreaPressed:	  boolean = false;
		public circleAreaPressed:	  boolean = false;
		public ellipseAreaPressed:	  boolean = false;
		public rectAreaPressed:		  boolean = false;
		public freeAreaPressed:		  boolean = false;
		public delEditAreaPressed:	  boolean = false;
		public commitAreaPressed:	  boolean = false;

		public autoProceed:			  boolean = false;
		public editorOptions:		  boolean = false;

		public clickX = 0;
		public clickY = 0;

		@Watch( '$route' )
		routeWatcher( newVal: any, oldVal: any ) {
			this.fetchData();
		}

		@Watch( 'selectedDevice' )
		async selectedDeviceWatcher( newVal: DeviceData, oldVal: DeviceData | undefined ) {
			if ( newVal != undefined && (oldVal == undefined || newVal.deviceId != oldVal.deviceId) ) {
				try {
					var response = await fetch( '/sunseeker/getdeviceinfo?deviceId=' + newVal.deviceId );
					if ( response.ok ) {
						var data = (await response.json()).data as DeviceData;
						Object.assign( this.selectedDevice!, data );
					}

					response = await fetch( '/sunseeker/getmap?deviceSn=' + newVal.deviceSn );
					if ( response.ok ) {
						this.map = (await response.json()).data;
						this.realPath = this.map?.realPathData;

						var response3 = await fetch( this.map!.realPathFileUlr );
						if ( response3.ok ) {
							try {
								this.devicePath = await response3.json();
							}
							catch {}
						}

						var response2 = await fetch( this.map!.mapPathFileUrl );
						if ( response2.ok ) {
							this.mapPath = await response2.json();
							this.regions = this.mapPath!.region_work;
							this.mapPhi  = Math.round( this.mapPath!.map_coordniate.phi * 180 / Math.PI );
						}
					}
				}
				catch ( ex: any ) {
				}
			}
		}

		@Emit('devicesLoaded')
		triggerDevicesLoadedEvent() {
			return this.allDevices.data;
		}

		public selectDevice( device: DeviceData ) {
			this.selectedDevice = device;
		}

		public selectRegion( region: RegionData ) {
			this.selectedRegion = region;
		}

		public get selectedId() {
			if ( this.selectedRegion !== undefined )
				return this.selectedRegion.id;
			else if ( this.selectedArea !== undefined)
				return this.selectedArea.id;
			else if ( this.editAreaSelected )
				return -100;
			else
				return -1;
		}

		public regionSelected( regionId: number ) {
			for ( var i in this.regions ) {
				if ( this.regions[i].id == regionId ) {
					this.selectedRegion	  = this.regions[i];
					this.selectedArea	  = undefined;
					this.editAreaSelected = false;
					return;
				}
			}

			for ( var i in this.mapPath!.region_forbidden ) {
				if ( this.mapPath!.region_forbidden[i].id == regionId ) {
					this.selectedArea	  = this.mapPath!.region_forbidden[i];
					this.selectedRegion   = undefined;
					this.editAreaSelected = false;
					return;
				}
			}

			for ( var i in this.mapPath!.region_obstacle ) {
				if ( this.mapPath!.region_obstacle[i].id == regionId ) {
					this.selectedArea	  = this.mapPath!.region_obstacle[i];
					this.selectedRegion   = undefined;
					this.editAreaSelected = false;
					return;
				}
			}

			if ( regionId == -100 ) {
				this.selectedRegion	  = undefined;
				this.selectedArea	  = undefined;
				this.editAreaSelected = true;

				return;
			}

			this.selectedRegion	  = undefined;
			this.selectedArea	  = undefined;
			this.editAreaSelected = false;
		}

		public mapClicked( arg: { x: number, y: number } ) {
			this.clickX = arg.x;
			this.clickY = arg.y;
		}

		public get optionAreas() {
			var options: any[] = [];

			if ( this.mapPath != undefined && this.mapPath.region_forbidden != undefined ) {
				this.mapPath.region_forbidden.forEach( item => {
					options.push( {
						label: item.id + " " + item.name,
						value: item
					} );
				} );
			}

			if ( this.mapPath != undefined && this.mapPath.region_obstacle != undefined ) {
				this.mapPath.region_obstacle.forEach( item => {
					options.push( {
						label: item.id + " " + item.name,
						value: item
					} );
				} );
			}

			return options;
		}

		public areaSelected() {

		}

		public editAreaMoving( arg: { shiftX: number, shiftY: number } ) {
			if ( this.editArea === undefined )
				return;

			this.editArea.setOffset( arg.shiftX, arg.shiftY );
		}

		public editAreaMoved() {
			if ( this.editArea === undefined )
				return;

			this.editArea.shift();
		}

		public get paths() : Path[] {
			var ps: Path[] = [];

			if ( 0 == this.regions.length )
				return ps;

			var workPath: RegionData[] = (this.workData !== undefined && this.workData[this.selectedDevice!.deviceSn] !== undefined)
									   ? [{ effective_area: "DEVICE", id: -1, points: JSON.stringify(this.workData[this.selectedDevice!.deviceSn].path ?? []) } as RegionData]
									   : [];
			var devPath:  RegionData[] = [{ effective_area: "DEVICE", id: -1, points: JSON.stringify( this.devicePath ?? [] ) } as RegionData];
			var realPath: RegionData[] = [{ effective_area: "DEVICE", id: -1, points: JSON.stringify( this.realPath ?? [] ) } as RegionData];
			var editPath: RegionData[] = [{ effective_area: "EDIT", id: -100, points: JSON.stringify( this.editArea?.path ?? [] ) } as RegionData];
			this.mapPath!.region_channel.forEach( c => c.effective_area = "CHANNEL" );
			this.mapPath!.region_charger_channel.forEach( c => c.effective_area = "CHARGER" );

			var allRegions = [this.mapPath!.region_charger_channel, this.mapPath!.region_channel, this.regions, realPath, workPath, this.mapPath!.region_forbidden, this.mapPath!.region_obstacle, devPath, editPath];

			for ( var r in allRegions ) {
				for ( var i in allRegions[r] ) {
					var p: Path = {
						type:	  allRegions[r][i].effective_area,
						id:		  allRegions[r][i].id,
						is_learn: allRegions[r][i].is_learn,
						points:	  [],
						sections: false
					};

					var points = JSON.parse( allRegions[r][i].points );
					for ( var j in points ) {
						if ( 2 == points[j].length ) {
							p.points.push( { x: points[j][0], y: points[j][1], a: 0 } );
						}
						else /*if ( points[j][2] == 9 )*/ {
							p.points.push( { x: points[j][0], y: points[j][1], a: points[j][2] } );
							p.sections = true;
						}
					}

					ps.push( p );
				}
			}

			return ps;
		}

		public get planAngle(): number {
			return this.getCustom( this.selectedRegion! )!.plan_angle;
		}

		public set planAngle( value: number ) {
			this.getCustom( this.selectedRegion! )!.plan_angle = value;
		}

		async created() {
			// fetch the data when the view is created and the data is
			// already being observed
			await this.fetchData();
		}

		mounted() {
			this.connectPushService();

			document.addEventListener( "visibilitychange", async () => {
				if ( document.hidden ) {
					this.disappear();
				} else {
					await this.fetchData();
					this.connectPushService();
				}
			} );
		}

		public disappear() {
			if ( null != this.connection ) {
				this.connection.close();

				setTimeout( () => {
					this.connection = null;
				}, 1000 );
			}
		}

		public async refreshMap() {
			if ( this.selectedDevice != undefined )
				await this.selectedDeviceWatcher( this.selectedDevice, undefined );
		}

		private async fetchData() {
			try {
				var response = await fetch( '/sunseeker/getautoproceed' );
				if ( response.ok ) {
					this.autoProceed = await response.json();
				}

				response = await fetch( '/sunseeker/getalldevices' );
				if ( response.ok ) {
					this.allDevices = await response.json();
					this.selectedDevice = this.allDevices.data[0];
					//this.allDevices.data.push( JSON.parse( JSON.stringify( this.selectedDevice ) ) );
					//this.allDevices.data.push( JSON.parse( JSON.stringify( this.selectedDevice ) ) );

					this.triggerDevicesLoadedEvent();
				}
			}
			catch (ex: any) {
			}

			do {
				try {
					var response = await fetch( '/sunseeker/getworkerstatus' );
					if ( response.ok ) {
						this.workData = await response.json();
					}
				}
				catch ( ex: any ) {
				}
			}
			while ( this.workData == undefined || 0 == Object.keys( this.workData ).length );

			try {
				var response = await fetch( '/sunseeker/getmqttdata' );
				if ( response.ok ) {
					this.mqttData = await response.json();
				}
			}
			catch ( ex: any ) {
			}
		}

		private connectPushService() {
			if ( null != this.connection )
				return;

			this.connection = new WebSocket( '/ws/register');

			let _this: ControlPane = this;

			this.connectTimer = setInterval( function() {
				if ( null == _this.connection ) {
					_this.connectPushService();
				}
			}, 1000 );

			if ( null == this.connection )
				return;

			this.connection.onmessage = function( event ) {
				console.log( "Push Message: " + event.data );

				if ( event.data.startsWith( "MQTT|" ) )
					_this.mqttData = JSON.parse( event.data.slice( 5 ) );
				else if ( event.data.startsWith( "WORK|" ) )
					_this.importWorkData( JSON.parse( event.data.slice( 5 ) ) );
			}

			this.connection.onopen = function( event ) {
				console.log( "Successfully connected to the websocket server..." )
				console.log( event )
			}

			this.connection.onclose = function( event ) {
				console.log( "Connection to the websocket server closed..." )
				console.log( event );

				_this.connection = null;
			}

			this.connection.onerror = function( event ) {
				console.log( "Error with websocket server..." )
				console.log( event );

				_this.connection = null;
			}
		}

		private importWorkData( newWorkData: { [id: string]: WorkData } ) {
			//var oldWorkData: { [id: string]: WorkData } = (! this.workData) ? {} : JSON.parse( JSON.stringify( this.workData ) );

			this.workData = newWorkData;

		//	var deviceSns = Object.keys( oldWorkData );
		//	for ( var i in deviceSns ) {
		//		var deviceSn = deviceSns[i];
		//		if ( ! this.workData[deviceSn] )
		//			continue;

		//		if ( this.workData[deviceSn].robotPos.date !== undefined
		//		  && oldWorkData[deviceSn].robotPos.date !== undefined
		//		  && this.workData[deviceSn].robotPos.date! < oldWorkData[deviceSn].robotPos.date! )
		//			this.workData[deviceSn].robotPos = oldWorkData[deviceSn].robotPos;
		//	}
		}

		public getCustom( region: RegionData ): CustomData | undefined {
			var id = region.id;
			for ( var i in this.selectedDevice!.custom ) {
				if ( this.selectedDevice!.custom[i].region_id == region.id )
					return this.selectedDevice!.custom[i];
			}

			return undefined;
		}

		public get isWorkDataInvalid() {
			return ! this.selectedDevice || ! this.workData || 0 == Object.keys( this.workData ).length || ! this.workData[this.selectedDevice.deviceSn];
		}

		public get status(): Status {
			if ( this.isWorkDataInvalid ) {
				return Status.Unknown;
			}
			else {
				return this.workData[this.selectedDevice!.deviceSn].status;
			}
		}

		public get statusText(): string	| undefined {
			if ( this.isWorkDataInvalid ) {
				return "?";
			}
			else if ( Status.Working != this.workData[this.selectedDevice!.deviceSn].status ) {
				var text = Status[this.workData[this.selectedDevice!.deviceSn].status];
				if ( text === undefined || 0 == text.length )
					return this.workData[this.selectedDevice!.deviceSn].status.toString();

				return text;
			}
			else {
				return Status[this.workData[this.selectedDevice!.deviceSn].status] + ", " + AlgoStatus[this.workData[this.selectedDevice!.deviceSn].algoStatus];
			}
		}

		public get battery(): number | undefined {
			if ( this.isWorkDataInvalid ) {
				return undefined;
			}
			else {
				return this.workData[this.selectedDevice!.deviceSn].elec;
			}
		}

		public get areaDone(): number | undefined {
			if ( this.isWorkDataInvalid ) {
				return undefined;
			}
			else {
				return this.workData[this.selectedDevice!.deviceSn].coverArea;
			}
		}

		public get areaToDo(): number | undefined {
			if ( this.isWorkDataInvalid ) {
				return undefined;
			}
			else {
				return this.workData[this.selectedDevice!.deviceSn].totalArea;
			}
		}

		public get currentArea(): string | number | undefined {
			if ( this.isWorkDataInvalid ) {
				return undefined;
			}
			else if ( this.workData[this.selectedDevice!.deviceSn].areaId == 0 ) {
				return "-";
			}
			else {
				return this.workData[this.selectedDevice!.deviceSn].areaId;
			}
		}

		public get currentAreaText(): string {
			if ( this.isWorkDataInvalid ) {
				return "-";
			}
			else if ( this.workData[this.selectedDevice!.deviceSn].areaId == 0 ) {
				return "-";
			}
			else {
				for ( var i in this.regions )
					if ( this.regions[i].id == this.workData[this.selectedDevice!.deviceSn].areaId )
						return this.regions[i].name;
				return "?";
			}
		}

		public get rainStatus(): number | undefined {
			if ( this.isWorkDataInvalid ) {
				return undefined;
			}
			else {
				return this.workData[this.selectedDevice!.deviceSn].rainStatus;
			}
		}

		public get rainStatusText(): string {
			if ( this.isWorkDataInvalid ) {
				return "?";
			}
			else {
				return RainStatus[this.workData[this.selectedDevice!.deviceSn].rainStatus];
			}
		}

		public get rainCountdown(): number | undefined {
			if ( this.isWorkDataInvalid ) {
				return undefined;
			}
			else {
				return this.workData[this.selectedDevice!.deviceSn].rainCountdown;
			}
		}

		public get startDisabled(): boolean {
			return ! (this.selectedRegion != undefined && (this.status == Status.Idle || this.status == Status.Pause || this.status == Status.Charging || this.status == Status.ChargingFull));
		}

		public get pauseDisabled(): boolean {
			return this.status === undefined || ! (this.status == Status.Working);
		}

		public get homeDisabled() : boolean {
			return this.status === undefined || ! (this.status == Status.Working || this.status == Status.Pause || this.status == Status.Idle );
		}

		public get stopDisabled() : boolean {
			return this.status === undefined || ! (this.status == Status.Working || this.status == Status.Pause);
		}

		public get applyDisabled(): boolean {
			return this.selectedRegion === undefined;
		}

		public async start() {
			try {
				var response = await fetch( '/sunseeker/start?deviceSn=' + this.selectedDevice!.deviceSn
											+ '&regionId=' + this.selectedRegion!.id );
				if ( response.ok ) {
					var answer = await response.json();
				}
			}
			catch ( ex: any ) {
				console.error( "Start Exception: " + ex );
			}
		}

		public async pause() {
			try {
				var response = await fetch( '/sunseeker/pause?deviceSn=' + this.selectedDevice!.deviceSn );
				if ( response.ok ) {
					var answer = await response.json();
				}
			}
			catch ( ex: any ) {
				console.error( "Pause Exception: " + ex );
			}
		}

		public async home() {
			try {
				var response = await fetch( '/sunseeker/home?deviceSn=' + this.selectedDevice!.deviceSn );
				if ( response.ok ) {
					var answer = await response.json();
				}
			}
			catch ( ex: any ) {
				console.error( "Home Exception: " + ex );
			}
		}

		public async stop() {
			try {
				var response = await fetch( '/sunseeker/stop?deviceSn=' + this.selectedDevice!.deviceSn );
				if ( response.ok ) {
					var answer = await response.json();
				}
			}
			catch ( ex: any ) {
				console.error( "Stop Exception: " + ex );
			}
		}

		public async applyRegionParams() {

			var customData: CustomData = this.getCustom( this.selectedRegion! )!;

			var parameter: RegionParamter = {
				blade_height: customData.blade_height,
				blade_speed:  customData.blade_speed,
				plan_angle:	  this.planAngle,
				plan_mode:	  customData.plan_mode,
				region_id:	  customData.region_id,
				work_gap:	  customData.work_gap,
				work_speed:	  customData.work_speed
			};

			try {
				var response = await fetch( '/sunseeker/apply', {
					method: "POST",
					headers: {
						"Content-Type": "application/json"
					},
					body: JSON.stringify( {
						deviceSn:	this.selectedDevice!.deviceSn,
						parameters:	[parameter]
					} )
				} );

				if ( response.ok ) {
					var answer = await response.json();
				}
			}
			catch ( ex: any ) {
				console.error( "Apply Exception: " + ex );
			}
		}

		public async delArea() {
			var type = 0;
			if ( -1 < this.mapPath!.region_forbidden?.indexOf( this.selectedArea! ) )
				type = 4;
			else if ( -1 < this.mapPath!.region_obstacle?.indexOf( this.selectedArea! ) )
				type = 3;

			if ( 0 == type ) {
				console.log( "Delete Area not found" );
				return;
			}

			try {
				var response = await fetch( '/sunseeker/deletearea?deviceSn=' + this.selectedDevice!.deviceSn +
																 '&areaId=' + this.selectedArea!.id +
																 '&type=' + type +
																 '&mapId=' + this.selectedDevice!.mapId );
				if ( response.ok ) {
					var answer = await response.json();
				}
			}
			catch ( ex: any ) {
				console.error( "Delete Area Exception: " + ex );
			}

			await this.refreshMap();
		}

		public async renameArea() {
			var type = 0;
			if ( -1 < this.mapPath!.region_forbidden?.indexOf( this.selectedArea! ) )
				type = 4;
			else if ( -1 < this.mapPath!.region_obstacle?.indexOf( this.selectedArea! ) )
				type = 3;

			if ( 0 == type ) {
				console.log( "Area not found" );
				return;
			}

			try {
				var response = await fetch( '/sunseeker/renamearea?deviceSn=' + this.selectedDevice!.deviceSn +
																 '&areaId=' + this.selectedArea!.id +
																 '&type=' + type +
																 '&name=' + this.selectedArea!.name );
				if ( response.ok ) {
					var answer = await response.json();
				}
			}
			catch ( ex: any ) {
				console.error( "Rename Area Exception: " + ex );
			}
		}

		public createCircleArea() {
			this.editArea = new EditCircleArea( this.clickX, this.clickY, 0.5, 12 );
			this.selectedRegion	  = undefined;
			this.selectedArea	  = undefined;
			this.editAreaSelected = true;
		}

		public createEllipseArea() {
			this.editArea = new EditEllipseArea( this.clickX, this.clickY, 2, 1, 0, 20, this.mapPhi );
			this.selectedRegion	  = undefined;
			this.selectedArea	  = undefined;
			this.editAreaSelected = true;
		}

		public createRectArea() {
			this.editArea = new EditRectArea( this.clickX, this.clickY, 2, 1, 0, this.mapPhi );
			this.selectedRegion	  = undefined;
			this.selectedArea	  = undefined;
			this.editAreaSelected = true;
		}

		public createFreeArea() {
			this.editArea = new EditFreeArea( this.clickX, this.clickY );
			this.selectedRegion	  = undefined;
			this.selectedArea	  = undefined;
			this.editAreaSelected = true;
		}

		public createFreeAreaFromArea() {
			this.editArea = new EditFreeArea( JSON.parse( this.selectedArea!.points ) );
			this.selectedRegion	  = undefined;
			this.selectedArea	  = undefined;
			this.editAreaSelected = true;
		}

		public delEditArea() {
			this.editArea = undefined;
		}

		public applyEditArea() {
			if ( this.editArea != undefined )
				this.editArea.apply();
		}

		public async commitArea() {
			if ( this.editArea === undefined )
				return;

			var existing = [];
			for ( var i in this.mapPath!.region_forbidden ) {
				existing.push({
					id:		this.mapPath!.region_forbidden[i].id,
					points:	this.mapPath!.region_forbidden[i].points,
					name:	this.mapPath!.region_forbidden[i].name,
				} );
			}

			try {
				var response = await fetch('/sunseeker/createarea', {
					method: "POST",
					headers: {
						"Content-Type": "application/json"
					},
					body: JSON.stringify( {
						deviceSn: this.selectedDevice!.deviceSn,
						mapId:	  this.selectedDevice!.mapId,
						points:	  this.editArea.path,
						name:	  this.editArea.creatorString(),
						existing: existing
					} )
				} );

				if ( response.ok ) {
					var answer = await response.json();
				}
			}
			catch ( ex: any ) {
				console.error( "Create Area Exception: " + ex );
			}

			await this.refreshMap();
			this.delEditArea();
		}

		public async onAutoProceedChanged() {
			await fetch( '/sunseeker/setautoproceed?autoproceed=' + this.autoProceed );
		}
	}

	export default toNative( ControlPane );
</script>

<style scoped>
	.device-list {
		display: flex;
		gap: 20px; /* Abstand zwischen den Boxen */
		margin-top: 10px;
		margin-bottom: 10px;
	}

	.device-item {
		border-radius: 10px;
		border: 2px solid forestgreen;
		padding: 5px 5px 2px 5px;
		width: 130px;
	}

	.region-list {
		display: flex;
		gap: 20px; /* Abstand zwischen den Boxen */
		margin-top: 10px;
		margin-bottom: 10px;
	}

	.region-item {
		border-radius: 10px;
		border: 2px solid forestgreen;
		padding: 5px 5px 2px 5px;
		width: 130px;
	}

	.selected {
		border: 4px solid forestgreen;
		padding: 3px 3px 0px 3px;
	}

	.button-row {
		margin-top: 20px;
		width: 100%;
		display: grid;
		grid-template-columns: repeat(4, 1fr);
		grid-gap: 10px;
	}

	.margin-top {
		margin-top: 20px;
	}

	.button-subrow {
/*		margin-top: 20px;
*/		display: grid;
		grid-auto-flow: column;
		grid-template-columns: repeat(auto-fit, minmax(50px, 200px));
		gap: 10px;
	}

	.button {
		width: 100%;
		padding: 10px;
		background-color: #339933;
		color: white;
		border: none;
		cursor: pointer;
		font-size: 16px;
		border-radius: 5px;
	}

		.button:hover {
			background-color: #006600;
		}
		.button:disabled {
			background-color: #ccc;
			color: #666;
			cursor: not-allowed;
		}
	.pressed {
		background-color: #99ff33 !important;
		transform: scale(0.95);
	}

	.cb-label {
		display: flex;
		align-items: center;
		gap: 8px;
	}
	label span {
		position: relative;
		top: 1px;
	}

	input[type="checkbox"] {
		appearance: none;
		width: 21px;
		height: 21px;
		background-color: white;
		border: 1px solid #333;
		border-radius: 4px;
		position: relative;
	}

		input[type="checkbox"]:checked::before {
			content: "✔";
			font-size: 30px;
			font-weight: bold;
			color: forestgreen;
			position: absolute;
			left: 1px;
			top: -10px;
		}

	.editorOptions {
		height: 500px;
	}

	.areaSelection {
		margin-top: 20px;
	}

	.regionParams {
		margin-top: 20px;
	}

	.areaParams {
		margin-top: 20px;
	}

	.areaEditor {
		margin-top: 20px;
	}

	.parameter {
		width: 295px;
		display: grid;
		grid-template-columns: 150px auto;
	}

	.parameter-with-legende {
		width: 595px;
		display: grid;
		grid-template-columns: 150px auto 300px;
	}

	.parameter-legende {
		margin-left: 5px;
	}

	.parameter-lang {
		width: 100%;
		display: grid;
		grid-template-columns: 150px auto;
	}

	.param-input {
		width: 100%;
	}

	/* Chrome, Safari, Edge, Opera */
	input.no-spinner::-webkit-inner-spin-button,
	input.no-spinner::-webkit-outer-spin-button {
		-webkit-appearance: none;
		margin: 0;
	}

	/* Firefox */
	input.no-spinner {
		-moz-appearance: textfield;
	}
</style>
