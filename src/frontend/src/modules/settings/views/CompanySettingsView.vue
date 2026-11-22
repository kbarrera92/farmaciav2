<script setup lang="ts">
import { computed, onMounted, reactive, ref } from 'vue'
import AsyncState from '@/components/AsyncState.vue'
import AuditInfo from '@/components/AuditInfo.vue'
import PageHeader from '@/components/PageHeader.vue'
import { useAuthStore } from '@/modules/auth/stores/authStore'
import { Permissions } from '@/modules/auth/types'
import { ApiError, errorMessage } from '@/services/http'
import { companySettingsService } from '../services/companySettingsService'
import { LogoRules, type CompanySettings } from '../types'

const auth = useAuthStore()
const canManage = auth.can(Permissions.SettingsManage)
const maxLogoKb = LogoRules.maxBytes / 1024

const settings = ref<CompanySettings | null>(null)
const form = reactive({ name: '', taxId: '', address: '', phone: '', email: '' })

const loading = ref(false)
const loadError = ref<string | null>(null)
const saving = ref(false)
const logoBusy = ref(false)
const error = ref<string | null>(null)
const success = ref<string | null>(null)
const fieldErrors = ref<Record<string, string | undefined>>({})
const fileInput = ref<HTMLInputElement | null>(null)

const logoUrl = computed(() => (settings.value ? companySettingsService.logoUrl(settings.value) : null))

async function load(): Promise<void> {
  loading.value = true
  loadError.value = null
  try {
    setSettings(await companySettingsService.get())
  } catch (e) {
    loadError.value = errorMessage(e)
  } finally {
    loading.value = false
  }
}

function setSettings(detail: CompanySettings): void {
  settings.value = detail
  form.name = detail.name
  form.taxId = detail.taxId ?? ''
  form.address = detail.address ?? ''
  form.phone = detail.phone ?? ''
  form.email = detail.email ?? ''
}

function clearMessages(): void {
  error.value = null
  success.value = null
  fieldErrors.value = {}
}

async function save(): Promise<void> {
  clearMessages()
  saving.value = true
  try {
    setSettings(
      await companySettingsService.update({
        name: form.name,
        taxId: form.taxId.trim() || null,
        address: form.address.trim() || null,
        phone: form.phone.trim() || null,
        email: form.email.trim() || null,
      }),
    )
    success.value = 'Datos de la empresa guardados.'
  } catch (e) {
    error.value = errorMessage(e)
    if (e instanceof ApiError && e.status === 400) {
      fieldErrors.value = {
        name: e.fieldError('name'),
        taxId: e.fieldError('taxId'),
        address: e.fieldError('address'),
        phone: e.fieldError('phone'),
        email: e.fieldError('email'),
      }
    }
  } finally {
    saving.value = false
  }
}

async function uploadLogo(event: Event): Promise<void> {
  const input = event.target as HTMLInputElement
  const file = input.files?.[0]
  input.value = ''
  if (!file) return

  clearMessages()
  // Quick feedback only; the backend validates the real content and size.
  if (!LogoRules.accept.split(',').includes(file.type)) {
    fieldErrors.value = { logo: 'El logo debe ser una imagen PNG o JPEG.' }
    return
  }
  if (file.size > LogoRules.maxBytes) {
    fieldErrors.value = { logo: `El logo no puede superar ${maxLogoKb} KB.` }
    return
  }

  logoBusy.value = true
  try {
    setSettings(await companySettingsService.uploadLogo(file))
    success.value = 'Logo actualizado.'
  } catch (e) {
    error.value = errorMessage(e)
    if (e instanceof ApiError && e.status === 400) fieldErrors.value = { logo: e.fieldError('logo') }
  } finally {
    logoBusy.value = false
  }
}

async function removeLogo(): Promise<void> {
  if (!window.confirm('¿Quitar el logo de la empresa?')) return
  clearMessages()
  logoBusy.value = true
  try {
    setSettings(await companySettingsService.removeLogo())
    success.value = 'Logo eliminado.'
  } catch (e) {
    error.value = errorMessage(e)
  } finally {
    logoBusy.value = false
  }
}

onMounted(load)
</script>

