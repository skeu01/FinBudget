<script setup lang="ts">
import { onBeforeUnmount, ref } from 'vue'
import { FileImage, FileText, ImagePlus, LoaderCircle, RotateCcw, ScanLine, ShieldCheck, Sparkles, UploadCloud } from '@lucide/vue'
import { financeApi, getApiError } from '../api/client'
import { useHouseholdStore } from '../stores/household'
import type { ReceiptOcr } from '../types/finance'
import { currency, dateLabel } from '../utils/format'
import PageHeader from '../components/PageHeader.vue'
import UiButton from '../components/UiButton.vue'

const household = useHouseholdStore()
const input = ref<HTMLInputElement | null>(null)
const file = ref<File | null>(null)
const previewUrl = ref('')
const dragging = ref(false)
const uploading = ref(false)
const error = ref('')
const result = ref<ReceiptOcr | null>(null)

function setFile(nextFile?: File) {
  if (!nextFile) return
  error.value = ''
  result.value = null
  if (!['image/jpeg', 'image/png', 'application/pdf'].includes(nextFile.type)) {
    error.value = 'Elige una imagen JPG o PNG, o un documento PDF.'
    return
  }
  if (nextFile.size > 15 * 1024 * 1024) {
    error.value = 'El archivo no puede superar los 15 MB.'
    return
  }
  if (previewUrl.value) URL.revokeObjectURL(previewUrl.value)
  file.value = nextFile
  previewUrl.value = nextFile.type.startsWith('image/') ? URL.createObjectURL(nextFile) : ''
}

function onDrop(event: DragEvent) {
  dragging.value = false
  setFile(event.dataTransfer?.files[0])
}

function onInputChange(event: Event) {
  setFile((event.target as HTMLInputElement).files?.[0])
}

async function analyze() {
  if (!file.value || !household.activeHouseholdId) {
    error.value = household.activeHouseholdId ? 'Selecciona un archivo primero.' : 'Crea o selecciona un hogar para continuar.'
    return
  }
  uploading.value = true
  error.value = ''
  result.value = null
  try {
    const response = await financeApi.uploadReceipt(household.activeHouseholdId, file.value)
    result.value = response.data
  } catch (cause) {
    if (typeof cause === 'object' && cause !== null && 'response' in cause) {
      const response = (cause as { response?: { data?: ReceiptOcr } }).response?.data
      if (response?.id) result.value = response
    }
    if (!result.value) error.value = getApiError(cause, 'No se pudo analizar el documento.')
  } finally {
    uploading.value = false
  }
}

function reset() {
  file.value = null
  result.value = null
  error.value = ''
  if (previewUrl.value) URL.revokeObjectURL(previewUrl.value)
  previewUrl.value = ''
  if (input.value) input.value.value = ''
}

onBeforeUnmount(() => {
  if (previewUrl.value) URL.revokeObjectURL(previewUrl.value)
})
</script>

