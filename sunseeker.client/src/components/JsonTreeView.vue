<template>
	<div class="table">
		<JsonTreeItem v-for="node in sortedKeys( treeData )" :name="node" :node="treeData[node]" :level="0" />
	</div>
</template>

<script lang="ts">
	import { Component, Vue, Prop, toNative } from 'vue-facing-decorator'
	import JsonTreeItem from './JsonTreeItem.vue'
	import { type JsonTreeNodes } from './JsonTreeItem.vue'


	@Component( {
		components: {
			JsonTreeItem
		}
	} )
	export class JsonTreeView extends Vue {
		@Prop({ required: true })
		treeData: JsonTreeNodes = {};

		public sortedKeys( obj: any ) {
			var keys = Object.keys( obj );
			return keys.sort();
		}
	}

	export default toNative( JsonTreeView );
</script>

<style scoped>
	.table {
		display: table;
		width: 100%;
	}
</style>
