<template>
	<div class="table" v-for="key in sortedKeys( treeData )">
		<h3>{{key}}</h3>
		<TreeItem v-for="node in sortedKeys( treeData[key] )" :name="node" :node="treeData[key][node]" :level="0" />
	</div>
</template>

<script lang="ts">
	import { Component, Vue, Prop, toNative } from 'vue-facing-decorator'
	import TreeItem from './TreeItem.vue'
	import { type TreeNodes } from './TreeItem.vue'


	@Component( {
		components: {
			TreeItem
		}
	} )
	export class TreeView extends Vue {
		@Prop({ required: true })
		treeData: { [id: string]: TreeNodes } = {};

		public sortedKeys( obj: any ) {
			var keys = Object.keys( obj );
			return keys.sort();
		}
	}

	export default toNative( TreeView );
</script>

<style scoped>
	.table {
		display: table;
		width: 100%;
	}
</style>