<template>
  <div>
    <PageHeader eyebrow="MENOS TIEMPO TECLEANDO" title="Leer un ticket" description="Sube una foto o un PDF y deja que te ayudemos a registrar los datos.">
      <span class="ocr-private"><ShieldCheck :size="15" /> Tus documentos son privados</span>
    </PageHeader>

    <div class="ocr-layout">
      <section class="surface ocr-upload-card">
        <div class="ocr-step"><span>01</span><div><p class="eyebrow">SUBE TU DOCUMENTO</p><h2>Una foto, un paso menos.</h2></div></div>
        <input ref="input" class="sr-only" type="file" accept=".jpg,.jpeg,.png,.pdf,image/jpeg,image/png,application/pdf" @change="onInputChange" />
        <button v-if="!file" class="drop-zone" :class="{ 'drop-zone--active': dragging }" @click="input?.click()" @dragover.prevent="dragging = true" @dragleave.prevent="dragging = false" @drop.prevent="onDrop">
          <span class="drop-zone__icon"><UploadCloud :size="24" /></span>
          <strong>Arrastra tu archivo aquí</strong>
          <span>o <u>selecciona desde tu dispositivo</u></span>
          <small>JPG, PNG o PDF · Máximo 15 MB</small>
        </button>
        <div v-else class="file-preview">
          <div class="file-preview__visual">
            <img v-if="previewUrl" :src="previewUrl" alt="Vista previa del ticket seleccionado" />
            <span v-else class="pdf-preview"><FileText :size="33" /><small>PDF</small></span>
          </div>
          <div class="file-preview__meta"><strong>{{ file.name }}</strong><small>{{ (file.size / 1024 / 1024).toFixed(2) }} MB · {{ file.type === 'application/pdf' ? 'Documento PDF' : 'Imagen' }}</small></div>
          <button class="icon-button" aria-label="Quitar archivo" :disabled="uploading" @click="reset">×</button>
          <button v-if="!result" class="change-file" :disabled="uploading" @click="input?.click()">Cambiar archivo</button>
        </div>

        <div v-if="error" class="form-error" role="alert">{{ error }}</div>
        <div v-if="uploading" class="ocr-progress" role="status"><LoaderCircle :size="18" class="spin-icon" /><span><strong>Estamos leyendo tu documento…</strong><small>Puede tardar unos segundos.</small></span></div>

        <div v-if="result" class="ocr-result">
          <div class="ocr-result__header"><span class="ocr-result__check"><ScanLine :size="18" /></span><div><strong>{{ result.ocrStatus === 'Succeeded' ? 'Lectura completada' : 'No se pudo leer el documento' }}</strong><small>{{ result.provider }} · {{ dateLabel(result.uploadedAt) }}</small></div></div>
          <div v-if="result.ocrStatus === 'Succeeded'" class="ocr-fields">
            <div class="ocr-field"><span>COMERCIO</span><strong>{{ result.merchant || 'No detectado' }}</strong></div>
            <div class="ocr-field"><span>FECHA</span><strong>{{ result.occurredOn ? dateLabel(result.occurredOn) : 'No detectada' }}</strong></div>
            <div class="ocr-field"><span>IMPORTE</span><strong>{{ result.total !== null ? currency(result.total, result.currency) : 'No detectado' }}</strong></div>
            <div class="ocr-field"><span>IVA</span><strong>{{ result.vat !== null ? currency(result.vat, result.currency) : 'No detectado' }}</strong></div>
          </div>
          <div v-if="result.ocrStatus === 'Succeeded' && result.items?.length" class="ocr-items"><strong>ARTÍCULOS</strong><span v-for="item in result.items" :key="`${item.description}-${item.quantity}`">{{ item.description }} · {{ item.quantity }} × {{ currency(item.price, result.currency) }}</span></div>
          <p v-if="result.ocrStatus !== 'Succeeded'" class="ocr-result__error">Código de error: {{ result.errorCode || result.ocrStatus }}. Puedes intentarlo con otra imagen más nítida.</p>
          <div v-if="result.ocrStatus === 'Succeeded'" class="ocr-review-note"><Sparkles :size="15" /> Revisa los datos antes de registrar el gasto. El OCR puede cometer errores.</div>
          <div class="ocr-result__actions"><UiButton variant="secondary" @click="reset"><RotateCcw :size="15" /> Leer otro ticket</UiButton><span v-if="result.confidence" class="confidence-pill">Confianza {{ Math.round(result.confidence) }}%</span></div>
        </div>

        <div v-else class="ocr-upload-footer">
          <span><ShieldCheck :size="15" /> Tu archivo se procesa de forma segura.</span>
          <UiButton :disabled="!file || uploading" @click="analyze"><ScanLine :size="16" /> {{ uploading ? 'Analizando…' : 'Leer ticket' }}</UiButton>
        </div>
      </section>

      <aside class="ocr-side">
        <section class="ocr-how-card">
          <div class="ocr-side-icon"><Sparkles :size="17" /></div>
          <p class="eyebrow">ASÍ DE SENCILLO</p>
          <h2>Tu tiempo es para lo importante.</h2>
          <div class="ocr-how-steps">
            <div><span>1</span><p><strong>Sube una foto</strong><small>Un ticket nítido funciona mejor.</small></p></div>
            <div><span>2</span><p><strong>Deja que lo leamos</strong><small>Detectamos fecha, comercio, total e IVA.</small></p></div>
            <div><span>3</span><p><strong>Revisa y continúa</strong><small>Comprueba los datos antes de usarlos.</small></p></div>
          </div>
          <div class="ocr-side-decoration">✳</div>
        </section>
        <section class="ocr-hint-card"><span class="ocr-hint-card__icon"><ImagePlus :size="17" /></span><div><strong>Un consejo</strong><p>Coloca el ticket sobre una superficie plana, con buena luz y todo el texto visible.</p></div></section>
        <div class="ocr-formats"><FileImage :size="16" /><span>JPG, PNG</span><i /><FileText :size="16" /><span>PDF</span></div>
      </aside>
    </div>
  </div>
</template>