<template>
  <section class="page">
    <PageHeader title="Datos de la empresa" subtitle="Información y logo que se muestran en tickets y reportes." />

    <AsyncState :loading="loading" :error="loadError" @retry="load">
      <p v-if="success" class="alert alert-success" role="status">{{ success }}</p>
      <p v-if="error" class="alert alert-error" role="alert">{{ error }}</p>

      <form class="card" novalidate @submit.prevent="save">
        <h2 class="card-title">Información general</h2>
        <fieldset :disabled="!canManage || saving" class="form-grid">
          <div class="field">
            <label for="name">Nombre comercial</label>
            <input id="name" v-model="form.name" class="input" maxlength="100" :aria-invalid="!!fieldErrors.name" />
            <p v-if="fieldErrors.name" class="field-error">{{ fieldErrors.name }}</p>
          </div>
          <div class="field">
            <label for="taxId">NIT</label>
            <input id="taxId" v-model="form.taxId" class="input" maxlength="25" :aria-invalid="!!fieldErrors.taxId" />
            <p v-if="fieldErrors.taxId" class="field-error">{{ fieldErrors.taxId }}</p>
          </div>
          <div class="field">
            <label for="address">Dirección</label>
            <input id="address" v-model="form.address" class="input" maxlength="150" :aria-invalid="!!fieldErrors.address" />
            <p v-if="fieldErrors.address" class="field-error">{{ fieldErrors.address }}</p>
          </div>
          <div class="field">
            <label for="phone">Teléfono</label>
            <input id="phone" v-model="form.phone" class="input" maxlength="50" :aria-invalid="!!fieldErrors.phone" />
            <p v-if="fieldErrors.phone" class="field-error">{{ fieldErrors.phone }}</p>
          </div>
          <div class="field">
            <label for="email">Correo electrónico</label>
            <input
              id="email"
              v-model="form.email"
              type="email"
              class="input"
              maxlength="100"
              :aria-invalid="!!fieldErrors.email"
            />
            <p v-if="fieldErrors.email" class="field-error">{{ fieldErrors.email }}</p>
          </div>
        </fieldset>

        <div v-if="canManage" class="actions form-actions">
          <button type="submit" class="btn btn-primary" :disabled="saving">
            {{ saving ? 'Guardando…' : 'Guardar datos' }}
          </button>
        </div>
        <AuditInfo v-if="settings?.audit" :audit="settings.audit" />
      </form>

      <div class="card">
        <h2 class="card-title">Logo</h2>
        <div class="logo-row">
          <div class="logo-preview">
            <img v-if="logoUrl" :src="logoUrl" alt="Logo de la empresa" />
            <span v-else class="muted small">Sin logo</span>
          </div>
          <div class="logo-help">
            <p class="muted small">Imagen PNG o JPEG de hasta {{ maxLogoKb }} KB.</p>
            <p v-if="canManage && !settings?.isConfigured" class="muted small">
              Guarde primero la información general para poder subir el logo.
            </p>
            <p v-if="fieldErrors.logo" class="field-error">{{ fieldErrors.logo }}</p>
            <div v-if="canManage" class="actions">
              <input ref="fileInput" type="file" :accept="LogoRules.accept" hidden @change="uploadLogo" />
              <button
                type="button"
                class="btn btn-secondary"
                :disabled="logoBusy || !settings?.isConfigured"
                @click="fileInput?.click()"
              >
                {{ logoBusy ? 'Procesando…' : settings?.hasLogo ? 'Cambiar logo' : 'Subir logo' }}
              </button>
              <button
                v-if="settings?.hasLogo"
                type="button"
                class="btn btn-danger"
                :disabled="logoBusy"
                @click="removeLogo"
              >
                Quitar logo
              </button>
            </div>
          </div>
        </div>
      </div>
    </AsyncState>
  </section>
</template>

<style scoped>
fieldset {
  border: none;
  margin: 0;
  padding: 0;
  min-width: 0;
}

.form-actions {
  margin-top: 1.25rem;
}

.logo-row {
  display: flex;
  flex-wrap: wrap;
  gap: 1.5rem;
  align-items: flex-start;
}

.logo-preview {
  display: grid;
  place-items: center;
  width: 180px;
  height: 120px;
  padding: 0.5rem;
  border: 1px dashed var(--color-border);
  border-radius: 8px;
  background: var(--color-surface-muted);
}

.logo-preview img {
  max-width: 100%;
  max-height: 100%;
  object-fit: contain;
}

.logo-help {
  flex: 1;
  min-width: 200px;
}

.logo-help p {
  margin: 0 0 0.5rem;
}
</style>
