<script setup lang="ts">
import { computed, reactive, ref } from 'vue'
import { RouterLink, useRouter } from 'vue-router'
import { ApiError, errorMessage } from '@/services/http'
import { useAuthStore } from '../stores/authStore'

const auth = useAuthStore()
const router = useRouter()

const form = reactive({ currentPassword: '', newPassword: '', confirmPassword: '' })
const submitting = ref(false)
const error = ref<string | null>(null)
const fieldErrors = ref<Record<string, string | undefined>>({})

const forced = computed(() => auth.user?.mustChangePassword ?? false)

async function submit(): Promise<void> {
  fieldErrors.value = {}
  error.value = null

  if (form.newPassword !== form.confirmPassword) {
    fieldErrors.value = { confirmPassword: 'Las contraseñas no coinciden.' }
    return
  }

  submitting.value = true
  try {
    await auth.changePassword({ currentPassword: form.currentPassword, newPassword: form.newPassword })
    await router.replace({ name: 'home' })
  } catch (e) {
    if (e instanceof ApiError && e.status === 400) {
      fieldErrors.value = {
        currentPassword: e.fieldError('currentPassword'),
        newPassword: e.fieldError('newPassword'),
      }
    }
    error.value = errorMessage(e)
  } finally {
    submitting.value = false
  }
}

async function logout(): Promise<void> {
  await auth.logout()
  await router.push({ name: 'login' })
}
</script>

<template>
  <form class="change-password" novalidate @submit.prevent="submit">
    <div>
      <h1>Cambiar contraseña</h1>
      <p class="muted">
        {{ forced ? 'Por seguridad debe definir una contraseña nueva antes de continuar.' : 'Defina una contraseña nueva.' }}
      </p>
    </div>

    <p v-if="error" class="alert alert-error" role="alert">{{ error }}</p>

    <div class="field">
      <label for="current">Contraseña actual</label>
      <input
        id="current"
        v-model="form.currentPassword"
        class="input"
        type="password"
        autocomplete="current-password"
        :aria-invalid="!!fieldErrors.currentPassword"
      />
      <p v-if="fieldErrors.currentPassword" class="field-error">{{ fieldErrors.currentPassword }}</p>
    </div>

    <div class="field">
      <label for="new">Nueva contraseña</label>
      <input
        id="new"
        v-model="form.newPassword"
        class="input"
        type="password"
        autocomplete="new-password"
        :aria-invalid="!!fieldErrors.newPassword"
      />
      <p v-if="fieldErrors.newPassword" class="field-error">{{ fieldErrors.newPassword }}</p>
      <p v-else class="field-hint">Mínimo 8 caracteres, con letras y números.</p>
    </div>

    <div class="field">
      <label for="confirm">Confirmar nueva contraseña</label>
      <input
        id="confirm"
        v-model="form.confirmPassword"
        class="input"
        type="password"
        autocomplete="new-password"
        :aria-invalid="!!fieldErrors.confirmPassword"
      />
      <p v-if="fieldErrors.confirmPassword" class="field-error">{{ fieldErrors.confirmPassword }}</p>
    </div>

    <button type="submit" class="btn btn-primary" :disabled="submitting">
      {{ submitting ? 'Guardando…' : 'Guardar contraseña' }}
    </button>

    <RouterLink v-if="!forced" to="/" class="btn btn-secondary">Cancelar</RouterLink>
    <button v-else type="button" class="btn-link" @click="logout">Cerrar sesión</button>
  </form>
</template>

<style scoped>
.change-password {
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
