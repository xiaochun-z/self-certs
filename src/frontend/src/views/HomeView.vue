<script setup lang="ts">
import { ref, onMounted, computed, watch } from 'vue'

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
O                   = My Organization    # <-- CHANGE THIS
CN                  = app.shenhe.org     # <-- CHANGE THIS: Primary Domain

[ v3_req ]
basicConstraints    = CA:FALSE
keyUsage            = nonRepudiation, digitalSignature, keyEncipherment
subjectAltName      = @alt_names

[ alt_names ]
# <-- CHANGE THESE: List all your domains and IPs below
DNS.1               = app.shenhe.org
DNS.2               = *.app.shenhe.org
IP.1                = 192.168.2.4
`

const cas = ref<any[]>([])
const selectedCaId = ref<number | null>(null)
const isCreatingCa = ref(false)

// CA Creation State
const newCaName = ref('')
const newCaPassword = ref('')
const isCaLoading = ref(false)
const caErrorMessage = ref('')

// Cert Generation State
const caPasswordForCert = ref('')
const serverReqCnf = ref(defaultCnf)
const generatedKey = ref('')
const generatedCrt = ref('')
const isCertLoading = ref(false)
const certErrorMessage = ref('')
const lastGeneratedPrefix = ref('server')

// History State
const history = ref<any[]>([])

const selectedCa = computed(() => cas.value.find(c => c.id === selectedCaId.value))

onMounted(async () => {
  await loadCas()
})

watch(selectedCaId, async (newId) => {
  if (newId) {
    await loadHistory(newId)
    generatedKey.value = ''
    generatedCrt.value = ''
  }
})

const loadCas = async () => {
  try {
    const res = await fetch('/api/cert/cas')
    const data = await res.json()
    if (data.code === 200) {
      cas.value = data.data || []
      if (cas.value.length > 0 && !selectedCaId.value) {
        selectedCaId.value = cas.value[0].id
      } else if (cas.value.length === 0) {
        isCreatingCa.value = true
      }
    }
  } catch (e) {
    console.error('Failed to load CAs', e)
  }
}

const createCa = async () => {
  if (!newCaName.value) {
    caErrorMessage.value = 'Please enter a CA Name.'
    return
  }
  isCaLoading.value = true
  caErrorMessage.value = ''
  try {
    const res = await fetch('/api/cert/ca', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ name: newCaName.value, password: newCaPassword.value })
    })
    const data = await res.json()
    if (data.code === 200) {
      await loadCas()
      selectedCaId.value = data.data.id
      isCreatingCa.value = false
      newCaName.value = ''
      newCaPassword.value = ''
    } else {
      caErrorMessage.value = data.message || 'Failed to create CA.'
    }
  } catch (e: any) {
    caErrorMessage.value = e.message || 'Network error.'
  } finally {
    isCaLoading.value = false
  }
}

const generateCert = async () => {
  isCertLoading.value = true
  certErrorMessage.value = ''
  generatedKey.value = ''
  generatedCrt.value = ''

  try {
    const response = await fetch('/api/cert/generate', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({
        caId: selectedCaId.value,
        caPassword: caPasswordForCert.value,
        serverReqCnfTemplate: serverReqCnf.value
      })
    })

    const data = await response.json()
    if (data.code === 200) {
      generatedKey.value = data.data.serverKey
      generatedCrt.value = data.data.serverCrt
      lastGeneratedPrefix.value = getSafeFilenamePrefix(serverReqCnf.value)
      await loadHistory(selectedCaId.value!)
    } else {
      certErrorMessage.value = data.message || 'Generation failed.'
    }
  } catch (err: any) {
    certErrorMessage.value = err.message || 'Network error occurred.'
  } finally {
    isCertLoading.value = false
  }
}

const loadHistory = async (caId: number) => {
  try {
    const res = await fetch(`/api/cert/history/${caId}`)
    const data = await res.json()
    if (data.code === 200) {
      history.value = data.data || []
    }
  } catch (e) {
    console.error('Failed to load history', e)
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

const extractCN = (cnf: string) => {
  if (!cnf) return 'Unknown Domain';
  const match = cnf.match(/^CN\s*=\s*(.*?)(?:\s*#.*)?$/m);
  return (match && match[1]) ? match[1].trim() : 'Unknown Domain';
}

const getSafeFilenamePrefix = (cnf: string) => {
  const cn = extractCN(cnf);
  if (cn === 'Unknown Domain') return 'server';
  // Replace wildcard * with 'wildcard' and non-alphanumeric chars with underscore
  return cn.replace(/^\*\./, 'wildcard_').replace(/[^a-zA-Z0-9.-]/g, '_');
}

const extractAltNames = (cnf: string) => {
  if (!cnf) return [];
  const match = cnf.match(/\[\s*alt_names\s*\]([\s\S]*)/i);
  const section = match ? match[1] : undefined;
  if (!section) return [];
  
  const lines = section.split('\n');
  const sans: string[] = [];
  for (const line of lines) {
    const trimmed = line.trim();
    if (trimmed.startsWith('[')) break; // Reached next section
    if (!trimmed || trimmed.startsWith('#')) continue;
    
    const parts = trimmed.split('=');
    if (parts.length >= 2) {
       const rawValue = parts[1];
       if (rawValue) {
         const cleanValue = rawValue.trim().split('#')[0];
         if (cleanValue) {
           sans.push(cleanValue.trim()); // remove inline comments
         }
       }
    }
  }
  return sans;
}

const formatDate = (dateStr: string) => {
  if (!dateStr) return '';
  return new Date(dateStr).toLocaleString()
}
</script>

<template>
  <div class="min-h-screen bg-gray-50 dark:bg-gray-900 py-8 px-4 sm:px-6 lg:px-8 transition-colors duration-200">
    <div class="max-w-[1536px] mx-auto space-y-8">

      <div class="text-center">
        <h1 class="text-3xl font-extrabold text-gray-900 dark:text-white">Self-Signed Certificate Generator</h1>
        <p class="mt-2 text-sm text-gray-600 dark:text-gray-400">Step-by-step assistant for issuing server certificates using Root CAs.</p>
      </div>

      <div class="bg-white dark:bg-gray-800 p-6 rounded-xl shadow-lg ring-1 ring-gray-200 dark:ring-gray-700 transition-colors duration-200">
        <div class="flex flex-wrap justify-between items-center mb-4 border-b border-gray-200 dark:border-gray-700 pb-2 gap-4">
          <h2 class="text-xl font-bold text-gray-800 dark:text-gray-100">Step 1: Root CA Configuration</h2>
          <button v-if="cas.length > 0 && !isCreatingCa" @click="isCreatingCa = true" class="text-sm text-indigo-600 dark:text-indigo-400 font-semibold hover:underline bg-transparent border-0 cursor-pointer">+ Create Another Root CA</button>
          <button v-if="cas.length > 0 && isCreatingCa" @click="isCreatingCa = false" class="text-sm text-gray-500 font-semibold hover:underline bg-transparent border-0 cursor-pointer">Cancel</button>
        </div>

        <div v-if="isCreatingCa || cas.length === 0" class="space-y-4">
          <div v-if="cas.length === 0" class="bg-blue-50 dark:bg-blue-900/30 text-blue-800 dark:text-blue-300 p-4 rounded-md text-sm mb-4 border border-blue-100 dark:border-blue-800">
            👋 <strong>Welcome!</strong> To start issuing certificates, you need a Root Certificate Authority (CA) first. Fill out the form below to generate one with a single click.
          </div>
          <div class="grid grid-cols-1 sm:grid-cols-2 gap-4">
            <div>
              <label class="block text-sm font-medium text-gray-700 dark:text-gray-300">CA Name</label>
              <input v-model="newCaName" type="text" placeholder="e.g. My Local Root CA" class="mt-1 block w-full rounded-md border-gray-300 dark:border-gray-600 dark:bg-gray-700 dark:text-gray-100 shadow-sm focus:border-indigo-500 focus:ring-indigo-500 sm:text-sm border p-2" />
            </div>
            <div>
              <label class="block text-sm font-medium text-gray-700 dark:text-gray-300">CA Key Password</label>
              <input v-model="newCaPassword" type="password" placeholder="(Optional) Protect your CA private key" class="mt-1 block w-full rounded-md border-gray-300 dark:border-gray-600 dark:bg-gray-700 dark:text-gray-100 shadow-sm focus:border-indigo-500 focus:ring-indigo-500 sm:text-sm border p-2" />
            </div>
          </div>
          <button @click="createCa" :disabled="isCaLoading" class="mt-4 w-full sm:w-auto px-6 py-2 border border-transparent rounded-md shadow-sm text-sm font-medium text-white bg-green-600 hover:bg-green-700 disabled:opacity-50 transition cursor-pointer">
            {{ isCaLoading ? 'Generating...' : 'Generate Root CA' }}
          </button>
          <div v-if="caErrorMessage" class="text-red-600 text-sm mt-2">{{ caErrorMessage }}</div>
        </div>

        <div v-else class="space-y-4">
          <div>
            <label class="block text-sm font-medium text-gray-700 dark:text-gray-300">Select Active Root CA</label>
            <div class="mt-1 flex flex-col sm:flex-row sm:items-center space-y-3 sm:space-y-0 sm:space-x-4">
              <select v-model="selectedCaId" class="block w-full sm:w-1/2 rounded-md border-gray-300 dark:border-gray-600 dark:bg-gray-700 dark:text-gray-100 shadow-sm focus:border-indigo-500 focus:ring-indigo-500 sm:text-sm border p-2">
                <option v-for="ca in cas" :key="ca.id" :value="ca.id">{{ ca.name }}</option>
              </select>
              <button v-if="selectedCa" @click="downloadFile('ca.crt', selectedCa.caCrt)" class="px-4 py-2 bg-indigo-50 dark:bg-indigo-900/30 text-indigo-700 dark:text-indigo-400 border border-indigo-200 dark:border-indigo-800 text-sm font-medium rounded-md hover:bg-indigo-100 dark:hover:bg-indigo-800/50 transition cursor-pointer">
                ⬇️ Download CA Cert
              </button>
            </div>
            <p class="mt-2 text-xs text-gray-500 dark:text-gray-400">
              💡 <strong>Tip:</strong> Install this Root CA certificate (<code>ca.crt</code>) into your OS or browser's "Trusted Root Certification Authorities" store to automatically trust all server certificates it issues.
            </p>
          </div>
        </div>
      </div>

      <div v-if="selectedCaId && !isCreatingCa" class="grid grid-cols-1 lg:grid-cols-5 gap-8">

        <div class="lg:col-span-3 bg-white dark:bg-gray-800 p-6 rounded-xl shadow-lg ring-1 ring-gray-200 dark:ring-gray-700 transition-colors duration-200 flex flex-col">
          <h2 class="text-xl font-bold text-gray-800 dark:text-gray-100 mb-4 border-b border-gray-200 dark:border-gray-700 pb-2">Step 2: Issue Certificate</h2>
          
          <div class="space-y-4 grow">
            <div>
              <label class="block text-sm font-medium text-gray-700 dark:text-gray-300">Root CA Password</label>
              <input v-model="caPasswordForCert" type="password" class="mt-1 block w-full rounded-md border-gray-300 dark:border-gray-600 dark:bg-gray-700 dark:text-gray-100 shadow-sm focus:border-indigo-500 focus:ring-indigo-500 sm:text-sm border p-2" placeholder="Leave blank if CA was created without a password" />
            </div>
            
            <div>
              <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-2">Server Configuration</label>
              
              <div class="mb-3 p-3 bg-amber-50 dark:bg-amber-900/30 border border-amber-200 dark:border-amber-800 rounded-md">
                <p class="text-sm text-amber-800 dark:text-amber-300">
                  <span class="font-bold">⚠️ Action Required:</span> Please update the <code class="font-bold">CN = ...</code> and the entries under <code class="font-bold">[ alt_names ]</code> below to match your actual Domain(s) and IP address(es) before issuing.
                </p>
              </div>

              <textarea v-model="serverReqCnf" rows="14" wrap="off" class="block w-full rounded-md border-gray-300 dark:border-gray-600 dark:bg-gray-700 dark:text-gray-100 shadow-sm focus:border-indigo-500 focus:ring-indigo-500 sm:text-sm font-mono border p-2 overflow-x-auto"></textarea>
            </div>
          </div>

          <div class="mt-4 space-y-4">
            <button @click="generateCert" :disabled="isCertLoading" class="w-full flex justify-center py-2 px-4 border border-transparent rounded-md shadow-sm text-sm font-medium text-white bg-indigo-600 hover:bg-indigo-700 disabled:opacity-50 transition cursor-pointer">
              {{ isCertLoading ? 'Processing...' : 'Issue Certificate' }}
            </button>
            <div v-if="certErrorMessage" class="p-3 bg-red-50 dark:bg-red-900/30 text-red-700 dark:text-red-400 text-sm rounded-md border border-red-200 dark:border-red-800/50">{{ certErrorMessage }}</div>

            <div v-if="generatedKey" class="pt-4 border-t border-gray-200 dark:border-gray-700 space-y-4">
              <h3 class="font-bold text-green-600 dark:text-green-400">✅ Certificate Issued Successfully!</h3>
              <div class="flex space-x-4">
                <button @click="downloadFile(`${lastGeneratedPrefix}.key`, generatedKey)" class="flex-1 bg-gray-100 dark:bg-gray-700 text-gray-800 dark:text-gray-200 py-2 px-4 rounded-md text-sm font-medium hover:bg-gray-200 dark:hover:bg-gray-600 transition cursor-pointer border-0">Download .key</button>
                <button @click="downloadFile(`${lastGeneratedPrefix}.crt`, generatedCrt)" class="flex-1 bg-gray-100 dark:bg-gray-700 text-gray-800 dark:text-gray-200 py-2 px-4 rounded-md text-sm font-medium hover:bg-gray-200 dark:hover:bg-gray-600 transition cursor-pointer border-0">Download .crt</button>
              </div>
            </div>
          </div>
        </div>

        <div class="lg:col-span-2 bg-white dark:bg-gray-800 p-6 rounded-xl shadow-lg ring-1 ring-gray-200 dark:ring-gray-700 transition-colors duration-200 flex flex-col">
          <h2 class="text-xl font-bold text-gray-800 dark:text-gray-100 mb-4 border-b border-gray-200 dark:border-gray-700 pb-2">Step 3: History</h2>
          
          <div v-if="history.length === 0" class="grow flex items-center justify-center text-gray-400 dark:text-gray-500 text-sm text-center py-12 italic border-2 border-dashed border-gray-200 dark:border-gray-700 rounded-lg">
            No certificates issued with this CA yet. <br> Configure and generate your first one on the left!
          </div>
          
          <div v-else class="space-y-3 overflow-y-auto max-h-[600px] pr-2 grow">
            <div v-for="record in history" :key="record.id" class="p-4 border border-gray-200 dark:border-gray-700 rounded-md bg-gray-50 dark:bg-gray-900 shadow-sm hover:shadow transition">
              <div class="flex justify-between items-start mb-3">
                <div class="overflow-hidden">
                  <div class="font-bold text-indigo-600 dark:text-indigo-400 text-sm truncate" :title="extractCN(record.serverReqCnf)">
                    {{ extractCN(record.serverReqCnf) }}
                  </div>
                  
                  <div class="mt-1.5 flex flex-wrap gap-1.5">
                    <span v-for="san in extractAltNames(record.serverReqCnf)" :key="san" class="px-1.5 py-0.5 bg-gray-200 dark:bg-gray-700 text-gray-700 dark:text-gray-300 text-[10px] font-mono rounded border border-gray-300 dark:border-gray-600">
                      {{ san }}
                    </span>
                  </div>

                  <div class="text-xs text-gray-500 dark:text-gray-400 mt-2">{{ formatDate(record.createdAt) }}</div>
                </div>
              </div>
              <div class="flex space-x-2">
                <button @click="downloadFile(`${getSafeFilenamePrefix(record.serverReqCnf)}.key`, record.serverKey)" class="flex-1 text-xs bg-white dark:bg-gray-800 border border-gray-300 dark:border-gray-600 px-3 py-1.5 rounded hover:bg-gray-50 dark:hover:bg-gray-700 transition font-medium cursor-pointer text-gray-700 dark:text-gray-200">.key</button>
                <button @click="downloadFile(`${getSafeFilenamePrefix(record.serverReqCnf)}.crt`, record.serverCrt)" class="flex-1 text-xs bg-white dark:bg-gray-800 border border-gray-300 dark:border-gray-600 px-3 py-1.5 rounded hover:bg-gray-50 dark:hover:bg-gray-700 transition font-medium cursor-pointer text-gray-700 dark:text-gray-200">.crt</button>
              </div>
            </div>
          </div>
        </div>

      </div>

    </div>
  </div>
</template>