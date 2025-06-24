<template>
	<div class="row">
		<!-- Baum-Struktur links mit dynamischer Einrückung -->
		<div class="tree-cell" :style="{ paddingLeft: level * 16 + 'px' }" @click="toggle">
			<p v-if="typeof node === 'string' || typeof node === 'number' || typeof node === 'boolean' || node == null">   {{ name }}</p>
			<p v-else-if="! expanded">▶ {{ name }}</p>
			<p v-else>▼ {{ name }}</p>
		</div>
		<!-- Wert rechts -->
		<div class="value-cell" v-if="typeof node === 'string' || typeof node === 'number' || typeof node === 'boolean'">
			{{ node }}
		</div>
		<div class="value-cell" v-else-if="node === null">
			null
		</div>
		<div class="value-cell" v-else-if="Array.isArray(node)">
			Array, length={{node.length}}
		</div>
		<div class="value-cell" v-else>
			{{typeof node}}
		</div>
	</div>
	<!-- Rekursive Darstellung der Kinder mit korrekter Einrückung -->
	<div v-if="expanded && Array.isArray(node)">
		<JsonTreeItem v-for="(item, index) in node" :key="index" :name="'Index-' + index" :node="item" :level="level + 1" />
	</div>
	<div v-else-if="expanded && typeof node === 'object' && node !== null">
		<JsonTreeItem v-for="child in sortedKeys( node )" :name="child" :node="(node as any)[child]" :level="level + 1" />
	</div>
</template>

<script lang="ts">
	import { Component, Vue, Prop, toNative } from 'vue-facing-decorator'

	export type JsonTreeNode  = string | number | null | {} | [];
	export type JsonTreeNodes = { [id: string]: JsonTreeNode };

	@Component({
		components: {
		}
	})
	export class JsonTreeItem extends Vue {
		@Prop({ required: true })
		name!: string;

		@Prop({ required: true })
		node!: JsonTreeNode;

		@Prop({ default: 0 })
		level!: number; // Level für die Einrückung

		expanded: boolean = false;

		private pad( num: number ): string {
			return num.toString().padStart( 2, '0' );
		}

		toggle() {
			this.expanded = ! this.expanded;
		}

		public sortedKeys( obj: any ) {
			var keys = Object.keys( obj );
			return keys.sort();
		}
	}

	export default toNative( JsonTreeItem );
</script>

<style scoped>
	.row {
		display: grid;
		grid-template-columns: 30% 70%;
		grid-column-gap: 2px;
		border-bottom: 1px solid #ccc;
		padding: 5px;
	}

	.tree-cell {
		cursor: pointer;
	}

	.value-cell {
	}
</style>
