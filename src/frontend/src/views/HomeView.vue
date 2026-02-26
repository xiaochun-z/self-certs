<script setup lang="ts">
import { ref, onMounted } from 'vue'

const defaultCnf = `# server_req.cnf
[ req ]
default_bits        = 2048
prompt              = no
default_md          = sha256
distinguished_name  = req_distinguished_name
req_extensions      = v3_req

[ req_distinguished_name ]
C                   = US
ST                  = California
L                   = San Francisco
O                   = app.Shenhe.org
CN                  = app.shenhe.org

[ v3_req ]
basicConstraints    = CA:FALSE
keyUsage            = nonRepudiation, digitalSignature, keyEncipherment
subjectAltName      = @alt_names

[ alt_names ]
DNS.1               = app.shenhe.org
DNS.2               = *.app.shenhe.org
DNS.3               = api.proxy.app.shenhe.org
IP.1                = 192.168.2.4
`

const caCrt = ref('')
const caKey = ref('')
const caPassword = ref('')
const serverReqCnf = ref(defaultCnf)

const generatedKey = ref('')
const generatedCrt = ref('')
const isLoading = ref(false)
const errorMessage = ref('')

onMounted(async () => {
  try {
    const res = await fetch('/api/cert/ca')
    const data = await res.json()
    if (data.code === 200 && data.data) {
      if (data.data.caCrt) caCrt.value = data.data.caCrt
      if (data.data.caKey) caKey.value = data.data.caKey
    }
  } catch (e) {
    console.error('Failed to load CA config', e)
  }
})

const generateCert = async () => {
  isLoading.value = true
  errorMessage.value = ''
  generatedKey.value = ''
  generatedCrt.value = ''

  try {
    const response = await fetch('/api/cert/generate', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({
        caCrt: caCrt.value,
        caKey: caKey.value,
        caPassword: caPassword.value,
        serverReqCnfTemplate: serverReqCnf.value
      })
    })

    const data = await response.json()
    if (data.code === 200) {
      generatedKey.value = data.data.serverKey
      generatedCrt.value = data.data.serverCrt
    } else {
      errorMessage.value = data.message || 'Generation failed.'
    }
  } catch (err: any) {
    errorMessage.value = err.message || 'Network error occurred.'
  } finally {
    isLoading.value = false
  }
}

const downloadFile = (filename: string, content: string) => {
  const blob = new Blob([content], { type: 'text/plain' })
  const url = URL.createObjectURL(blob)
  const a = document.createElement('a')
  a.href = url
  a.download = filename
  document.body.appendChild(a)
  a.click()
  document.body.removeChild(a)
  URL.revokeObjectURL(url)
}
</script>

