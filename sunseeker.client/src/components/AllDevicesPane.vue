<template>
	<div>
		<h1>All Devices</h1>
		<hr />
		<JsonTreeView :treeData="allDevices"/>
	</div>
</template>

<script lang="ts">
	import { Component, Vue, Watch, Emit, toNative } from 'vue-facing-decorator'
	import MainPane from './MainPane.vue'
	import JsonTreeView from './JsonTreeView.vue'
	import { type DeviceData } from '../data/DeviceData'

	@Component( {
		mixins: [MainPane],
		components: {
			JsonTreeView
		}
	} )
	export class AllDevicesPane extends Vue {
		public allDevices = { data: {} };

		@Watch('$route')
		routeWatcher( newVal: any, oldVal: any ) {
			this.fetchData();
		}

		@Emit('devicesLoaded')
		triggerDevicesLoadedEvent() {
			return this.allDevices.data;
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
				var response = await fetch( '/sunseeker/getalldevices' );
				if ( response.ok ) {
					this.allDevices = await response.json();
					this.triggerDevicesLoadedEvent();
				}
			}
			catch ( ex: any ) {
			}
		}
	}

	export default toNative( AllDevicesPane );
</script>

<style scoped>
</style>
