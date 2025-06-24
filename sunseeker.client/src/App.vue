<template>
	<div class="example example-1">
		<tabs @tabChanged="showPane" v-show="showTabs">
			<tab v-for="(tab, index) in panes" :key="index" :title="tab.tab" />
		</tabs>
	</div>

	<ControlPane ref="control" v-if="panes[0].vbl" @devicesLoaded="devicesLoaded" />
	<MqttPane ref="mqtt" v-if="panes[1].vbl" />
	<AllDevicesPane ref="allDevices" v-if="panes[2].vbl" @devicesLoaded="devicesLoaded" />
	<DevicePane v-for="device in visibleDeviceData" :ref="device.deviceId" :deviceId="device.deviceId" :deviceSn="device.deviceSn" />
</template>

<script lang="ts">
	import { Component, Vue, toNative } from 'vue-facing-decorator'
	import { reactive } from 'vue'
	import { nextTick } from 'vue'
	import Tabs	from './components/Tabs.vue'
	import Tab from './components/Tab.vue'
	import MainPane from './components/MainPane.vue'
	import ControlPane from './components/ControlPane.vue'
	import MqttPane from './components/MqttPane.vue'
	import AllDevicesPane from './components/AllDevicesPane.vue'
	import { type DeviceData } from './data/DeviceData'
	import DevicePane from './components/DevicePane.vue'

	class PaneData {
		public ref: string  = "";
		public tab: string  = "";
		public vbl: boolean = false;
	}

	@Component( {
		components: {
			Tabs,
			Tab,
			ControlPane,
			MqttPane,
			AllDevicesPane,
			DevicePane,
		}
	} )
	class App extends Vue {

		private basePanes: PaneData[] = [
			{ ref: "control",	 tab: "Control",	 vbl: true },
			{ ref: "mqtt",		 tab: "MQTT",		 vbl: false },
			{ ref: "allDevices", tab: "Device List", vbl: false },
		];

		public panes: PaneData[] = reactive([...this.basePanes]);
		public allDeviceData: DeviceData[] = [];
		public showTabs = true;

		created() {
		}

		mounted() {
		}

		public get visibleDeviceData(): DeviceData[] {
			var visible: DeviceData[] = [];
			var devicePanes = this.panes.slice( 3 );
			for ( var i in devicePanes )
				if ( devicePanes[i].vbl )
					visible.push( this.allDeviceData[i] );

			return visible;
		}

		public showPane( pane: number ) {
			for ( var i = 0; i < this.panes.length; i++ ) {
				if ( this.panes[i].vbl && i == pane )
					return;

				if ( this.panes[i].vbl ) {
					var lastPane = this.$refs[this.panes[i].ref] as InstanceType<typeof MainPane>;
					if (Array.isArray(lastPane))
						lastPane[0].disappear();
					else
						lastPane.disappear();

					this.panes[i].vbl = false;
				}
			}

			this.panes[pane].vbl = true;
		}

		public devicesLoaded( allDeviceData: DeviceData[] ) {
			this.panes = [...this.basePanes];

			for ( var index in allDeviceData ) {
				var deviceData = allDeviceData[index];
				this.panes = [...this.panes, { ref: deviceData.deviceId, tab: deviceData.deviceName, vbl: false }];
			}

			this.panes = reactive([...this.panes]);
			this.allDeviceData = [...allDeviceData];

			this.showTabs = false;		// Trick to refresh the view and tio make new tabs visible.
			nextTick(() => {
				this.showTabs = true;
			} );
		}
	}

	export default toNative( App )
</script>

<style>
	#app {
		font-family: Avenir, Helvetica, Arial, sans-serif;
		-webkit-font-smoothing: antialiased;
		-moz-osx-font-smoothing: grayscale;
		color: #2c3e50;
		margin-top: 5px;
	}

	.button-area {
		width: fit-content;
		height: 40px;
		flex-direction: row;
		margin: auto;
	}

		.button-area button {
			margin-top: 10px;
			margin-left: 10px;
			width: 150px;
		}

	.tabs-example {
		display: grid;
		place-items: center;
		text-align: left;
	}

		.tabs-example .example {
			width: 80%;
			padding: 0 1rem;
			border-radius: 8px;
			background: #fdfdff;
			border: 2px solid #e7e7f5;
			margin-block-end: 1rem;
		}
</style>
