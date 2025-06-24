<template>
	<div class="post">
		<h1>{{ msg }}</h1>
	</div>
</template>

<script lang="ts">
	import { Component, Prop, Vue, toNative } from 'vue-facing-decorator'

	@Component
	class MainPane extends Vue {

		@Prop
		public msg: string = "";

		public disappear() {}

		created() {
		}

		mounted() {
		}
	}

	export default toNative( MainPane );
</script>

<style lang="css">
</style>