<template>
  <div class="min-h-screen bg-gray-50 dark:bg-gray-900 py-8 px-4 sm:px-6 lg:px-8 transition-colors duration-200">
    <div class="max-w-6xl mx-auto space-y-8">

      <div class="text-center">
        <h1 class="text-3xl font-extrabold text-gray-900 dark:text-white">Self-Signed Certificate Generator</h1>
        <p class="mt-2 text-sm text-gray-600 dark:text-gray-400">Issue server certificates securely using your own Root CA.</p>
      </div>

      <div class="grid grid-cols-1 lg:grid-cols-2 gap-8">
        <div class="bg-white dark:bg-gray-800 p-6 rounded-xl shadow-lg ring-1 ring-gray-200 dark:ring-gray-700 transition-colors duration-200">
          <h2 class="text-xl font-bold text-gray-800 dark:text-gray-100 mb-4 border-b border-gray-200 dark:border-gray-700 pb-2">CA Information & Config</h2>

          <div class="space-y-4">
            <div>
              <label class="block text-sm font-medium text-gray-700 dark:text-gray-300">CA Certificate (ca.crt)</label>
              <textarea v-model="caCrt" rows="4" class="mt-1 block w-full rounded-md border-gray-300 dark:border-gray-600 dark:bg-gray-700 dark:text-gray-100 shadow-sm focus:border-indigo-500 focus:ring-indigo-500 sm:text-sm font-mono border p-2" placeholder="-----BEGIN CERTIFICATE-----..."></textarea>
            </div>

            <div>
              <label class="block text-sm font-medium text-gray-700 dark:text-gray-300">CA Private Key (ca.key)</label>
              <textarea v-model="caKey" rows="4" class="mt-1 block w-full rounded-md border-gray-300 dark:border-gray-600 dark:bg-gray-700 dark:text-gray-100 shadow-sm focus:border-indigo-500 focus:ring-indigo-500 sm:text-sm font-mono border p-2" placeholder="-----BEGIN PRIVATE KEY-----..."></textarea>
            </div>

            <div>
              <label class="block text-sm font-medium text-gray-700 dark:text-gray-300">CA Key Password (Optional)</label>
              <input v-model="caPassword" type="password" class="mt-1 block w-full rounded-md border-gray-300 dark:border-gray-600 dark:bg-gray-700 dark:text-gray-100 shadow-sm focus:border-indigo-500 focus:ring-indigo-500 sm:text-sm border p-2" />
            </div>

            <div>
              <label class="block text-sm font-medium text-gray-700 dark:text-gray-300">Server Request Config (server_req.cnf)</label>
              <textarea v-model="serverReqCnf" rows="8" class="mt-1 block w-full rounded-md border-gray-300 dark:border-gray-600 dark:bg-gray-700 dark:text-gray-100 shadow-sm focus:border-indigo-500 focus:ring-indigo-500 sm:text-sm font-mono border p-2"></textarea>
            </div>

            <button
              @click="generateCert"
              :disabled="isLoading"
              class="w-full flex justify-center py-2 px-4 border border-transparent rounded-md shadow-sm text-sm font-medium text-white bg-indigo-600 hover:bg-indigo-700 dark:bg-indigo-500 dark:hover:bg-indigo-600 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-indigo-500 dark:focus:ring-offset-gray-800 disabled:opacity-50 transition">
              {{ isLoading ? 'Generating...' : 'Generate Certificate' }}
            </button>

            <div v-if="errorMessage" class="p-3 mt-4 bg-red-50 dark:bg-red-900/30 text-red-700 dark:text-red-400 text-sm rounded-md border border-red-200 dark:border-red-800/50">
              {{ errorMessage }}
            </div>
          </div>
        </div>

        <div class="bg-white dark:bg-gray-800 p-6 rounded-xl shadow-lg ring-1 ring-gray-200 dark:ring-gray-700 transition-colors duration-200">
          <h2 class="text-xl font-bold text-gray-800 dark:text-gray-100 mb-4 border-b border-gray-200 dark:border-gray-700 pb-2">Generated Result</h2>

          <div v-if="!generatedKey && !generatedCrt" class="text-gray-400 dark:text-gray-500 text-center py-12 italic">
            Generated keys and certificates will appear here.
          </div>

          <div v-else class="space-y-6">
            <div>
              <div class="flex justify-between items-center mb-1">
                <label class="block text-sm font-medium text-gray-700 dark:text-gray-300">Server Private Key (app.shenhe.key)</label>
                <button @click="downloadFile('app.shenhe.key', generatedKey)" class="text-xs text-indigo-600 dark:text-indigo-400 hover:text-indigo-800 dark:hover:text-indigo-300 font-semibold">Download .key</button>
              </div>
              <textarea :value="generatedKey" readonly rows="8" class="mt-1 block w-full rounded-md border-gray-300 dark:border-gray-600 bg-gray-50 dark:bg-gray-900 dark:text-gray-300 shadow-sm sm:text-sm font-mono border p-2"></textarea>
            </div>

            <div>
              <div class="flex justify-between items-center mb-1">
                <label class="block text-sm font-medium text-gray-700 dark:text-gray-300">Server Certificate (app.shenhe.crt)</label>
                <button @click="downloadFile('app.shenhe.crt', generatedCrt)" class="text-xs text-indigo-600 dark:text-indigo-400 hover:text-indigo-800 dark:hover:text-indigo-300 font-semibold">Download .crt</button>
              </div>
              <textarea :value="generatedCrt" readonly rows="10" class="mt-1 block w-full rounded-md border-gray-300 dark:border-gray-600 bg-gray-50 dark:bg-gray-900 dark:text-gray-300 shadow-sm sm:text-sm font-mono border p-2"></textarea>
            </div>
          </div>
        </div>
      </div>

    </div>
  </div>
</template>