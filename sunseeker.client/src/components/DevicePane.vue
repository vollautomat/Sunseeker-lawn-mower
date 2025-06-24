<template>
	<div>
		<h1>Device Data</h1>
		<hr />
		<JsonTreeView :treeData="deviceData" />
		<hr />
		<!--<h1>Device Properties</h1>
		<hr />
		<JsonTreeView :treeData="deviceProps" />
		<hr />-->
		<h1>Map</h1>
		<hr />
		<JsonTreeView :treeData="mapData" />
	</div>
</template>

<script lang="ts">
	import { Component, Vue, Prop, Watch, toNative } from 'vue-facing-decorator'
	import MainPane from './MainPane.vue'
	import JsonTreeView from './JsonTreeView.vue'

	@Component( {
		mixins: [MainPane],
		components: {
			JsonTreeView
		}
	} )
	export class DevicePane extends Vue {
		public deviceData  = {};
		//public deviceProps = {};
		public mapData	   = {};

		@Prop({ required: true })
		deviceId!: string;

		@Prop({ required: true })
		deviceSn!: string;

		@Watch('$route')
		routeWatcher( newVal: any, oldVal: any ) {
			this.fetchData();
		}

		async created() {
			// fetch the data when the view is created and the data is
			// already being observed
			await this.fetchData();
		}

		mounted() {
		}

		public disappear() {
		}

		private async fetchData() {
			try {
				var response = await fetch( '/sunseeker/getdeviceinfo?deviceId=' + this.deviceId );
				if ( response.ok ) {
					this.deviceData = await response.json();
				}
			}
			catch ( ex: any ) {
			}

			//try {
			//	var response = await fetch( '/sunseeker/getdevallproperties?deviceSn='+ this.deviceSn );
			//	if ( response.ok ) {
			//		this.deviceProps = await response.json();
			//	}
			//}
			//catch ( ex: any ) {
			//}

			try {
				var response = await fetch( '/sunseeker/getmap?deviceSn=' + this.deviceSn );
				if ( response.ok ) {
					this.mapData = (await response.json()).data;
				}
			}
			catch ( ex: any ) {
			}
		}
	}

	export default toNative( DevicePane );
</script>

<style scoped>
</style>
