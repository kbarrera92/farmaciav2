<script setup lang="ts">
import { reactive, ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { errorMessage } from '@/services/http'
import { useAuthStore } from '../stores/authStore'

const auth = useAuthStore()
const router = useRouter()
const route = useRoute()

const form = reactive({ username: '', password: '' })
const submitting = ref(false)
const error = ref<string | null>(null)

async function submit(): Promise<void> {
  if (!form.username.trim() || !form.password) {
    error.value = 'Ingrese su usuario y contraseña.'
    return
  }

  submitting.value = true
  error.value = null
  try {
    await auth.login({ username: form.username.trim(), password: form.password })
    const redirect = typeof route.query.redirect === 'string' && route.query.redirect.startsWith('/')
      ? route.query.redirect
      : '/'
    await router.replace(auth.user?.mustChangePassword ? { name: 'change-password' } : redirect)
  } catch (e) {
    error.value = errorMessage(e)
    form.password = ''
  } finally {
    submitting.value = false
  }
}
</script>

<template>
  <form class="login" novalidate @submit.prevent="submit">
    <div>
      <h1>Iniciar sesión</h1>
      <p class="muted">Ingrese con el usuario asignado por el administrador.</p>
    </div>

    <p v-if="error" class="alert alert-error" role="alert">{{ error }}</p>

    <div class="field">
      <label for="username">Usuario</label>
      <input
        id="username"
        v-model="form.username"
        class="input"
        autocomplete="username"
        autofocus
        maxlength="50"
        :disabled="submitting"
      />
    </div>

    <div class="field">
      <label for="password">Contraseña</label>
      <input
        id="password"
        v-model="form.password"
        class="input"
        type="password"
        autocomplete="current-password"
        maxlength="128"
        :disabled="submitting"
      />
    </div>

    <button type="submit" class="btn btn-primary" :disabled="submitting">
      {{ submitting ? 'Ingresando…' : 'Ingresar' }}
    </button>
  </form>
</template>

<style scoped>
.login {
  display: flex;
  flex-direction: column;
  gap: 1rem;
}

h1 {
  font-size: 1.35rem;
}

p {
  margin: 0.25rem 0 0;
}

.btn {
  width: 100%;
  padding: 0.7rem;
}
</style>
