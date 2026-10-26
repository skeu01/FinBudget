<script setup lang="ts">
import { ref } from 'vue'
import { RouterLink, useRoute, useRouter } from 'vue-router'
import { ArrowRight, Eye, EyeOff, House, ShieldCheck, Sparkles } from '@lucide/vue'
import UiButton from '../components/UiButton.vue'
import { getApiError } from '../api/client'
import { useAuthStore } from '../stores/auth'

const auth = useAuthStore()
const router = useRouter()
const route = useRoute()
const email = ref('')
const password = ref('')
const visible = ref(false)
const loading = ref(false)
const error = ref('')

async function submit() {
  loading.value = true
  error.value = ''
  try {
    await auth.login(email.value.trim(), password.value)
    await router.push(typeof route.query.redirect === 'string' ? route.query.redirect : '/')
  } catch (cause) {
    error.value = getApiError(cause, 'No se ha podido iniciar sesión. Revisa tus datos.')
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
        <span class="brand__name">Fin<span>Budget</span><small>FINANZAS EN FAMILIA</small></span>
      </RouterLink>
      <div class="auth-art__content">
        <span class="auth-art__eyebrow"><Sparkles :size="15" /> UN POCO MÁS DE CLARIDAD</span>
        <h1>El dinero en casa,<br />en <em>armonía.</em></h1>
        <p>Un espacio sencillo para entender en qué se va, planear lo que viene y compartirlo en familia.</p>
      </div>
      <div class="auth-quote"><span>“</span><p>La tranquilidad financiera también se construye en equipo.</p><small>UN HOGAR, UN PLAN</small></div>
      <div class="auth-art__footer"><span>Seguro y privado</span><span>Diseñado para familias</span></div>
      <div class="auth-orb auth-orb--one" /><div class="auth-orb auth-orb--two" />
    </section>

    <section class="auth-form-side">
      <div class="auth-form-wrap">
        <div class="auth-mobile-brand brand">
          <span class="brand__mark"><House :size="19" /></span>
          <span class="brand__name">Fin<span>Budget</span></span>
        </div>
        <div class="auth-heading">
          <span class="auth-kicker">QUÉ BUENO TENERTE DE VUELTA</span>
          <h2>Inicia sesión</h2>
          <p>Tu hogar financiero te está esperando.</p>
        </div>
        <form class="auth-form" @submit.prevent="submit">
          <label class="field-label" for="email">Correo electrónico</label>
          <input id="email" v-model="email" class="input" type="email" placeholder="tu@correo.com" autocomplete="email" required />
          <div class="field-label-row"><label class="field-label" for="password">Contraseña</label><a href="#" @click.prevent>¿La olvidaste?</a></div>
          <div class="password-field">
            <input id="password" v-model="password" class="input" :type="visible ? 'text' : 'password'" placeholder="Tu contraseña" autocomplete="current-password" required />
            <button type="button" class="password-toggle" :aria-label="visible ? 'Ocultar contraseña' : 'Mostrar contraseña'" @click="visible = !visible"><EyeOff v-if="visible" :size="18" /><Eye v-else :size="18" /></button>
          </div>
          <div v-if="error" class="form-error" role="alert">{{ error }}</div>
          <UiButton type="submit" class="auth-submit" :disabled="loading">{{ loading ? 'Entrando…' : 'Entrar a mi espacio' }} <ArrowRight :size="17" /></UiButton>
        </form>
        <p class="auth-switch">¿Aún no tienes cuenta? <RouterLink to="/registro">Crea tu cuenta</RouterLink></p>
        <p class="auth-privacy"><ShieldCheck :size="15" /> Tu información financiera siempre es privada.</p>
      </div>
      <span class="auth-copyright">© {{ new Date().getFullYear() }} FinBudget · Un hogar, un plan.</span>
    </section>
  </div>
</template>
