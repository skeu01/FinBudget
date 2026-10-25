<script setup lang="ts">
import { onBeforeUnmount, onMounted, ref, watch } from 'vue'
import Chart from 'chart.js/auto'
import { currency } from '../utils/format'

const props = defineProps<{
  labels: string[]
  incomes: number[]
  expenses: number[]
  balances: number[]
  currencyCode: string
}>()

const canvas = ref<HTMLCanvasElement | null>(null)
let chart: Chart<'line', number[], string> | undefined

function renderChart() {
  if (!canvas.value) return
  chart?.destroy()
  chart = new Chart(canvas.value, {
    type: 'line',
    data: {
      labels: props.labels,
      datasets: [
        {
          label: 'Ingresos',
          data: props.incomes,
          borderColor: '#5c9a70',
          backgroundColor: '#5c9a70',
          pointBackgroundColor: '#fff',
          pointBorderColor: '#5c9a70',
          pointBorderWidth: 2,
          pointRadius: 3,
          pointHoverRadius: 5,
          tension: 0.35,
          borderWidth: 2,
        },
        {
          label: 'Gastos',
          data: props.expenses,
          borderColor: '#d69876',
          backgroundColor: '#d69876',
          pointBackgroundColor: '#fff',
          pointBorderColor: '#d69876',
          pointBorderWidth: 2,
          pointRadius: 3,
          pointHoverRadius: 5,
          tension: 0.35,
          borderWidth: 2,
        },
        {
          label: 'Balance',
          data: props.balances,
          borderColor: '#8980a4',
          backgroundColor: '#8980a4',
          pointBackgroundColor: '#fff',
          pointBorderColor: '#8980a4',
          pointBorderWidth: 2,
          pointRadius: 3,
          pointHoverRadius: 5,
          tension: 0.35,
          borderWidth: 2,
          borderDash: [5, 4],
        },
      ],
    },
    options: {
      responsive: true,
      maintainAspectRatio: false,
      interaction: { intersect: false, mode: 'index' },
      plugins: {
        legend: { display: false },
        tooltip: {
          padding: 10,
          callbacks: {
            label: context => ` ${context.dataset.label}: ${currency(Number(context.raw), props.currencyCode)}`,
          },
        },
      },
      scales: {
        x: {
          border: { display: false },
          grid: { display: false },
          ticks: { color: '#94a097', font: { size: 10 }, maxRotation: 0 },
        },
        y: {
          border: { display: false, dash: [3, 4] },
          grid: { color: '#edf1ed' },
          ticks: {
            color: '#99a39b',
            font: { size: 9 },
            maxTicksLimit: 5,
            callback: value => new Intl.NumberFormat('es-ES', {
              style: 'currency',
              currency: props.currencyCode,
              notation: 'compact',
              maximumFractionDigits: 1,
            }).format(Number(value)),
          },
        },
      },
      animation: { duration: 350 },
    },
  })
}

onMounted(renderChart)
watch(() => [props.labels, props.incomes, props.expenses, props.balances], renderChart, { deep: true })
onBeforeUnmount(() => chart?.destroy())
</script>

<template>
  <div class="trend-chart">
    <canvas ref="canvas" role="img" aria-label="Evolución mensual de ingresos, gastos y balance" />
  </div>
</template>
