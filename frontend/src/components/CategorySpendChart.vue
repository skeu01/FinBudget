<script setup lang="ts">
import { computed, onBeforeUnmount, onMounted, ref, watch } from 'vue'
import Chart from 'chart.js/auto'
import { currency } from '../utils/format'

const props = defineProps<{
  labels: string[]
  values: number[]
  colors: string[]
  total: number
  currencyCode: string
}>()

const canvas = ref<HTMLCanvasElement | null>(null)
let chart: Chart<'doughnut', number[], string> | undefined
const centerTotal = computed(() => currency(props.total, props.currencyCode))

function renderChart() {
  if (!canvas.value) return
  chart?.destroy()
  chart = new Chart(canvas.value, {
    type: 'doughnut',
    data: {
      labels: props.labels,
      datasets: [{
        data: props.values,
        backgroundColor: props.colors,
        borderColor: '#fff',
        borderWidth: 4,
        hoverOffset: 5,
        borderRadius: 4,
      }],
    },
    options: {
      responsive: true,
      maintainAspectRatio: false,
      cutout: '76%',
      plugins: {
        legend: { display: false },
        tooltip: {
          padding: 10,
          displayColors: true,
          callbacks: {
            label: context => ` ${currency(Number(context.raw), props.currencyCode)}`,
          },
        },
      },
      animation: { duration: 350 },
    },
  })
}

onMounted(renderChart)
watch(() => [props.labels, props.values, props.colors], renderChart, { deep: true })
onBeforeUnmount(() => chart?.destroy())
</script>

<template>
  <div class="category-chart">
    <canvas ref="canvas" role="img" :aria-label="`Gráfico de gastos por categoría. Total ${centerTotal}`" />
    <div class="category-chart__center" aria-hidden="true">
      <span>GASTADO</span>
      <strong>{{ centerTotal }}</strong>
    </div>
  </div>
</template>
