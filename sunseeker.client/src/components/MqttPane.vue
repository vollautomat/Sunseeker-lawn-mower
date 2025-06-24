<template>
	<div>
		<h1>MQTT Messages</h1>
		<hr />
		<TreeView :treeData="mqttData"/>
	</div>
</template>

<script lang="ts">
	import { Component, Vue, Watch, toNative } from 'vue-facing-decorator'
	import MainPane from './MainPane.vue'
	import TreeView from './TreeView.vue'

	@Component( {
		mixins: [MainPane],
		components: {
			TreeView
		}
	} )
	export class MqttPane extends Vue {
		public mqttData	= {};

		private connection: WebSocket | null = null;
		private timerId: any;

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
			this.connectPushService();

			document.addEventListener( "visibilitychange", () => {
				if ( document.hidden ) {
					this.disappear();
				} else {
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

		private async fetchData() {
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

			this.connection = new WebSocket( '/ws/register' );

			let _this: MqttPane = this;

			this.timerId = setInterval( function() {
				if ( null == _this.connection ) {
					_this.connectPushService();
				}
			}, 1000 );

			if ( null == this.connection )
				return;

			this.connection.onmessage = function( event ) {

				if ( event.data.startsWith( "MQTT|" ) )
					_this.mqttData = JSON.parse( event.data.slice( 5 ) );
			}

			this.connection.onopen = function (event) {
				console.log( "Successfully connected to the websocket server: " + _this.connection?.url )
				console.log( event )
			}

			this.connection.onclose = function( event ) {
				console.log( "Connection to the websocket server closed..." )
				console.log( event );

				_this.connection = null;
			}

			this.connection.onerror = function( event ) {
				console.log( "Error with websocket server: " + _this.connection?.url )
				console.log( event );

				_this.connection = null;
			}
		}
	}

	export default toNative( MqttPane );
</script>

<style scoped>
</style>
