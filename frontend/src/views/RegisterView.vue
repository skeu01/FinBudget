<script setup lang="ts">
import { ref } from 'vue'
import { RouterLink, useRouter } from 'vue-router'
import { ArrowRight, Eye, EyeOff, House, ShieldCheck, Sparkles } from '@lucide/vue'
import UiButton from '../components/UiButton.vue'
import { getApiError } from '../api/client'
import { useAuthStore } from '../stores/auth'

const auth = useAuthStore()
const router = useRouter()
const name = ref('')
const email = ref('')
const password = ref('')
const visible = ref(false)
const loading = ref(false)
const error = ref('')

async function submit() {
  loading.value = true
  error.value = ''
  try {
    await auth.register(email.value.trim(), name.value.trim(), password.value)
    await router.push('/')
  } catch (cause) {
    error.value = getApiError(cause, 'No se ha podido crear la cuenta.')
  } finally {
    loading.value = false
  }
}
</script>

<template>
  <div class="auth-shell">
    <section class="auth-art">
      <RouterLink to="/login" class="brand brand--light">
        <span class="brand__mark"><House :size="19" /></span>
        <span class="brand__name">casa<span>clara</span><small>FINANZAS EN FAMILIA</small></span>
      </RouterLink>
      <div class="auth-art__content">
        <span class="auth-art__eyebrow"><Sparkles :size="15" /> UN NUEVO COMIENZO</span>
        <h1>Los buenos planes<br />empiezan <em>en casa.</em></h1>
        <p>Crea tu espacio familiar y empieza a construir una relación más tranquila con el dinero.</p>
      </div>
      <div class="auth-quote"><span>“</span><p>Pequeños pasos, grandes metas compartidas.</p><small>UN HOGAR, UN PLAN</small></div>
      <div class="auth-art__footer"><span>Seguro y privado</span><span>Diseñado para familias</span></div>
      <div class="auth-orb auth-orb--one" /><div class="auth-orb auth-orb--two" />
    </section>
    <section class="auth-form-side">
      <div class="auth-form-wrap">
        <div class="auth-mobile-brand brand"><span class="brand__mark"><House :size="19" /></span><span class="brand__name">casa<span>clara</span></span></div>
        <div class="auth-heading">
          <span class="auth-kicker">TU ESPACIO EMPIEZA AQUÍ</span>
          <h2>Crea tu cuenta</h2>
          <p>Solo necesitamos unos datos para empezar.</p>
        </div>
        <form class="auth-form" @submit.prevent="submit">
          <label class="field-label" for="name">¿Cómo te llamas?</label>
          <input id="name" v-model="name" class="input" placeholder="Tu nombre" autocomplete="name" maxlength="120" required />
          <label class="field-label" for="email">Correo electrónico</label>
          <input id="email" v-model="email" class="input" type="email" placeholder="tu@correo.com" autocomplete="email" required />
          <label class="field-label" for="password">Crea una contraseña</label>
          <div class="password-field">
            <input id="password" v-model="password" class="input" :type="visible ? 'text' : 'password'" placeholder="Al menos 12 caracteres" autocomplete="new-password" minlength="12" maxlength="128" required />
            <button type="button" class="password-toggle" :aria-label="visible ? 'Ocultar contraseña' : 'Mostrar contraseña'" @click="visible = !visible"><EyeOff v-if="visible" :size="18" /><Eye v-else :size="18" /></button>
          </div>
          <p class="field-hint">Usa al menos 12 caracteres para mantener tu cuenta segura.</p>
          <div v-if="error" class="form-error" role="alert">{{ error }}</div>
          <UiButton type="submit" class="auth-submit" :disabled="loading">{{ loading ? 'Creando tu cuenta…' : 'Crear mi cuenta' }} <ArrowRight :size="17" /></UiButton>
        </form>
        <p class="auth-switch">¿Ya tienes una cuenta? <RouterLink to="/login">Inicia sesión</RouterLink></p>
        <p class="auth-privacy"><ShieldCheck :size="15" /> Tus datos nunca se comparten.</p>
      </div>
      <span class="auth-copyright">© {{ new Date().getFullYear() }} Casa Clara · Un hogar, un plan.</span>
    </section>
  </div>
</template>
