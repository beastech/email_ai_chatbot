@description('Name of the Function App')
param functionAppName string = 'email-ai-chatbot-${uniqueString(resourceGroup().id)}'
@description('Location for all resources')
param location string = resourceGroup().location

@description('Storage account name (must be globally unique, 3-24 lowercase)')
param storageAccountName string = toLower('st${substring(uniqueString(resourceGroup().id), 0, 18)}')

@description('SKU for the App Service plan (use Y1 for Consumption)')
param skuName string = 'Y1'

@description('Optional package URL to run from (ZIP)')
param packageUri string = ''

// Email / SMTP / OpenAI settings (sensitive values marked secure)
param IMAP_Host string
param IMAP_Port int = 993
param IMAP_Username string
@secure()
param IMAP_Password string

param SMTP_Host string
param SMTP_Port int = 587
param SMTP_Username string
@secure()
param SMTP_Password string

param OPENAI_Endpoint string
@secure()
param OPENAI_Key string

@description('From email address used for replies (optional)')
param FROM_Email string = ''

// Resource: Storage Account
resource storageAccount 'Microsoft.Storage/storageAccounts@2022-09-01' = {
  name: storageAccountName
  location: location
  sku: {
    name: 'Standard_LRS'
  }
  kind: 'StorageV2'
  properties: {
    minimumTlsVersion: 'TLS1_2'
    allowBlobPublicAccess: false
  }
}

// Resource: Application Insights (optional but recommended)
resource appInsights 'Microsoft.Insights/components@2020-02-02' = {
  name: '${functionAppName}-ai'
  location: location
  kind: 'web'
  properties: {
    Application_Type: 'web'
  }
}

// App Service Plan (Consumption)
resource serverFarm 'Microsoft.Web/serverfarms@2021-02-01' = {
  name: '${functionAppName}-plan'
  location: location
  kind: 'functionapp'
  sku: {
    name: skuName
    tier: skuName == 'Y1' ? 'Dynamic' : 'Elastic'
  }
}

// helper: storage connection string
var storageKeys = listKeys(storageAccount.id, storageAccount.apiVersion)
var storageConnectionString = 'DefaultEndpointsProtocol=https;AccountName=${storageAccount.name};AccountKey=${storageKeys.keys[0].value};EndpointSuffix=core.windows.net'

// Function App
resource functionApp 'Microsoft.Web/sites@2022-03-01' = {
  name: functionAppName
  location: location
  kind: 'functionapp'
  properties: {
    serverFarmId: serverFarm.id
    siteConfig: {
      appSettings: [
        {
          name: 'AzureWebJobsStorage'
          value: storageConnectionString
        }
        {
          name: 'FUNCTIONS_WORKER_RUNTIME'
          value: 'dotnet-isolated'
        }
        {
          name: 'WEBSITE_RUN_FROM_PACKAGE'
          value: packageUri
        }
        {
          name: 'APPINSIGHTS_INSTRUMENTATIONKEY'
          value: appInsights.properties.InstrumentationKey
        }

        // Email / SMTP / IMAP settings exposed as environment variables
        {
          name: 'IMAP_Host'
          value: IMAP_Host
        }
        {
          name: 'IMAP_Port'
          value: string(IMAP_Port)
        }
        {
          name: 'IMAP_Username'
          value: IMAP_Username
        }
        {
          name: 'IMAP_Password'
          value: IMAP_Password
        }
        {
          name: 'SMTP_Host'
          value: SMTP_Host
        }
        {
          name: 'SMTP_Port'
          value: string(SMTP_Port)
        }
        {
          name: 'SMTP_Username'
          value: SMTP_Username
        }
        {
          name: 'SMTP_Password'
          value: SMTP_Password
        }
        {
          name: 'OPENAI_Endpoint'
          value: OPENAI_Endpoint
        }
        {
          name: 'OPENAI_Key'
          value: OPENAI_Key
        }
        {
          name: 'FROM_Email'
          value: FROM_Email
        }
      ]
    }
  }
  dependsOn: [ storageAccount, serverFarm, appInsights ]
}

output functionAppDefaultHostName string = functionApp.properties.defaultHostName
output functionAppName string = functionApp.name
