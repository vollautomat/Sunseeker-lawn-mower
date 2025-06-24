<template>
	<div class="row">
		<!-- Baum-Struktur links mit dynamischer Einrückung -->
		<div class="tree-cell" :style="{ paddingLeft: level * 16 + 'px' }" @click="toggle">
			<p v-if="! node.Children">   {{ name }}</p>
			<p v-else-if="! expanded">▶ {{ name }}</p>
			<p v-else>▼ {{ name }}</p>
		</div>
		<!-- Wert rechts -->
		<div class="value-cell">
			{{ formattedDate }}
		</div>
		<div class="value-cell">
			{{ node.Count }}
		</div>
		<div class="value-cell">
			{{ node.Value }}
		</div>
	</div>
	<!-- Rekursive Darstellung der Kinder mit korrekter Einrückung -->
	<div v-if="expanded">
		<TreeItem v-for="child in sortedKeys( node.Children )" :name="child" :node="node.Children[child]" :level="level + 1" />
	</div>
</template>

<script lang="ts">
	import { Component, Vue, Prop, toNative } from 'vue-facing-decorator'

	export type TreeNode  = { Date: string, Count: number, Value: string, Children: TreeNodes };
	export type TreeNodes = { [id: string]: TreeNode };

	@Component({
		components: {
		}
	})
	export class TreeItem extends Vue {
		@Prop({ required: true })
		name!: string;

		@Prop({ required: true })
		node!: TreeNode;

		@Prop({ default: 0 })
		level!: number; // Level für die Einrückung

		expanded: boolean = false;

		get formattedDate() {
			let date = new Date( this.node.Date );

			const hours	  = this.pad( date.getHours() );
			const minutes = this.pad( date.getMinutes() );
			const seconds = this.pad( date.getSeconds() );
			const day	  = this.pad( date.getDate() );
			const month   = this.pad( date.getMonth() + 1 ); // Monate sind 0-basiert!

			return `${hours}:${minutes}:${seconds} ${day}.${month}.`;
		}

		private pad( num: number ): string {
			return num.toString().padStart( 2, '0' );
		}

		toggle() {
			this.expanded = !this.expanded;
		}

		public sortedKeys( obj: any ) {
			var keys = Object.keys( obj );
			return keys.sort();
		}
	}

	export default toNative(TreeItem);
</script>

<style scoped>
	.row {
		display: grid;
		grid-template-columns: 30% 15% 5% 50%;
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
